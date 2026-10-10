import { useEffect, useId, useRef, useState, type ReactNode } from 'react'
import type { GameSystem } from '../../api/types'
import { t } from '../../i18n'
import { Icon, type IconName } from '../Icon'
import { SYSTEM_LABELS } from '../../utils/labels'
import { filterPickerRows, toggleSelection, type PickerRow } from '../../utils/contentLibrary'

export function SystemBadge({ system }: { system: GameSystem }) {
  return <span className={`rd-system-badge ${system}`}>{SYSTEM_LABELS[system]}</span>
}
export function Switch({ checked, onChange, label, disabled = false }: {
  checked: boolean; onChange: (next: boolean) => void; label: string; disabled?: boolean
}) {
  return <button type="button" role="switch" aria-checked={checked} aria-label={label}
    className="rd-switch" disabled={disabled} onClick={e => { e.stopPropagation(); onChange(!checked) }}><span /></button>
}
export function FilterChip({ active, onClick, children, disabled = false }: {
  active: boolean; onClick: () => void; children: ReactNode; disabled?: boolean
}) {
  return <button type="button" aria-pressed={active} disabled={disabled}
    className={`rd-filter-chip${active ? ' active' : ''}`} onClick={onClick}>{children}</button>
}
export function SideNavRow({ active, onClick, children, count, icon = 'book' }: {
  active: boolean; onClick: () => void; children: ReactNode; count?: number; icon?: IconName
}) {
  return <button type="button" aria-current={active ? 'page' : undefined}
    className={`rd-side-row${active ? ' active' : ''}`} onClick={onClick}>
    <Icon name={icon} /><span>{children}</span>{count !== undefined && <b>{count}</b>}
  </button>
}
export function CheckRow({ checked, onChange, label, meta, disabled, suffix, className = '' }: {
  checked: boolean; onChange: (checked: boolean) => void; label: string; meta?: ReactNode; disabled?: boolean; suffix?: ReactNode; className?: string
}) {
  return <div className={`rd-check-row${checked ? ' selected' : ''} ${className}`}>
    <label><input type="checkbox" checked={checked} disabled={disabled} onChange={e => onChange(e.target.checked)} />
      <span><strong>{label}</strong>{meta && <small>{meta}</small>}</span></label>{suffix}
  </div>
}
export function SectionCard({ title, icon = 'book', subtitle, children, actions, className = '', id }: {
  title: ReactNode; icon?: IconName; subtitle?: ReactNode; children: ReactNode; actions?: ReactNode; className?: string; id?: string
}) {
  return <section id={id} className={`rd-section-card ${className}`}><header><Icon name={icon} />
    <div><h3>{title}</h3>{subtitle && <p>{subtitle}</p>}</div>{actions && <div className="rd-actions">{actions}</div>}
  </header>{children}</section>
}
export function DialogFrame({ title, children, footer, onClose, drawer = false, busy = false }: {
  title: string; children: ReactNode; footer?: ReactNode; onClose: () => void; drawer?: boolean; busy?: boolean
}) {
  const ref = useRef<HTMLDialogElement>(null)
  const heading = useId()
  useEffect(() => {
    const dialog = ref.current!
    const previous = document.activeElement as HTMLElement | null
    if (dialog.showModal && !dialog.open) dialog.showModal()
    else dialog.setAttribute('open', '')
    return () => { dialog.close?.(); previous?.focus() }
  }, [])
  return <dialog ref={ref} aria-labelledby={heading} className={`rd-dialog${drawer ? ' rd-drawer' : ''}`}
    onCancel={e => { e.preventDefault(); if (!busy) onClose() }}
    onClick={e => { if (e.target === e.currentTarget && !busy) onClose() }}>
    <div className="rd-dialog-inner"><header><h3 id={heading}>{title}</h3>
      <button type="button" className="rd-icon-button" aria-label={t('Закрыть', 'Close')} disabled={busy} onClick={onClose}><Icon name="close" /></button>
    </header><div className="rd-dialog-body">{children}</div>{footer && <footer>{footer}</footer>}</div>
  </dialog>
}
export function Drawer(props: Omit<Parameters<typeof DialogFrame>[0], 'drawer'>) { return <DialogFrame {...props} drawer /> }

export function PickerModal({ title, rows, onConfirm, onClose, busy = false, hint, actionLabel = t('Добавить', 'Add') }: {
  title: string; rows: PickerRow[]; onConfirm: (keys: string[]) => Promise<void>; onClose: () => void; busy?: boolean; hint?: string; actionLabel?: string
}) {
  const [group, setGroup] = useState('all')
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<'type' | 'name' | 'recent'>('type')
  const [selected, setSelected] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const visible = filterPickerRows(rows, group, search, sort)
  const groups = [...new Set(rows.map(x => x.group))]
  const allChecked = visible.length > 0 && visible.every(x => selected.includes(x.key))
  async function save() {
    setSaving(true); setError(null)
    try { await onConfirm(selected); onClose() }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка', 'Error')) }
    finally { setSaving(false) }
  }
  return <DialogFrame title={title} onClose={onClose} busy={busy || saving} footer={<>
    <span className="muted">{t(`Выбрано: ${selected.length}`, `Selected: ${selected.length}`)}</span>
    <button disabled={saving} onClick={onClose}>{t('Отмена', 'Cancel')}</button>
    <button className="primary" disabled={busy || saving || !selected.length} onClick={() => void save()}>{actionLabel} ({selected.length})</button>
  </>}>
    {hint && <p className="hint">{hint}</p>}{error && <p className="error" role="alert">{error}</p>}
    <div className="rd-picker-layout"><nav aria-label={t('Типы контента', 'Content types')}>
      <SideNavRow active={group === 'all'} onClick={() => setGroup('all')} count={rows.length}>{t('Все', 'All')}</SideNavRow>
      {groups.map(g => <SideNavRow key={g} active={group === g} onClick={() => setGroup(g)} count={rows.filter(x => x.group === g).length}>
        {rows.find(x => x.group === g)!.groupLabel}</SideNavRow>)}
    </nav><div className="rd-picker-main"><div className="rd-toolbar">
      <input type="search" aria-label={t('Поиск контента', 'Search content')} placeholder={t('Поиск по названию…', 'Search by name…')} value={search} onChange={e => setSearch(e.target.value)} />
      <select aria-label={t('Сортировка', 'Sort')} value={sort} onChange={e => setSort(e.target.value as typeof sort)}>
        <option value="type">{t('По типу', 'By type')}</option><option value="name">{t('По имени', 'By name')}</option><option value="recent">{t('По изменению', 'Recently edited')}</option>
      </select></div>
      <CheckRow label={allChecked ? t('Снять видимые', 'Deselect visible') : t(`Выбрать видимые (${visible.length})`, `Select visible (${visible.length})`)} checked={allChecked} disabled={!visible.length || saving}
        onChange={on => setSelected(prev => toggleSelection(prev, visible.map(x => x.key), on))} />
      {!visible.length && <p className="rd-empty">{rows.length ? t('Ничего не найдено по фильтру.','No matches for this filter.') : t('Всё уже добавлено.','Everything is already added.')}</p>}
      {[...new Set(visible.map(x => x.group))].map(g => {
        const items = visible.filter(x => x.group === g)
        return <div key={g}>{sort === 'type' && group === 'all' && <CheckRow label={items[0].groupLabel} checked={items.every(x => selected.includes(x.key))}
          onChange={on => setSelected(prev => toggleSelection(prev, items.map(x => x.key), on))} disabled={saving} />}
          {items.map(row => <CheckRow key={row.key} label={row.name} meta={row.meta} checked={selected.includes(row.key)} disabled={saving}
            onChange={on => setSelected(prev => toggleSelection(prev, [row.key], on))} />)}</div>
      })}
    </div></div>
  </DialogFrame>
}
