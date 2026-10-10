import type { Characteristic } from '../api/types'
import { t } from '../i18n'
import { CHARACTERISTIC_LABELS, magicSkillLabel } from '../utils/labels'
import type { MagicSkillPool } from './MagicBuilder'
import { FilterChip } from './content/ContentUi'

export function MagicDirectionChips({ skills, activeSkill, onChange, characterSkills }: {
  skills: string[]; activeSkill: string; onChange: (value: string) => void; characterSkills?: MagicSkillPool[]
}) {
  return <div className="magic-directions"><small>{t('Направление', 'School')}</small><div className="sheet-chips">
    {skills.map(skill => {
      const own = characterSkills?.find(s => s.name === skill)
      const hint = characterSkills ? own ? t(`Ваш навык: ${CHARACTERISTIC_LABELS[own.characteristic as Characteristic] ?? own.characteristic} ${own.characteristicValue} · ранг ${own.ranks}`, `Your skill: ${CHARACTERISTIC_LABELS[own.characteristic as Characteristic] ?? own.characteristic} ${own.characteristicValue} · rank ${own.ranks}`) : t('Навыка нет у персонажа', 'The character has no such skill') : undefined
      return <span key={skill} title={hint}><FilterChip active={activeSkill === skill} onClick={() => onChange(skill)}>{magicSkillLabel(skill)}
        {characterSkills && <i className={`magic-skill-dot ${own ? own.isCareer ? 'career' : 'owned' : 'missing'}`} />}</FilterChip></span>
    })}
  </div></div>
}
