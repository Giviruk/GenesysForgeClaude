import type {
  AllowedState, AttachmentDef, Characteristic, ContentEntryType, CreatureTemplate, EncounterType, GameSystem, HouseRuleCategory,
  HeroicOriginType, ImplementMaterial, InitiativeSlotType, ItemDamageState, SignatureWeaponProfile, WeaponCraftsmanship, WeaponFormTrait, ItemKind, ItemState, NpcCombatStyle, NpcKind, NpcPowerLevel, NpcRole,
  NpcVisibility, ParticipantType, SkillKind, TalentCategory, ThreatLevel, TransportKind, MovementMode,
} from '../api/types'
import { repeatsProperties } from '../data/itemQualities'
import { t } from '../i18n'

export const SYSTEM_LABELS: Record<GameSystem, string> = {
  genesysCore: 'Genesys Core',
  realmsOfTerrinoth: 'Realms of Terrinoth',
}

export const CHARACTERISTIC_LABELS: Record<Characteristic, string> = t({
  brawn: 'Мощь',
  agility: 'Ловкость',
  intellect: 'Интеллект',
  cunning: 'Хитрость',
  willpower: 'Воля',
  presence: 'Харизма',
}, {
  brawn: 'Brawn',
  agility: 'Agility',
  intellect: 'Intellect',
  cunning: 'Cunning',
  willpower: 'Willpower',
  presence: 'Presence',
})

export const CHARACTERISTICS: Characteristic[] = [
  'brawn', 'agility', 'intellect', 'cunning', 'willpower', 'presence',
]

/** Короткие подписи характеристик для узких таблиц. */
export const CHARACTERISTIC_SHORT_LABELS: Record<Characteristic, string> = t({
  brawn: 'Мощ',
  agility: 'Лов',
  intellect: 'Инт',
  cunning: 'Хит',
  willpower: 'Вол',
  presence: 'Хар',
}, {
  brawn: 'Br',
  agility: 'Ag',
  intellect: 'Int',
  cunning: 'Cun',
  willpower: 'Will',
  presence: 'Pr',
})

export const SKILL_KIND_LABELS: Record<SkillKind, string> = t({
  general: 'Общие',
  combat: 'Боевые',
  social: 'Социальные',
  knowledge: 'Знания',
  magic: 'Магия',
}, {
  general: 'General',
  combat: 'Combat',
  social: 'Social',
  knowledge: 'Knowledge',
  magic: 'Magic',
})

export const TALENT_CATEGORY_LABELS: Record<TalentCategory, string> = t({
  general: 'Общие',
  social: 'Социальные',
  combat: 'Боевые',
  magic: 'Магические',
}, {
  general: 'General',
  social: 'Social',
  combat: 'Combat',
  magic: 'Magic',
})

export const TALENT_CATEGORIES: TalentCategory[] = ['general', 'social', 'combat', 'magic']

export const ITEM_KIND_LABELS: Record<ItemKind, string> = t({
  weapon: 'Оружие',
  armor: 'Броня',
  gear: 'Снаряжение',
}, {
  weapon: 'Weapon',
  armor: 'Armor',
  gear: 'Gear',
})

export const ITEM_STATE_LABELS: Record<ItemState, string> = t({
  equipped: 'Используется',
  carried: 'Не используется',
  backpack: 'В рюкзаке',
}, {
  equipped: 'Equipped',
  carried: 'Carried',
  backpack: 'In backpack',
})

/** Нейтральная подпись валюты («монеты»). */
export const CURRENCY_LABEL = t('монеты', 'coins')

/** Основное имя контента: русское в RU-интерфейсе, оригинальное (английское) — в EN. */
export const localizedName = (value: { name: string; nameRu?: string | null }) =>
  t(value.nameRu?.trim() || value.name, value.name.trim() || value.nameRu?.trim() || '')

/**
 * Описание контента на языке интерфейса: в EN-режиме — английский парафраз (descriptionEn),
 * с откатом на русский (полное описание или safe-парафраз), если перевода нет.
 */
export const localizedDescription = (value: {
  description?: string | null
  safeDescription?: string | null
  descriptionEn?: string | null
}): string => {
  const ru = value.description?.trim() || value.safeDescription?.trim() || ''
  return t(ru, value.descriptionEn?.trim() || ru)
}

/**
 * Описание предмета — или пустая строка, если оно всего лишь пересказывает список свойств.
 *
 * У части записей каталога в описание попал тот же список качеств, который уже показан тегами с
 * тултипами: «Высококритичное 1» и там, и там. Второй раз его читать незачем, а записи с настоящим
 * описанием («Магический длинный лук с высоким качеством и увеличенной дальностью») остаются.
 */
export const itemDescription = (item: {
  description?: string | null
  safeDescription?: string | null
  descriptionEn?: string | null
  properties?: string | null
}): string => {
  const text = localizedDescription(item)
  return repeatsProperties(text, item.properties) ? '' : text
}

/**
 * Вторичное (оригинальное/английское) название для RU/ENG отображения.
 * Пустая строка, если оно совпадает с основным (нечего дублировать) или отсутствует.
 */
export const secondaryName = (value: { name: string; nameRu?: string | null }): string => {
  const primary = localizedName(value).trim()
  const original = value.name.trim()
  return original && original.toLowerCase() !== primary.toLowerCase() ? original : ''
}

/**
 * Однострочный RU/ENG формат для option/плоского текста: «Ближний бой / Melee».
 * Для разметки со стилями используйте пару localizedName + secondaryName.
 */
export const dualName = (value: { name: string; nameRu?: string | null }): string => {
  const original = secondaryName(value)
  return original ? `${localizedName(value)} / ${original}` : localizedName(value)
}

/**
 * Подбирает навык листа для броска оружием. Оружие хранит англ. имя навыка
 * (например, «Melee (Light)»), но в Genesys Core навык называется просто «Melee» —
 * поэтому при отсутствии точного совпадения пробуем базовое имя без скобок.
 */
export function resolveWeaponSkillName(weaponSkill: string, skillNames: string[]): string | null {
  if (!weaponSkill) return null
  if (skillNames.includes(weaponSkill)) return weaponSkill
  const base = weaponSkill.replace(/\s*\(.*\)\s*/, '').trim()
  if (base && skillNames.includes(base)) return base
  return null
}

/** Подписи магических навыков (направлений магии). Ключ — стабильный код из seed. */
export const MAGIC_SKILL_LABELS: Record<string, string> = t({
  Arcana: 'Тайная (Arcana)',
  Divine: 'Божественная (Divine)',
  Primal: 'Природная (Primal)',
  Runes: 'Руны (Runes)',
  Verse: 'Песнь (Verse)',
}, {
  Arcana: 'Arcana',
  Divine: 'Divine',
  Primal: 'Primal',
  Runes: 'Runes',
  Verse: 'Verse',
})

/** Подпись магического навыка с запасным вариантом для кастомных кодов. */
export const magicSkillLabel = (skill: string) => MAGIC_SKILL_LABELS[skill] ?? skill

/** Подписи уровней сложности проверки Genesys (число фиолетовых кубов). */
export const DIFFICULTY_LABELS: Record<number, string> = t({
  0: 'Простая',
  1: 'Лёгкая',
  2: 'Средняя',
  3: 'Сложная',
  4: 'Трудная',
  5: 'Грозная',
}, {
  0: 'Simple',
  1: 'Easy',
  2: 'Average',
  3: 'Hard',
  4: 'Daunting',
  5: 'Formidable',
})

/** Подпись уровня сложности с ограничением 0..5. */
export const difficultyLabel = (n: number) => DIFFICULTY_LABELS[Math.max(0, Math.min(5, n))] ?? `${n}`

/**
 * Извлекает числовое значение сложности из строки справочника магии:
 * базовый эффект — «2 (Average)» → 2; доп. эффект — «+1» → 1. Пусто/нечисло → 0.
 */
export const parseDifficulty = (raw: string): number => {
  const m = raw.match(/-?\d+/)
  return m ? parseInt(m[0], 10) : 0
}

/** Потолок итоговой сложности магического действия по правилам Genesys. */
export const MAX_SPELL_DIFFICULTY = 5

/**
 * Итоговая сложность магического действия без потолка: базовый эффект + сумма
 * выбранных дополнительных эффектов. Строки «2 (Average)» и «+1» считаются parseDifficulty.
 */
export const spellDifficulty = (baseDifficulty: string, additional: string[]): number =>
  parseDifficulty(baseDifficulty) + additional.reduce((sum, d) => sum + parseDifficulty(d), 0)

/** true, если добавление эффекта с данной сложностью превысит потолок 5. */
export const wouldExceedSpellCap = (baseDifficulty: string, chosen: string[], candidate: string): boolean =>
  spellDifficulty(baseDifficulty, chosen) + parseDifficulty(candidate) > MAX_SPELL_DIFFICULTY

export const NPC_KIND_LABELS: Record<NpcKind, string> = t({
  minion: 'Миньон',
  rival: 'Ривал',
  nemesis: 'Немезида',
}, {
  minion: 'Minion',
  rival: 'Rival',
  nemesis: 'Nemesis',
})

/**
 * Снаряжение, идущее вместе со скакуном (ROT-MOUNT-ITEM-01). Каталог хранит коды, подписи живут
 * здесь: неизвестный код показывается как есть, а не прячется.
 */
export const MOUNT_GEAR_LABELS: Record<string, string> = t({
  'harness': 'упряжь',
  'riding-tack': 'верховая сбруя',
}, {
  'harness': 'harness',
  'riding-tack': 'riding tack',
})

export const mountGearLabel = (code: string): string => MOUNT_GEAR_LABELS[code] ?? code

/** Скакун или транспортное средство (ROT-TRANSPORT-01). */
export const TRANSPORT_KIND_LABELS: Record<TransportKind, string> = t({
  mount: 'Скакун',
  vehicle: 'Транспортное средство',
}, {
  mount: 'Mount',
  vehicle: 'Vehicle',
})

/** Режим движения: числовой скорости книга этим профилям не даёт. */
export const MOVEMENT_MODE_LABELS: Record<MovementMode, string> = t({
  ground: 'по земле',
  flight: 'по воздуху',
  wheeled: 'на колёсах',
}, {
  ground: 'ground',
  flight: 'flight',
  wheeled: 'wheeled',
})

export const NPC_ROLE_LABELS: Record<NpcRole, string> = t({
  brute: 'Громила',
  skirmisher: 'Застрельщик',
  archer: 'Стрелок',
  caster: 'Маг',
  leader: 'Командир',
  social: 'Интриган',
  support: 'Поддержка',
  monster: 'Монстр',
  custom: 'Особая',
}, {
  brute: 'Brute',
  skirmisher: 'Skirmisher',
  archer: 'Archer',
  caster: 'Caster',
  leader: 'Leader',
  social: 'Schemer',
  support: 'Support',
  monster: 'Monster',
  custom: 'Custom',
})

export const NPC_VISIBILITY_LABELS: Record<NpcVisibility, string> = t({
  private: 'Приватный',
  campaignVisible: 'Виден в кампании',
  publicTemplate: 'Публичный шаблон',
}, {
  private: 'Private',
  campaignVisible: 'Visible in campaign',
  publicTemplate: 'Public template',
})

export const NPC_POWER_LABELS: Record<NpcPowerLevel, string> = t({
  weak: 'Слабый',
  standard: 'Обычный',
  strong: 'Сильный',
  elite: 'Элитный',
}, {
  weak: 'Weak',
  standard: 'Standard',
  strong: 'Strong',
  elite: 'Elite',
})

export const NPC_COMBAT_STYLE_LABELS: Record<NpcCombatStyle, string> = t({
  melee: 'Ближний бой',
  ranged: 'Дальний бой',
  magic: 'Магия',
  social: 'Социальный',
}, {
  melee: 'Melee',
  ranged: 'Ranged',
  magic: 'Magic',
  social: 'Social',
})

export const CREATURE_TEMPLATE_LABELS: Record<CreatureTemplate, string> = t({
  none: 'Без шаблона (гуманоид)',
  undead: 'Нежить',
  beast: 'Зверь',
  dragon: 'Дракон',
  demon: 'Демон',
  construct: 'Конструкт',
}, {
  none: 'No template (humanoid)',
  undead: 'Undead',
  beast: 'Beast',
  dragon: 'Dragon',
  demon: 'Demon',
  construct: 'Construct',
})
export const CREATURE_TEMPLATES: CreatureTemplate[] = ['none', 'undead', 'beast', 'dragon', 'demon', 'construct']

export const NPC_KINDS: NpcKind[] = ['minion', 'rival', 'nemesis']
export const NPC_ROLES: NpcRole[] = [
  'brute', 'skirmisher', 'archer', 'caster', 'leader', 'social', 'support', 'monster', 'custom',
]

export const PARTICIPANT_TYPE_LABELS: Record<ParticipantType, string> = t({
  playerCharacter: 'Персонаж',
  npc: 'NPC',
  minionGroup: 'Группа миньонов',
  hazard: 'Осложнение',
}, {
  playerCharacter: 'Player character',
  npc: 'NPC',
  minionGroup: 'Minion group',
  hazard: 'Hazard',
})

export const SLOT_TYPE_LABELS: Record<InitiativeSlotType, string> = t({
  player: 'Игроки',
  npc: 'NPC',
  neutral: 'Нейтрал',
}, {
  player: 'Players',
  npc: 'NPC',
  neutral: 'Neutral',
})

export const ENCOUNTER_TYPE_LABELS: Record<EncounterType, string> = t({
  combat: 'Бой',
  social: 'Социальный',
  exploration: 'Исследование',
  chase: 'Погоня',
  investigation: 'Расследование',
  travel: 'Путешествие',
  hazard: 'Опасность',
  mixed: 'Смешанный',
  custom: 'Особый',
}, {
  combat: 'Combat',
  social: 'Social',
  exploration: 'Exploration',
  chase: 'Chase',
  investigation: 'Investigation',
  travel: 'Travel',
  hazard: 'Hazard',
  mixed: 'Mixed',
  custom: 'Custom',
})

export const ENCOUNTER_TYPES: EncounterType[] = [
  'combat', 'social', 'exploration', 'chase', 'investigation', 'travel', 'hazard', 'mixed', 'custom',
]

export const THREAT_LEVEL_LABELS: Record<ThreatLevel, string> = t({
  trivial: 'Тривиальный',
  easy: 'Лёгкий',
  standard: 'Стандартный',
  hard: 'Тяжёлый',
  deadly: 'Смертельный',
}, {
  trivial: 'Trivial',
  easy: 'Easy',
  standard: 'Standard',
  hard: 'Hard',
  deadly: 'Deadly',
})

export const THREAT_LEVELS: ThreatLevel[] = ['trivial', 'easy', 'standard', 'hard', 'deadly']

export const CONTENT_ENTRY_TYPE_LABELS: Record<ContentEntryType, string> = t({
  archetype: 'Архетип',
  career: 'Карьера',
  skill: 'Навык',
  talent: 'Талант',
  item: 'Предмет',
  heroicAbility: 'Геройская способность',
  spell: 'Заклинание',
  magicAction: 'Магическое действие',
  alchemyRecipe: 'Алхимический рецепт',
  rune: 'Руна',
  houseRule: 'Домашнее правило',
  customNote: 'Заметка',
}, {
  archetype: 'Archetype',
  career: 'Career',
  skill: 'Skill',
  talent: 'Talent',
  item: 'Item',
  heroicAbility: 'Heroic ability',
  spell: 'Spell',
  magicAction: 'Magic action',
  alchemyRecipe: 'Alchemy recipe',
  rune: 'Rune',
  houseRule: 'House rule',
  customNote: 'Note',
})

export const CONTENT_ENTRY_TYPES: ContentEntryType[] = [
  'talent', 'item', 'career', 'archetype', 'skill', 'heroicAbility',
  'spell', 'magicAction', 'alchemyRecipe', 'rune', 'houseRule', 'customNote',
]

export const ALLOWED_STATE_LABELS: Record<AllowedState, string> = t({
  allowed: 'Разрешено',
  disallowed: 'Запрещено',
  askGm: 'С разрешения мастера',
}, {
  allowed: 'Allowed',
  disallowed: 'Disallowed',
  askGm: 'Ask the GM',
})

export const ALLOWED_STATES: AllowedState[] = ['allowed', 'disallowed', 'askGm']

export const HOUSE_RULE_CATEGORY_LABELS: Record<HouseRuleCategory, string> = t({
  none: '—',
  characterCreation: 'Создание персонажа',
  combat: 'Бой',
  magic: 'Магия',
  equipment: 'Снаряжение',
  xp: 'Опыт (XP)',
  campaignTone: 'Тон кампании',
  custom: 'Особая',
}, {
  none: '—',
  characterCreation: 'Character creation',
  combat: 'Combat',
  magic: 'Magic',
  equipment: 'Equipment',
  xp: 'Experience (XP)',
  campaignTone: 'Campaign tone',
  custom: 'Custom',
})

export const HOUSE_RULE_CATEGORIES: HouseRuleCategory[] = [
  'characterCreation', 'combat', 'magic', 'equipment', 'xp', 'campaignTone', 'custom',
]

/** Стоимость таланта тира N — 5 × N XP. */
export const talentCost = (tier: number) => tier * 5

/** Эффективный тир следующего ранга рангового таланта (каждый ранг — на тир выше, максимум 5). */
export const nextRankTier = (baseTier: number, ranksOwned: number) =>
  Math.min(baseTier + ranksOwned, 5)

/**
 * Категории происхождения героической способности (ROT-HA-01). Порядок совпадает
 * с гранями d10 1–9, поэтому список можно показывать вместе с номером грани.
 */
export const HEROIC_ORIGIN_TYPES: HeroicOriginType[] = [
  'bloodline', 'destiny', 'artifact', 'patron', 'purpose',
  'lifeChangingEvent', 'blessingOrCurse', 'training', 'wildMagic',
]

export const HEROIC_ORIGIN_LABELS: Record<HeroicOriginType, string> = t({
  bloodline: 'Наследственная сила или особая кровь',
  destiny: 'Избранность судьбой или пророчеством',
  artifact: 'Сила, связанная с артефактом',
  patron: 'Покровительство невидимой сверхъестественной силы',
  purpose: 'Исключительная внутренняя цель: долг, клятва или месть',
  lifeChangingEvent: 'Единственный преобразивший жизнь опыт',
  blessingOrCurse: 'Благословение либо проклятие',
  training: 'Уникальная многолетняя подготовка',
  wildMagic: 'Воздействие неконтролируемой магии',
}, {
  bloodline: 'Inherited power or special blood',
  destiny: 'Chosen by fate or prophecy',
  artifact: 'Power tied to an artifact',
  patron: 'Patronage of an unseen supernatural power',
  purpose: 'An exceptional inner purpose: duty, oath or revenge',
  lifeChangingEvent: 'A single life-changing experience',
  blessingOrCurse: 'A blessing or a curse',
  training: 'Unique lifelong training',
  wildMagic: 'Exposure to uncontrolled magic',
})

/** Грань d10, соответствующая категории: индекс в таблице + 1. */
export const heroicOriginFace = (type: HeroicOriginType) => HEROIC_ORIGIN_TYPES.indexOf(type) + 1

export const SIGNATURE_WEAPON_PROFILES: SignatureWeaponProfile[] = ['brawl', 'oneHanded', 'twoHanded', 'ranged']

export const SIGNATURE_WEAPON_PROFILE_LABELS: Record<SignatureWeaponProfile, string> = t({
  brawl: 'Рукопашный',
  oneHanded: 'Одноручный',
  twoHanded: 'Двуручный',
  ranged: 'Дальнобойный',
}, {
  brawl: 'Brawl',
  oneHanded: 'One-handed',
  twoHanded: 'Two-handed',
  ranged: 'Ranged',
})

/** Порядок выбора (ROT-WPN-02): от обычной работы к древней, как в таблице книги. */
export const WEAPON_CRAFTSMANSHIPS: WeaponCraftsmanship[] =
  ['steel', 'iron', 'dwarven', 'elven', 'ancient']

export const WEAPON_CRAFTSMANSHIP_LABELS: Record<WeaponCraftsmanship, string> = t({
  dwarven: 'Гномья работа',
  elven: 'Эльфийская работа',
  steel: 'Сталь',
  iron: 'Железо',
  ancient: 'Древняя работа',
}, {
  dwarven: 'Dwarven',
  elven: 'Elven',
  steel: 'Steel',
  iron: 'Iron',
  ancient: 'Ancient',
})

/**
 * Прилагательное для строки разбора поправок: «эльфийское · Вес 6 → 8». Отдельно от
 * <see cref="WEAPON_CRAFTSMANSHIP_LABELS"/>: там названия работы, а здесь свойство предмета.
 */
export const WEAPON_CRAFTSMANSHIP_ADJECTIVES: Record<WeaponCraftsmanship, string> = t({
  steel: 'стальное',
  iron: 'железное',
  dwarven: 'гномье',
  elven: 'эльфийское',
  ancient: 'древнее',
}, {
  steel: 'steel',
  iron: 'iron',
  dwarven: 'dwarven',
  elven: 'elven',
  ancient: 'ancient',
})

/** Краткая подсказка, что тип делает: игрок выбирает его при покупке и потом не меняет. */
export const WEAPON_CRAFTSMANSHIP_HINTS: Record<WeaponCraftsmanship, string> = t({
  steel: 'Числа таблицы без изменений',
  iron: 'Броня: вес +2 и помехи Атлетике, Координации, Верховой езде и Скрытности. Оружие: крит +1. Цена вдвое ниже, редкость −1',
  dwarven: 'Броня: вес +1, слот улучшений +1. Оружие: урон +1, вес +1. Цена вдвое выше, редкость +2',
  elven: 'Броня: вес −2, минус одна помеха Скрытности. Оружие: урон −1 и крит −1. Цена вдвое выше, редкость +3',
  ancient: 'Броня: поглощение +1, защита +1, укреплённая. Оружие: урон +1, крит −1, укреплённое. Слот улучшений −1, цена ×20, редкость 10',
}, {
  steel: 'Table values unchanged',
  iron: 'Armor: Enc +2 and setbacks to Athletics, Coordination, Riding and Stealth. Weapon: Crit +1. Half price, rarity −1',
  dwarven: 'Armor: Enc +1, one more hard point. Weapon: damage +1, Enc +1. Double price, rarity +2',
  elven: 'Armor: Enc −2, one less Stealth setback. Weapon: damage −1 and Crit −1. Double price, rarity +3',
  ancient: 'Armor: Soak +1, Defense +1, reinforced. Weapon: damage +1, Crit −1, reinforced. One less hard point, ×20 price, rarity 10',
})

/** Подписи характеристик в разборе поправок экземпляра (ROT-WPN-02). */
export const ITEM_STAT_FIELD_LABELS: Record<string, string> = t({
  encumbrance: 'Вес',
  soak: 'Поглощение',
  meleeDefense: 'Ближняя защита',
  rangedDefense: 'Дальняя защита',
  hardPoints: 'Слоты улучшений',
  price: 'Цена',
  rarity: 'Редкость',
  encumbranceThreshold: 'Порог веса',
}, {
  encumbrance: 'Encumbrance',
  soak: 'Soak',
  meleeDefense: 'Melee defense',
  rangedDefense: 'Ranged defense',
  hardPoints: 'Hard points',
  price: 'Price',
  rarity: 'Rarity',
  encumbranceThreshold: 'Encumbrance threshold',
})

/** Подписи материалов магического инструмента (ROT-MAG-MAT-01). */
export const IMPLEMENT_MATERIAL_LABELS: Record<ImplementMaterial, string> = t({
  oak: 'Дуб', bone: 'Кость', hazel: 'Орешник', willow: 'Ива', yew: 'Тис',
}, {
  oak: 'Oak', bone: 'Bone', hazel: 'Hazel', willow: 'Willow', yew: 'Yew',
})

/** Что даёт материал: короткой строкой в подсказке к выбору. */
export const IMPLEMENT_MATERIAL_HINTS: Record<ImplementMaterial, string> = t({
  oak: 'Цена и редкость без изменений, особого свойства нет',
  bone: 'После успешной Атаки или Проклятья лечит заклинателю 1 рану (раз за проверку). Цена ×1.5, редкость +2',
  hazel: 'При триумфе раз за проверку добавляет бросок бонусной кости; триумф остаётся. Цена ×1.5, редкость +1',
  willow: 'После успешного заклинания добавляет автоматическое преимущество (раз за проверку). Цена ×2, редкость +2',
  yew: 'После успешного Усиления, Барьера или Лечения снимает заклинателю 1 усталость. Цена ×1.5, редкость +1',
}, {
  oak: 'Price and rarity unchanged, no special property',
  bone: 'After a successful Attack or Curse, heals the caster 1 wound (once per check). Price ×1.5, rarity +2',
  hazel: 'With a Triumph, once per check roll one Boost and add its symbols; the Triumph remains. Price ×1.5, rarity +1',
  willow: 'After a successful spell, adds an automatic Advantage (once per check). Price ×2, rarity +2',
  yew: 'After a successful Augment, Barrier or Heal, heals the caster 1 strain. Price ×1.5, rarity +1',
})

/**
 * Что материал делает за столом — без цены и редкости. Отдельно от подсказки витрины, потому что
 * в памятке эти строки стоят рядом с посчитанными числами экземпляра, а не вместо них.
 * Приложение эти срабатывания не считает: им нужен рантайм столкновения.
 */
export const IMPLEMENT_MATERIAL_TRIGGERS: Record<ImplementMaterial, string> = t({
  oak: 'Особого свойства нет.',
  bone: 'После успешной Атаки или Проклятья заклинатель лечит 1 рану. Раз за проверку.',
  hazel: 'Если на проверке есть триумф, можно раз за проверку бросить одну бонусную кость и добавить её символы. Триумф при этом остаётся.',
  willow: 'После успешного заклинания добавляется одно автоматическое преимущество. Раз за проверку.',
  yew: 'После успешного Усиления, Барьера или Лечения заклинатель снимает 1 усталость. Раз за проверку.',
}, {
  oak: 'No special property.',
  bone: 'After a successful Attack or Curse the caster heals 1 wound. Once per check.',
  hazel: 'With a Triumph on the check, once per check roll one Boost die and add its symbols. The Triumph remains.',
  willow: 'After a successful spell, one automatic Advantage is added. Once per check.',
  yew: 'After a successful Augment, Barrier or Heal the caster heals 1 strain. Once per check.',
})

/** Порядок состояний повреждения для переключателя (GEN-EQP-DMG-01). */
export const ITEM_DAMAGE_STATES: ItemDamageState[] =
  ['undamaged', 'minor', 'moderate', 'major', 'destroyed']

export const ITEM_DAMAGE_STATE_LABELS: Record<ItemDamageState, string> = t({
  undamaged: 'Цел',
  minor: 'Незначительное',
  moderate: 'Умеренное',
  major: 'Серьёзное',
  destroyed: 'Уничтожено',
}, {
  undamaged: 'Undamaged',
  minor: 'Minor',
  moderate: 'Moderate',
  major: 'Major',
  destroyed: 'Destroyed',
})

/** Что состояние делает с предметом — короткой строкой под переключателем и в памятке. */
export const ITEM_DAMAGE_STATE_HINTS: Record<ItemDamageState, string> = t({
  undamaged: 'Штрафов нет, ремонт не нужен',
  minor: 'Одна помеха ко всем проверкам, прямо использующим предмет',
  moderate: 'Сложность таких проверок повышается на одну ступень',
  major: 'Пользоваться нельзя: ни атак, ни поглощения, ни защиты, ни улучшений. Вес и содержимое остаются',
  destroyed: 'Пользоваться нельзя, обычный ремонт недоступен — дальше решает ведущий',
}, {
  undamaged: 'No penalty, no repair needed',
  minor: 'One setback to every check that directly uses the item',
  moderate: 'The difficulty of such checks increases once',
  major: 'Unusable: no attacks, soak, defense or attachment effects. Weight and contents stay',
  destroyed: 'Unusable and beyond ordinary repair — the GM decides what happens next',
})

/**
 * Признаки формы, которые подтверждает GM. Группу профиля (brawl/oneHanded/twoHanded/ranged)
 * ставит сервер, поэтому в списке её нет.
 */
export const CONFIRMABLE_WEAPON_TRAITS: WeaponFormTrait[] = [
  'sword', 'bowOrCrossbow', 'bladed', 'bluntOrCrushing', 'hasCuttingEdge', 'woodenWorkingEdge',
]

export const WEAPON_TRAIT_LABELS: Record<WeaponFormTrait, string> = t({
  brawl: 'рукопашное',
  oneHanded: 'одноручное',
  twoHanded: 'двуручное',
  ranged: 'дальнобойное',
  sword: 'меч',
  bowOrCrossbow: 'лук или арбалет',
  bladed: 'клинковое',
  bluntOrCrushing: 'дробящее',
  hasCuttingEdge: 'есть режущая кромка',
  woodenWorkingEdge: 'деревянная рабочая кромка',
  plateArmor: 'латная',
  metalArmor: 'металлическая',
  hardenedPlate: 'закалённые латы',
}, {
  brawl: 'brawl',
  oneHanded: 'one-handed',
  twoHanded: 'two-handed',
  ranged: 'ranged',
  sword: 'sword',
  bowOrCrossbow: 'bow or crossbow',
  bladed: 'bladed',
  bluntOrCrushing: 'blunt or crushing',
  hasCuttingEdge: 'has a cutting edge',
  woodenWorkingEdge: 'wooden working edge',
  plateArmor: 'plate',
  metalArmor: 'metal',
  hardenedPlate: 'hardened plate',
})

/** Флаги формы приходят строкой «oneHanded, sword» — разбираем её в список. */
export const parseWeaponTraits = (value: string | null | undefined): WeaponFormTrait[] =>
  (value ?? '').split(',').map(x => x.trim()).filter(x => x && x !== 'none') as WeaponFormTrait[]

/** Обратная сборка: сервер читает тот же формат. */
export const formatWeaponTraits = (traits: WeaponFormTrait[]) =>
  traits.length === 0 ? 'none' : traits.join(', ')

/**
 * Признаки именного оружия так, как их достроит сервер: группу ставит профиль, меч всегда клинковый,
 * а у клинка есть режущая кромка. Нужно, чтобы список улучшений в сборке совпадал с тем, что примет
 * сервер (ROT-HA-02); проверяет он же.
 */
export function signatureWeaponTraits(
  profile: SignatureWeaponProfile, confirmed: WeaponFormTrait[],
): WeaponFormTrait[] {
  const traits = new Set<WeaponFormTrait>(confirmed.filter(
    x => x !== 'brawl' && x !== 'oneHanded' && x !== 'twoHanded' && x !== 'ranged'))
  traits.add(profile as WeaponFormTrait)
  if (traits.has('sword')) traits.add('bladed')
  if (traits.has('bladed')) traits.add('hasCuttingEdge')
  return [...traits]
}

/**
 * Улучшение подходит носителю по виду и признакам формы — то же правило, что и на сервере.
 * Повторено здесь только чтобы не предлагать заведомо невозможный выбор; решает сервер.
 * Признаки приходят полями записи, поэтому своей таблицы совместимости у клиента нет.
 */
export function isAttachmentCompatible(
  hostKind: string,
  traits: WeaponFormTrait[],
  def: Pick<AttachmentDef, 'hostKind' | 'requiredTraits' | 'requiredAnyTraits' | 'forbiddenTraits'>,
): boolean {
  if (hostKind !== def.hostKind) return false
  const has = (trait: WeaponFormTrait) => traits.includes(trait)
  const requiredAny = parseWeaponTraits(def.requiredAnyTraits)
  if (!parseWeaponTraits(def.requiredTraits).every(has)) return false
  if (requiredAny.length > 0 && !requiredAny.some(has)) return false
  return !parseWeaponTraits(def.forbiddenTraits).some(has)
}

/** Уровни улучшения Power героической способности. */
export const HEROIC_UPGRADE_LABELS: Record<number, string> =
  t({ 1: 'Улучшенная', 2: 'Высшая' }, { 1: 'Improved', 2: 'Supreme' })
