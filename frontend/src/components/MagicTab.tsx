import { useMemo, useState } from 'react'
import type { CharacterSheet } from '../api/types'
import { SpellsTab } from './SpellsTab'
import { t } from '../i18n'
import { MagicBuilder, type MagicSkillPool } from './MagicBuilder'

/**
 * Вкладка «Магия» листа персонажа: переключатель между справочником эффектов и
 * сборщиком магического действия. Сборщику передаются магические навыки персонажа с пулами кубов.
 */
export function MagicTab({ sheet, onError }: { sheet: CharacterSheet; onError: (m: string) => void }) {
  const [mode, setMode] = useState<'reference' | 'builder'>('builder')

  const magicSkills = useMemo<MagicSkillPool[]>(
    () => sheet.skills.filter(s => s.kind === 'magic').map(s => ({ name: s.name, pool: s.pool })),
    [sheet.skills])

  return (
    <div>
      <div className="system-switch">
        <button className={mode === 'builder' ? 'tab active' : 'tab'} onClick={() => setMode('builder')}>{t('Сборка действия', 'Build action')}</button>
        <button className={mode === 'reference' ? 'tab active' : 'tab'} onClick={() => setMode('reference')}>{t('Справочник', 'Reference')}</button>
      </div>
      {mode === 'builder'
        ? <MagicBuilder system={sheet.system} characterSkills={magicSkills} onError={onError} />
        : <SpellsTab system={sheet.system} onError={onError} />}
    </div>
  )
}
