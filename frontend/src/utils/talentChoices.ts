import type {
  Characteristic, CharacterTalentChoice, SheetTalent, SkillDef, SkillKind, Spell, TalentDef,
} from '../api/types'
import { lang, t } from '../i18n'
import { CHARACTERISTIC_LABELS, CHARACTERISTICS, localizedName } from './labels'

/** Талант Dedication не поднимает характеристику выше этого значения. */
export const TALENT_CHARACTERISTIC_MAX = 5

/** Порядок групп навыков в форме выбора: сначала те, что доступны «Квалификации». */
const SKILL_KIND_ORDER: SkillKind[] = ['general', 'knowledge', 'social', 'combat', 'magic']

/** Один вариант формы выбора таланта. */
export interface TalentChoiceOption {
  /** Стабильное значение, которое уходит на сервер. */
  value: string
  label: string
  /** Группа навыка — для заголовков в форме. */
  group?: SkillKind
  /** Пояснение рядом с вариантом: «2 → 3» у Dedication. */
  note?: string
}

/** Сколько значений нужно выбрать при покупке следующего ранга (ROT-TAL-03). */
export function choiceCountForNextRank(
  talent: Pick<TalentDef, 'choiceKind' | 'choiceCountFirstRank' | 'choiceCountNextRank'>,
  ranksOwned: number,
): number {
  if (talent.choiceKind === 'none') return 0
  return ranksOwned === 0 ? talent.choiceCountFirstRank : talent.choiceCountNextRank
}

/**
 * Варианты выбора характеристики или навыка для следующего ранга. Зеркало серверных
 * ограничений: повтор между рангами, виды навыков и потолок Dedication. Сервер всё равно
 * проверяет запрос сам.
 */
export function talentChoiceOptions(
  talent: TalentDef,
  owned: SheetTalent | undefined,
  characteristics: Record<Characteristic, number>,
  skills: SkillDef[],
): TalentChoiceOption[] {
  // Регистр значений не важен: сервер хранит «Willpower», клиент оперирует «willpower».
  const taken = new Set<string>()
  if (talent.choiceDistinctAcrossRanks) {
    for (const choice of owned?.choices ?? []) taken.add(choice.value.toLowerCase())
    for (const granted of owned?.grantedCharacteristics ?? []) taken.add(granted.toLowerCase())
  }

  if (talent.choiceKind === 'characteristic') {
    return CHARACTERISTICS
      .filter(c => !taken.has(c))
      .filter(c => !talent.grantsCharacteristic || characteristics[c] < TALENT_CHARACTERISTIC_MAX)
      .map(c => ({
        value: c,
        label: CHARACTERISTIC_LABELS[c],
        note: talent.grantsCharacteristic ? `${characteristics[c]} → ${characteristics[c] + 1}` : undefined,
      }))
  }

  if (talent.choiceKind === 'skill') {
    const allowed = talent.choiceAllowedSkillKinds
    const seen = new Set<string>()
    return skills
      .filter(s => allowed.length === 0 || allowed.includes(s.kind))
      .filter(s => !taken.has(s.name.toLowerCase()))
      // Свой навык с тем же каноническим именем сервер не отличает от встроенного.
      .filter(s => {
        if (seen.has(s.name)) return false
        seen.add(s.name)
        return true
      })
      .map(s => ({ value: s.name, label: localizedName(s), group: s.kind }))
      .toSorted((a, b) => SKILL_KIND_ORDER.indexOf(a.group) - SKILL_KIND_ORDER.indexOf(b.group)
        || a.label.localeCompare(b.label, lang))
  }

  return []
}

const SIGNATURE_SPELL_SEPARATOR = '|'
const ordinal = (a: string, b: string) => (a < b ? -1 : a > b ? 1 : 0)

/** Конфигурация Signature Spell: действие и мультисет кодов дополнительных эффектов. */
export interface SignatureSpellConfiguration {
  action: string
  effects: string[]
}

/**
 * Стабильное значение выбора Signature Spell — «Действие|Эффект|Эффект…» по кодам `nameEn`.
 * Эффекты сортируются так же, как на сервере: порядок не важен, кратность важна.
 */
export function signatureSpellValue({ action, effects }: SignatureSpellConfiguration): string {
  return [action, ...effects.toSorted(ordinal)].join(SIGNATURE_SPELL_SEPARATOR)
}

export function parseSignatureSpell(value: string): SignatureSpellConfiguration | null {
  const parts = value.split(SIGNATURE_SPELL_SEPARATOR).map(part => part.trim())
  if (parts.length < 2 || parts.some(part => part.length === 0)) return null
  return { action: parts[0], effects: parts.slice(1).toSorted(ordinal) }
}

/** «Атака: Огонь, Дистанция ×2» — подпись конфигурации по справочнику магии или по кодам. */
export function signatureSpellLabel(
  configuration: SignatureSpellConfiguration,
  name: (code: string, parent?: string) => string = code => code,
): string {
  const counts = new Map<string, number>()
  for (const code of configuration.effects) counts.set(code, (counts.get(code) ?? 0) + 1)
  const effects = [...counts].map(([code, count]) => {
    const label = name(code, configuration.action)
    return count > 1 ? `${label} ×${count}` : label
  })
  return `${name(configuration.action)}: ${effects.join(', ')}`
}

/** Локализованное имя записи справочника магии по коду; родитель задаётся для эффектов. */
export function spellNameResolver(spells: Spell[]) {
  return (code: string, parent?: string) => {
    const entry = spells.find(s => s.nameEn === code
      && (parent === undefined ? s.kind === 'effect' : s.kind === 'additionalEffect' && s.parentEffect === parent))
    return entry ? t(entry.nameRu || entry.nameEn, entry.nameEn) : code
  }
}

/**
 * Подпись сохранённого выбора на языке интерфейса. Сервер хранит русский снимок имени, поэтому
 * в английском интерфейсе имя строится заново по стабильному значению.
 */
export function choiceLabel(choice: CharacterTalentChoice, skills: SkillDef[]): string {
  switch (choice.kind) {
    case 'characteristic': {
      const key = choice.value.toLowerCase() as Characteristic
      return CHARACTERISTICS.includes(key) ? CHARACTERISTIC_LABELS[key] : choice.displayName
    }
    case 'skill': {
      const skill = skills.find(s => s.name === choice.value)
      return skill ? localizedName(skill) : choice.displayName
    }
    case 'spellConfiguration': {
      const configuration = parseSignatureSpell(choice.value)
      // Коды действий и эффектов — их английские названия.
      return configuration ? t(choice.displayName, signatureSpellLabel(configuration)) : choice.displayName
    }
    default:
      return choice.displayName
  }
}
