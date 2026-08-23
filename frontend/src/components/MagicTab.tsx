import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, Quality } from '../api/types'
import { SpellsTab } from './SpellsTab'
import { localizedName } from '../utils/labels'
import { t } from '../i18n'
import {
  MagicBuilder, type BuilderImplement, type BuilderShard, type MagicSkillPool,
} from './MagicBuilder'

/**
 * Вкладка «Магия» листа персонажа: переключатель между справочником эффектов и
 * сборщиком магического действия. Сборщику передаются магические навыки персонажа с пулами кубов.
 */
export function MagicTab({ sheet, onError, refresh, qualities }: {
  sheet: CharacterSheet
  onError: (m: string) => void
  /** Качества из уже загруженного справочника; standalone/campaign режимы используют fallback. */
  qualities?: Quality[]
  /** Перечитать лист после настройки инструмента; без неё выбор эффектов не предлагается. */
  refresh?: () => Promise<void>
}) {
  const [mode, setMode] = useState<'reference' | 'builder'>('builder')

  const magicSkills = useMemo<MagicSkillPool[]>(
    () => sheet.skills.filter(s => s.kind === 'magic').map(s => ({
      name: s.name,
      characteristic: s.characteristic,
      // Полный API-лист всегда содержит характеристики; fallback сохраняет работу старых
      // импортированных/тестовых частичных листов, но не пытается выводить рейтинг из пула.
      characteristicValue: sheet.characteristics?.[s.characteristic] ?? 0,
      pool: s.pool, ranks: s.ranks, isCareer: s.isCareer,
      setbackDice: s.setbackDice, boostDice: s.boostDice,
      difficultyDice: s.difficultyDice, difficultyUpgrades: s.difficultyUpgrades,
      removeBoosts: s.removeBoosts,
    })),
    [sheet.skills, sheet.characteristics])

  // Инструмент работает только в руках: лежащий в рюкзаке фолиант не помогает (ROT-MAG-IMP-01).
  // Сломанным он тоже не работает — как и любой предмет с серьёзным повреждением.
  const implementsInHand = useMemo<BuilderImplement[]>(
    () => sheet.items
      .filter(i => i.implement != null && i.state === 'equipped' && i.isUsable)
      .map(i => ({ itemId: i.id, name: localizedName(i), implement: i.implement! })),
    [sheet.items])
  const shardsInHand = useMemo<BuilderShard[]>(
    () => sheet.items
      .filter(i => i.shard != null && i.state === 'equipped' && i.isUsable)
      .map(i => ({ itemId: i.id, name: localizedName(i), shard: i.shard! })),
    [sheet.items])

  return (
    <div>
      <div className="system-switch">
        <button className={mode === 'builder' ? 'tab active' : 'tab'} onClick={() => setMode('builder')}>{t('Сборка действия', 'Build action')}</button>
        <button className={mode === 'reference' ? 'tab active' : 'tab'} onClick={() => setMode('reference')}>{t('Справочник', 'Reference')}</button>
      </div>
      {mode === 'builder'
        ? <MagicBuilder system={sheet.system} characterSkills={magicSkills}
          knowledgeRating={sheet.knowledgeRating} qualities={qualities} talents={sheet.talents}
          implements={implementsInHand} shards={shardsInHand} onError={onError}
          onConfigureImplement={refresh
            ? async (itemId, codes) => {
              await api.setImplementConfiguration(sheet.id, itemId, codes)
              await refresh()
            }
            : undefined}
          onConfigureLesserRune={refresh
            ? async (itemId, activation, actionCode, effectCode) => {
              await api.setLesserRuneConfiguration(sheet.id, itemId, activation, actionCode, effectCode)
              await refresh()
            }
            : undefined} />
        : <SpellsTab system={sheet.system} onError={onError} qualities={qualities} />}
    </div>
  )
}
