import { useState } from 'react'
import type { CharacterSheet, Reference } from '../api/types'
import { t } from '../i18n'
import { readWorkshopMode, writeWorkshopMode, type WorkshopMode } from '../utils/uiPreferences'
import { AttachmentsTab } from './AttachmentsTab'
import { CraftingTab } from './CraftingTab'
import { Icon } from './Icon'

export function WorkshopTab(props: {
  sheet: CharacterSheet; reference: Reference; onError: (message: string) => void; refresh: () => Promise<void>
}) {
  const [mode, setMode] = useState<WorkshopMode>(() => readWorkshopMode(props.sheet.id))
  function select(next: WorkshopMode) { writeWorkshopMode(props.sheet.id, next); setMode(next) }
  return <div className="workshop-tab">
    <div className="sheet-mode-header">
      <div className="sheet-segment" role="group" aria-label={t('Режим мастерской', 'Workshop mode')}>
        <button aria-pressed={mode === 'upgrades'} onClick={() => select('upgrades')}><Icon name="adjustments" />{t('Улучшения', 'Upgrades')}</button>
        <button aria-pressed={mode === 'craft'} onClick={() => select('craft')}><Icon name="hammer" />{t('Ремесло', 'Crafting')}</button>
      </div>
      <span className="muted small-text">{mode === 'upgrades'
        ? t('Слоты оружия и брони, запас и лавка улучшений', 'Weapon and armor slots, spare attachments and shop')
        : t('Изготовление, варка зелий и зачарование', 'Crafting, brewing and enchanting')}</span>
    </div>
    {mode === 'upgrades' ? <AttachmentsTab {...props} /> : <CraftingTab {...props} />}
  </div>
}
