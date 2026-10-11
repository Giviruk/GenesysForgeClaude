import { useEffect, useRef } from 'react'
import type { BestiaryFilters as Filters } from '../../utils/bestiary'
import { EMPTY_FILTERS } from '../../utils/bestiary'
import { NPC_KIND_LABELS, NPC_KINDS, NPC_ROLE_LABELS, NPC_ROLES, SYSTEM_LABELS } from '../../utils/labels'
import type { GameSystem } from '../../api/types'
import { t } from '../../i18n'
import { Icon } from '../Icon'

export function BestiaryFilters({ filters, count, onChange, onClose }: {
  filters: Filters; count: number; onChange: (f: Filters) => void; onClose: () => void
}) {
  const dialog = useRef<HTMLDialogElement>(null)
  const touch = useRef<number | null>(null)
  useEffect(() => {
    const element = dialog.current
    const previous = document.activeElement as HTMLElement | null
    if (element?.showModal) element.showModal()
    else element?.setAttribute('open', '')
    return () => { element?.close?.(); previous?.focus() }
  }, [])
  const set = <K extends keyof Filters>(key: K, value: Filters[K]) => onChange({ ...filters, [key]: value })
  return <dialog ref={dialog} className="bt-filters-dialog" aria-labelledby="bt-filter-title" onCancel={onClose}
    onClick={e => { if (e.target === dialog.current) { const r = dialog.current.getBoundingClientRect(); if (e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom) onClose() } }}>
    <div className="bt-sheet-handle" onTouchStart={e => { touch.current = e.touches[0].clientY }} onTouchEnd={e => { if (touch.current !== null && e.changedTouches[0].clientY - touch.current > 60) onClose(); touch.current = null }} />
    <div className="bt-filter-head"><h3 id="bt-filter-title">{t('Фильтры', 'Filters')}</h3><button type="button" onClick={() => onChange({ ...EMPTY_FILTERS })}>{t('Сбросить', 'Reset')}</button><button type="button" className="bt-icon" aria-label={t('Закрыть фильтры', 'Close filters')} onClick={onClose}><Icon name="close" /></button></div>
    <fieldset><legend>{t('Система', 'System')}</legend><div className="bt-pills">
      {(['', 'realmsOfTerrinoth', 'genesysCore'] as const).map(system => <button key={system} type="button" aria-pressed={filters.system === system} onClick={() => set('system', system as '' | GameSystem)}>{system ? SYSTEM_LABELS[system] : t('Все', 'All')}</button>)}
    </div></fieldset>
    <fieldset><legend>{t('Тип', 'Type')}</legend><div className="bt-pills">{NPC_KINDS.map(kind => <button key={kind} type="button" aria-pressed={filters.kinds.includes(kind)} onClick={() => set('kinds', filters.kinds.includes(kind) ? filters.kinds.filter(k => k !== kind) : [...filters.kinds, kind])}>{NPC_KIND_LABELS[kind]}</button>)}</div></fieldset>
    <fieldset><legend>{t('Роль', 'Role')}</legend><div className="bt-pills"><button type="button" aria-pressed={!filters.role} onClick={() => set('role', '')}>{t('Все', 'All')}</button>{NPC_ROLES.map(role => <button key={role} type="button" aria-pressed={filters.role === role} onClick={() => set('role', role)}>{NPC_ROLE_LABELS[role]}</button>)}</div></fieldset>
    <fieldset><legend>{t('Источник', 'Source')}</legend><div className="bt-pills">{(['', 'mine', 'builtin'] as const).map(source => <button key={source} type="button" aria-pressed={filters.source === source} onClick={() => set('source', source)}>{source === 'mine' ? t('Мои', 'Mine') : source === 'builtin' ? t('Встроенные', 'Built-in') : t('Все', 'All')}</button>)}</div></fieldset>
    <fieldset><legend>{t('Сортировка', 'Sort')}</legend><div className="bt-pills">{(['createdAt', 'name'] as const).map(sort => <button key={sort} type="button" aria-pressed={filters.sort === sort} onClick={() => set('sort', sort)}>{sort === 'name' ? t('По имени', 'By name') : t('Сначала новые', 'Newest first')}</button>)}</div></fieldset>
    <button type="button" className="primary bt-show-results" onClick={onClose}>{t(`Показать ${count} NPC`, `Show ${count} NPCs`)}</button>
  </dialog>
}
