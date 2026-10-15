import { Fragment } from 'react'
import type { GameSystem } from '../api/types'
import { t } from '../i18n'
import { SHEET_TABS, type CharacterSheetTab } from '../utils/uiPreferences'

export function CharacterSheetNavigation({ system, tab, onSelect, bioLabel = t('Образ и заметки', 'Bio and notes') }: {
  system: GameSystem
  tab: CharacterSheetTab
  onSelect: (tab: CharacterSheetTab) => void
  bioLabel?: string
}) {
  const labels: Record<CharacterSheetTab, string> = {
    sheet: t('Лист', 'Sheet'), talents: t('Таланты', 'Talents'), inventory: t('Инвентарь', 'Inventory'),
    magic: t('Магия', 'Magic'), heroic: t('Героика', 'Heroic'), workshop: t('Мастерская', 'Workshop'),
    transport: t('Транспорт', 'Transport'), bio: bioLabel, history: t('История', 'History'),
  }
  const tabs = SHEET_TABS.filter(key => key !== 'heroic' || system === 'realmsOfTerrinoth')
  return <>
    <div className="tabs main-tabs">
      {tabs.map((key, index) => <Fragment key={key}>
        {index === 4 && <span className="sheet-tab-divider" aria-hidden="true" />}
        <button className={`${index >= 4 ? 'sheet-secondary-tab ' : ''}tab${tab === key ? ' active' : ''}`}
          onClick={() => onSelect(key)}>{labels[key]}</button>
      </Fragment>)}
    </div>
    <div className="sheet-tab-select-wrap">
      <label className="visually-hidden" htmlFor="sheet-tab-select">{t('Раздел листа', 'Sheet section')}</label>
      <select id="sheet-tab-select" className="sheet-tab-select" value={tab}
        onChange={event => onSelect(event.target.value as CharacterSheetTab)}>
        {tabs.map(key => <option key={key} value={key}>{labels[key]}</option>)}
      </select>
    </div>
  </>
}
