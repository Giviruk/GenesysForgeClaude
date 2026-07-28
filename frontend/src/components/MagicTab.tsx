import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet } from '../api/types'
import { SpellsTab } from './SpellsTab'
import { localizedName } from '../utils/labels'
import { t } from '../i18n'
import { MagicBuilder, type BuilderImplement, type MagicSkillPool } from './MagicBuilder'

/**
 * Вкладка «Магия» листа персонажа: переключатель между справочником эффектов и
 * сборщиком магического действия. Сборщику передаются магические навыки персонажа с пулами кубов.
 */
export function MagicTab({ sheet, onError, refresh }: {
  sheet: CharacterSheet
  onError: (m: string) => void
  /** Перечитать лист после настройки инструмента; без неё выбор эффектов не предлагается. */
  refresh?: () => Promise<void>
}) {
  const [mode, setMode] = useState<'reference' | 'builder'>('builder')

  const magicSkills = useMemo<MagicSkillPool[]>(
    () => sheet.skills.filter(s => s.kind === 'magic').map(s => ({ name: s.name, pool: s.pool })),
    [sheet.skills])

  // Инструмент работает только в руках: лежащий в рюкзаке фолиант не помогает (ROT-MAG-IMP-01).
  // Сломанным он тоже не работает — как и любой предмет с серьёзным повреждением.
  const implementsInHand = useMemo<BuilderImplement[]>(
    () => sheet.items
      .filter(i => i.implement != null && i.state === 'equipped' && i.isUsable)
      .map(i => ({ itemId: i.id, name: localizedName(i), implement: i.implement! })),
    [sheet.items])

  return (
    <div>
      <div className="system-switch">
        <button className={mode === 'builder' ? 'tab active' : 'tab'} onClick={() => setMode('builder')}>{t('Сборка действия', 'Build action')}</button>
        <button className={mode === 'reference' ? 'tab active' : 'tab'} onClick={() => setMode('reference')}>{t('Справочник', 'Reference')}</button>
      </div>
      {mode === 'builder'
        ? <MagicBuilder system={sheet.system} characterSkills={magicSkills}
          implements={implementsInHand} onError={onError}
          onConfigureImplement={refresh
            ? async (itemId, codes) => {
              await api.setImplementConfiguration(sheet.id, itemId, codes)
              await refresh()
            }
            : undefined} />
        : <SpellsTab system={sheet.system} onError={onError} />}
    </div>
  )
}
