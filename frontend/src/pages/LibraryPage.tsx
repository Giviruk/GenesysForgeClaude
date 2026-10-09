import { useCallback, useEffect, useId, useState } from 'react'
import { api } from '../api/client'
import type { BaseCatalogEntry, CustomEntryType, GameSystem, HomebrewPackDocument, HomebrewPackListItem, LibraryEntry, LibraryProposal, Reference } from '../api/types'
import { localizedName, SYSTEM_LABELS } from '../utils/labels'
import { t } from '../i18n'
import { Icon } from '../components/Icon'
import { CustomEditedDate, HomebrewPackHistory } from '../components/HomebrewPackHistory'
import { SkillForm, TalentForm, ItemForm, ArchetypeForm, CareerForm, HeroicForm } from '../components/CustomContentForms'
import { CheckRow, DialogFrame, Drawer, FilterChip, PickerModal, SideNavRow, Switch, SystemBadge } from '../components/content/ContentUi'
import { BASE_CATEGORIES, CATEGORY_LABELS, EDITABLE_TYPES, ENTRY_LABELS, ENTRY_ICONS, ENTRY_TYPES } from '../components/content/contentLabels'
import { ContentProposalDialog } from '../components/content/ContentProposalDialog'
import { PROPOSAL_LABELS } from '../components/content/contentLabels'
import { contentKey, contentMeta, type PickerRow } from '../utils/contentLibrary'

export function LibraryPage() {
  const [system, setSystem] = useState<GameSystem>('genesysCore')
  const [entries, setEntries] = useState<LibraryEntry[] | null>(null)
  const [packs, setPacks] = useState<HomebrewPackListItem[]>([])
  const [proposals, setProposals] = useState<LibraryProposal[]>([])
  const [view, setView] = useState('all')
  const [type, setType] = useState<CustomEntryType | 'all'>('all')
  const [search, setSearch] = useState('')
  const [packTab, setPackTab] = useState<'entries' | 'exclusions'>('entries')
  const [exclusions, setExclusions] = useState<BaseCatalogEntry[]>([])
  const [error, setError] = useState<string | null>(null)
  const [shareLink, setShareLink] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [editor, setEditor] = useState<{ entry?: LibraryEntry } | null>(null)
  const [packEditor, setPackEditor] = useState<{ pack?: HomebrewPackListItem } | null>(null)
  const [membership, setMembership] = useState<LibraryEntry | null>(null)
  const [picker, setPicker] = useState<{ mode: 'entries' | 'exclusions'; rows: PickerRow[] } | null>(null)
  const [history, setHistory] = useState<HomebrewPackListItem | null>(null)
  const [importing, setImporting] = useState(false)
  const [proposal, setProposal] = useState<{ entry?: LibraryEntry; pack?: HomebrewPackListItem } | null>(null)
  const pack = packs.find(x => x.id === view && x.system === system)
  const refresh = useCallback(async () => {
    const [nextEntries, nextPacks, nextProposals] = await Promise.all([api.library(), api.homebrewPacks(), api.libraryProposals()])
    setEntries(nextEntries); setPacks(nextPacks); setProposals(nextProposals)
  }, [])
  useEffect(() => {
    let cancelled = false
    Promise.all([api.library(), api.homebrewPacks(), api.libraryProposals()]).then(([e, p, statuses]) => {
      if (!cancelled) { setEntries(e); setPacks(p); setProposals(statuses) }
    }).catch(err => { if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка', 'Error')) })
    return () => { cancelled = true }
  }, [])
  const packId = pack?.id, packUpdatedAt = pack?.updatedAt
  useEffect(() => {
    if (!packId) return
    let cancelled = false
    api.homebrewPackExclusions(packId).then(rows => { if (!cancelled) setExclusions(rows) })
      .catch(err => { if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка', 'Error')) })
    return () => { cancelled = true }
  }, [packId, packUpdatedAt])
  async function run(action: () => Promise<unknown>, message?: string) {
    setBusy(true); setError(null)
    try { await action(); await refresh(); if (message) setNotice(message); return true }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка', 'Error')); return false }
    finally { setBusy(false) }
  }
  const systemEntries = (entries ?? []).filter(x => x.system === system)
  const systemPacks = packs.filter(x => x.system === system)
  const baseEntries = systemEntries.filter(x => view === 'all' || (view === 'loose' ? !x.packIds.length : x.packIds.includes(view)))
  const rows = baseEntries.filter(x => (type === 'all' || x.entryType === type) && localizedName(x).toLocaleLowerCase().includes(search.toLocaleLowerCase()))
  function select(next: string) { setView(next); setPackTab(packs.find(p => p.id === next)?.entryCount === 0 && (packs.find(p => p.id === next)?.exclusionCount ?? 0) > 0 ? 'exclusions' : 'entries'); setType('all'); setSearch(''); setNotice(null) }
  async function openPicker(mode: 'entries' | 'exclusions') {
    if (!pack) return
    if (mode === 'entries') setPicker({ mode, rows: systemEntries.filter(x => !x.packIds.includes(pack.id)).map(x => ({
      key: contentKey(x.entryType, x.id), group: x.entryType, groupLabel: ENTRY_LABELS[x.entryType], name: localizedName(x), meta: contentMeta(x.meta), editedAt: x.lastEditedAt,
    })) })
    else {
      const catalog = await api.baseCatalog(system)
      setPicker({ mode, rows: catalog.filter(x => !exclusions.some(e => e.category === x.category && e.key === x.key)).map(x => ({
        key: contentKey(x.category, x.key), group: x.category, groupLabel: CATEGORY_LABELS[x.category], name: localizedName(x), meta: contentMeta(x.meta),
      })) })
    }
  }
  async function removeEntry(entry: LibraryEntry) {
    if (!window.confirm(t(`Удалить «${localizedName(entry)}» из библиотеки?`, `Delete "${localizedName(entry)}" from the library?`))) return
    const deletes = { skill: api.deleteCustomSkill, talent: api.deleteCustomTalent, item: api.deleteCustomItem,
      archetype: api.deleteCustomArchetype, career: api.deleteCustomCareer, heroicAbility: api.deleteCustomHeroicAbility }
    if (entry.entryType in deletes) await run(() => deletes[entry.entryType as keyof typeof deletes](undefined, entry.id))
  }
  return <div className="page rd-library">
    <header className="rd-page-head"><div><h2>{t('Моя библиотека', 'My library')}</h2>
      <p>{t('Кастомный контент аккаунта. Элемент может входить в несколько наборов или подключаться к кампании отдельно.', 'Account custom content. An entry can belong to several packs or connect to a campaign individually.')}</p></div>
      <div className="rd-actions"><button onClick={() => setImporting(true)}><Icon name="file-import" />{t('Импорт JSON', 'Import JSON')}</button>
        <button onClick={() => setPackEditor({})}><Icon name="book" />{t('Новый набор', 'New pack')}</button>
        <button className="primary" onClick={() => setEditor({})}><Icon name="plus" />{t('Создать контент', 'Create content')}</button></div>
    </header>
    {error && <p className="error" role="alert">{error}</p>}{notice && <p className="notice" role="status">{notice}</p>}
    <div className="rd-toolbar">{(['genesysCore', 'realmsOfTerrinoth'] as GameSystem[]).map(x => <FilterChip key={x} active={system === x}
      onClick={() => { setSystem(x); select('all') }}>{SYSTEM_LABELS[x]} <small>{entries?.filter(e => e.system === x).length ?? 0}</small></FilterChip>)}</div>
    {!entries ? <p>{t('Загрузка…', 'Loading…')}</p> : <div className="rd-two-panel">
      <nav className="rd-sidebar" aria-label={t('Библиотека', 'Library')}><h4>{t('Контент', 'Content')}</h4>
        <SideNavRow active={view === 'all'} onClick={() => select('all')} count={systemEntries.length}>{t('Все элементы', 'All entries')}</SideNavRow>
        <SideNavRow active={view === 'loose'} onClick={() => select('loose')} count={systemEntries.filter(x => !x.packIds.length).length}>{t('Вне наборов', 'Unpacked')}</SideNavRow>
        <h4>{t('Наборы', 'Packs')}</h4>{systemPacks.map(x => <SideNavRow key={x.id} active={view === x.id} onClick={() => select(x.id)} count={x.entryCount}>{x.name}</SideNavRow>)}
        <button className="rd-dashed" onClick={() => setPackEditor({})}>+ {t('Новый набор', 'New pack')}</button>
        <p className="hint">{t('Наборы добавляют свой контент и задают ограничения книг для кампаний.', 'Packs add custom content and define book restrictions for campaigns.')}</p>
      </nav><main className="rd-workspace">
        {pack && <section className="rd-pack-head"><div className="rd-page-head"><div><h3>{pack.name}</h3><p>{pack.description}</p>
          <div className="rd-chips"><span className="badge">{t(`${pack.entryCount} элементов · ${pack.exclusionCount} ограничений`, `${pack.entryCount} entries · ${pack.exclusionCount} exclusions`)}</span>
            {pack.campaigns?.map(c => <a key={c.id} className="badge" href={`/campaigns/${c.id}`}>{c.name}</a>)}
            {!pack.campaigns?.length && <span className="hint">{t('Не подключён к кампаниям', 'Not connected to campaigns')}</span>}
          </div></div><div className="rd-actions"><button disabled={busy} onClick={() => void openPicker(packTab === 'entries' ? 'entries' : 'exclusions').catch(err => setError(String(err)))}>
            <Icon name="plus" />{packTab === 'entries' ? t('Добавить из библиотеки', 'Add from library') : t('Убрать элементы книг', 'Exclude book entries')}</button>
            <button onClick={() => setPackEditor({ pack })}>{t('Изменить', 'Edit')}</button>
            <button aria-label={t('Удалить набор', 'Delete pack')} disabled={busy} onClick={() => { if (window.confirm(t('Удалить набор? Его элементы останутся в библиотеке.', 'Delete the pack? Its entries remain in the library.'))) void run(() => api.deleteHomebrewPack(pack.id)).then(ok => { if (ok) select('all') }) }}><Icon name="trash" /></button>
          </div></div><div className="rd-toolbar"><Switch checked={pack.isEnabledByDefault} label={t('Доступен своим персонажам', 'Available to own characters')}
            disabled={busy} onChange={on => void run(() => api.setHomebrewPackDefault(pack.id, on))} /><span>{t('Доступен своим персонажам вне кампании', 'Available to own characters outside campaigns')}</span>
            <button disabled={busy} onClick={() => void run(async () => { const shared = await api.shareHomebrewPack(pack.id); const url = new URL(shared.path, window.location.origin).href; setShareLink(url) })}><Icon name="share" />{t('Ссылка', 'Share link')}</button>
            <button disabled={busy} onClick={() => void run(async () => downloadJson(await api.exportHomebrewPack(pack.id), pack.name))}>{t('Экспорт JSON', 'Export JSON')}</button>
            <button onClick={() => setHistory(pack)}>{t('История', 'History')}</button><button onClick={() => setProposal({ pack })}>{t('Предложить мастеру', 'Propose to GM')}</button>
          </div>{proposals.filter(x => x.kind === 'pack' && x.targetId === pack.id).map(x => <span className={`badge rd-proposal-${x.status}`} key={x.campaignId}>{x.campaignName}: {PROPOSAL_LABELS[x.status]}</span>)}<div className="rd-toolbar"><FilterChip active={packTab === 'entries'} onClick={() => setPackTab('entries')}>{t('Свой контент', 'Custom entries')}</FilterChip>
            <FilterChip active={packTab === 'exclusions'} onClick={() => setPackTab('exclusions')}>{t('Ограничения книг', 'Book exclusions')}</FilterChip></div>
          {pack.exclusionCount > 0 && <p className="hint">{t('Ограничения действуют только в кампании с подключённым набором.', 'Exclusions apply only in campaigns with the pack connected.')}</p>}
        </section>}
        {pack && packTab === 'exclusions' ? <div className="rd-section-card"><p className="hint">{t('Элементы книг, которые набор отключает в кампании.', 'Book entries this pack disables in a campaign.')}</p>
          {!exclusions.length && <p className="rd-empty">{t('Ограничений пока нет.', 'No exclusions yet.')}</p>}
          {BASE_CATEGORIES.map(cat => { const items = exclusions.filter(x => x.category === cat); return items.length ? <section key={cat}><h4>{SYSTEM_LABELS[pack.system]} · {CATEGORY_LABELS[cat]} <small>{items.length}</small></h4>
            {items.map(x => <div className="rd-content-row rd-exclusion-row" key={x.key}><div><strong>{localizedName(x)}</strong><small>{contentMeta(x.meta)}</small></div>
              <button disabled={busy} aria-label={t(`Вернуть ${localizedName(x)}`, `Restore ${localizedName(x)}`)} onClick={() => void run(() => api.changeHomebrewPackExclusions(pack.id, [{ category: cat, key: x.key }], true))}>×</button></div>)}
          </section> : null })}</div> : <>
          <div className="rd-toolbar"><div className="rd-chips"><FilterChip active={type === 'all'} onClick={() => setType('all')}>{t('Все', 'All')} {baseEntries.length}</FilterChip>
            {ENTRY_TYPES.filter(x => baseEntries.some(e => e.entryType === x)).map(x => <FilterChip key={x} active={type === x} onClick={() => setType(x)}>{ENTRY_LABELS[x]} <small>{baseEntries.filter(e => e.entryType === x).length}</small></FilterChip>)}</div>
            <input type="search" aria-label={t('Поиск в библиотеке', 'Search library')} placeholder={t('Поиск…', 'Search…')} value={search} onChange={e => setSearch(e.target.value)} /></div>
          {!rows.length && <div className="rd-empty rd-dashed">{t('Подходящих элементов нет. Создайте контент или добавьте его из библиотеки.', 'No matching entries. Create content or add it from the library.')}</div>}
          <div className="rd-entry-list">{rows.map(entry => <article key={entry.id} className="rd-content-row"><span className="rd-entry-icon"><Icon name={ENTRY_ICONS[entry.entryType]} /></span>
            <div className="rd-entry-description"><strong>{localizedName(entry)}</strong><span className="muted small-text"> · {ENTRY_LABELS[entry.entryType]}</span>
              <small>{contentMeta(entry.meta)} <CustomEditedDate at={entry.lastEditedAt} /></small>
              {proposals.filter(x => x.kind === 'item' && x.targetId === entry.id).map(x => <small key={x.campaignId}>{x.campaignName} · {PROPOSAL_LABELS[x.status]}</small>)}
            </div><div className="rd-chips">{entry.packIds.filter(id => id !== pack?.id).map(id => <button className="rd-filter-chip" key={id} onClick={() => select(id)}>{packs.find(x => x.id === id)?.name}</button>)}
              {!entry.packIds.length && <span className="badge">{t('Вне наборов', 'Unpacked')}</span>}</div>
            <div className="rd-actions">{pack && <button disabled={busy} aria-label={t('Убрать из набора', 'Remove from pack')} onClick={() => void run(() => api.changeHomebrewPackEntries(pack.id, [{ entryType: entry.entryType, entryId: entry.id }], true))}>−</button>}
              <button aria-label={t('Выбрать наборы', 'Select packs')} onClick={() => setMembership(entry)}><Icon name="book" /></button>
              {EDITABLE_TYPES.includes(entry.entryType) && <button onClick={() => setEditor({ entry })}>{t('Изменить', 'Edit')}</button>}
              <details className="rd-row-menu"><summary aria-label={t('Действия с элементом', 'Entry actions')}>⋯</summary><div>
                <button onClick={() => setProposal({ entry })}>{t('Предложить мастеру', 'Propose to GM')}</button>
                {EDITABLE_TYPES.includes(entry.entryType) && <button onClick={() => void removeEntry(entry)}>{t('Удалить', 'Delete')}</button>}
              </div></details></div>
          </article>)}</div>
        </>}
      </main>
    </div>}
    {editor && <ContentEditor key={editor.entry?.id ?? system} system={system} entry={editor.entry} initialPack={pack?.id} packs={systemPacks} onClose={() => setEditor(null)} refresh={refresh} />}
    {packEditor && <PackEditor system={system} pack={packEditor.pack} onClose={() => setPackEditor(null)} onSave={async (name, description) => {
      if (packEditor.pack) await api.updateHomebrewPack(packEditor.pack.id, name, description)
      else { const created = await api.createHomebrewPack(name, description, system); select(created.id) }
      await refresh()
    }} />}
    {membership && <DialogFrame title={t('Входит в наборы', 'Pack membership')} onClose={() => setMembership(null)} busy={busy}>
      <p>{localizedName(membership)}</p>{!systemPacks.length && <p>{t('Сначала создайте набор.', 'Create a pack first.')}</p>}
      {systemPacks.map(p => <CheckRow key={p.id} label={p.name} checked={entries?.find(e => e.id === membership.id)?.packIds.includes(p.id) ?? false} disabled={busy}
        onChange={on => void run(() => api.changeHomebrewPackEntries(p.id, [{ entryType: membership.entryType, entryId: membership.id }], !on))} />)}
    </DialogFrame>}
    {picker && pack && <PickerModal actionLabel={picker.mode === 'entries' ? t('Добавить','Add') : t('Убрать','Exclude')} hint={picker.mode === 'entries' ? t('Элемент может входить в несколько наборов одновременно.','An entry can belong to multiple packs.') : t('Эти элементы книг станут недоступны в кампаниях с подключённым набором.','These book entries will be unavailable in campaigns using the pack.')} title={picker.mode === 'entries' ? t('Добавить из библиотеки', 'Add from library') : t('Ограничения книг', 'Book exclusions')} rows={picker.rows} onClose={() => setPicker(null)} onConfirm={async keys => {
      if (picker.mode === 'entries') await api.changeHomebrewPackEntries(pack.id, systemEntries.filter(x => keys.includes(contentKey(x.entryType, x.id))).map(x => ({ entryType: x.entryType, entryId: x.id })))
      else { const catalog = await api.baseCatalog(system); await api.changeHomebrewPackExclusions(pack.id, catalog.filter(x => keys.includes(contentKey(x.category, x.key))).map(x => ({ category: x.category, key: x.key }))) }
      await refresh()
    }} />}
    {shareLink && <DialogFrame title={t('Ссылка на набор','Pack link')} onClose={() => setShareLink(null)}><p>{t('Мастер может подключить исходный набор по этой ссылке.','The GM can connect the original pack using this link.')}</p><input aria-label={t('Ссылка на набор','Pack link')} value={shareLink} readOnly onFocus={e => e.currentTarget.select()} /><button onClick={() => void navigator.clipboard.writeText(shareLink).then(() => setNotice(t('Ссылка скопирована.','Link copied.'))).catch(() => setNotice(t('Скопируйте ссылку из поля.','Copy the link from the field.')))}>{t('Копировать','Copy')}</button></DialogFrame>}
    {history && <HomebrewPackHistory packId={history.id} name={history.name} onClose={() => setHistory(null)} />}
    {importing && <ImportPackDialog onClose={() => setImporting(false)} onImport={async doc => { const imported = await api.importHomebrewPack(doc); await refresh(); select(imported.id); setSystem(doc.system); setNotice(imported.warnings?.join('\n') || t('Набор импортирован.', 'Pack imported.')) }} />}
    {proposal && <ContentProposalDialog system={system} name={proposal.pack?.name ?? localizedName(proposal.entry!)} proposals={proposals} refresh={refresh} onClose={() => setProposal(null)}
      proposal={proposal.pack ? { packId: proposal.pack.id } : { entryType: proposal.entry!.entryType, entryId: proposal.entry!.id }} />}
  </div>
}

function ContentEditor({ system, entry, initialPack, packs, onClose, refresh }: { system: GameSystem; entry?: LibraryEntry; initialPack?: string; packs: HomebrewPackListItem[]; onClose: () => void; refresh: () => Promise<void> }) {
  const [type, setType] = useState<CustomEntryType>(entry?.entryType ?? 'skill')
  const [packIds, setPackIds] = useState(entry?.packIds ?? (initialPack ? [initialPack] : []))
  const [reference, setReference] = useState<Reference | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const formId = useId()
  useEffect(() => { let cancelled = false; api.reference(system, { library: true }).then(r => { if (!cancelled) setReference(r) })
    .catch(err => { if (!cancelled) setError(String(err)) }); return () => { cancelled = true } }, [system])
  async function run(action: () => Promise<unknown>) {
    setBusy(true); setError(null)
    try { await action(); await refresh(); return true }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка', 'Error')); return false }
    finally { setBusy(false) }
  }
  const props = { system, run, onDone: onClose, packIds, formId }
  return <Drawer title={entry ? t('Редактировать контент', 'Edit content') : t('Создать контент', 'Create content')} onClose={onClose} busy={busy} footer={<>
    {entry && <button className="danger rd-delete-entry" disabled={busy} onClick={() => {
      if (!window.confirm(t(`Удалить «${localizedName(entry)}» из библиотеки?`, `Delete "${localizedName(entry)}" from the library?`))) return
      const deletes = { skill:api.deleteCustomSkill, talent:api.deleteCustomTalent, item:api.deleteCustomItem, archetype:api.deleteCustomArchetype, career:api.deleteCustomCareer, heroicAbility:api.deleteCustomHeroicAbility }
      const action = deletes[entry.entryType as keyof typeof deletes]
      if (action) void run(() => action(undefined,entry.id)).then(ok => { if (ok) onClose() })
    }}>{t('Удалить','Delete')}</button>}
    <button disabled={busy} onClick={onClose}>{t('Отмена', 'Cancel')}</button><button type="submit" form={formId} className="primary" disabled={busy || !reference}>{entry ? t('Сохранить', 'Save') : t('Создать', 'Create')}</button>
  </>}>
    <SystemBadge system={system} />{error && <p className="error" role="alert">{error}</p>}
    {!entry && <div className="rd-chips">{EDITABLE_TYPES.filter(x => system === 'realmsOfTerrinoth' || x !== 'heroicAbility').map(x => <FilterChip key={x} active={type === x} disabled={busy} onClick={() => setType(x)}>{ENTRY_LABELS[x]}</FilterChip>)}</div>}
    {!reference ? <p>{t('Загрузка…', 'Loading…')}</p> : <fieldset disabled={busy} className="rd-form-fields">
      {type === 'skill' && <SkillForm {...props} editing={reference.skills.find(x => x.id === entry?.id) ?? null} />}
      {type === 'talent' && <TalentForm {...props} editing={reference.talents.find(x => x.id === entry?.id) ?? null} />}
      {type === 'item' && <ItemForm {...props} reference={reference} editing={reference.items.find(x => x.id === entry?.id) ?? null} />}
      {type === 'archetype' && <ArchetypeForm {...props} editing={reference.archetypes.find(x => x.id === entry?.id) ?? null} />}
      {type === 'career' && <CareerForm {...props} reference={reference} editing={reference.careers.find(x => x.id === entry?.id) ?? null} />}
      {type === 'heroicAbility' && <HeroicForm {...props} editing={reference.heroicAbilities.find(x => x.id === entry?.id) ?? null} />}
    </fieldset>}
    <h4>{t('Входит в наборы', 'Pack membership')}</h4><div className="rd-chips">{packs.map(p => <FilterChip key={p.id} active={packIds.includes(p.id)} disabled={busy}
      onClick={() => setPackIds(prev => prev.includes(p.id) ? prev.filter(id => id !== p.id) : [...prev, p.id])}>{p.name}</FilterChip>)}</div>
    <p className="hint">{t('Необязательно. Элемент вне наборов можно подключить к кампании отдельно.', 'Optional. An unpacked entry can connect to a campaign individually.')}</p>
  </Drawer>
}
function PackEditor({ system, pack, onClose, onSave }: { system: GameSystem; pack?: HomebrewPackListItem; onClose: () => void; onSave: (name: string, description: string) => Promise<void> }) {
  const [name, setName] = useState(pack?.name ?? '')
  const [description, setDescription] = useState(pack?.description ?? '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const id = useId()
  return <DialogFrame title={pack ? t('Изменить набор', 'Edit pack') : t('Новый набор', 'New pack')} onClose={onClose} busy={busy} footer={<>
    <button disabled={busy} onClick={onClose}>{t('Отмена', 'Cancel')}</button><button form={id} type="submit" className="primary" disabled={busy || !name.trim()}>{t('Сохранить', 'Save')}</button>
  </>}><SystemBadge system={system} />{error && <p className="error">{error}</p>}<form id={id} className="custom-form" onSubmit={e => {
    e.preventDefault(); setBusy(true); void onSave(name, description).then(onClose).catch(err => setError(String(err))).finally(() => setBusy(false))
  }}><label>{t('Название', 'Name')}<input required maxLength={200} value={name} disabled={busy} onChange={e => setName(e.target.value)} /></label>
    <label>{t('Описание', 'Description')}<textarea maxLength={2000} value={description} disabled={busy} onChange={e => setDescription(e.target.value)} /></label></form>
  </DialogFrame>
}
function ImportPackDialog({ onClose, onImport }: { onClose: () => void; onImport: (doc: HomebrewPackDocument) => Promise<void> }) {
  const [json, setJson] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  return <DialogFrame title={t('Импорт JSON', 'Import JSON')} onClose={onClose} busy={busy} footer={<>
    <button disabled={busy} onClick={onClose}>{t('Отмена', 'Cancel')}</button><button className="primary" disabled={busy || !json.trim()} onClick={() => {
      setBusy(true); setError(null); void Promise.resolve().then(() => onImport(JSON.parse(json) as HomebrewPackDocument)).then(onClose).catch(err => setError(String(err))).finally(() => setBusy(false))
    }}>{t('Импортировать', 'Import')}</button>
  </>}><p className="hint">{t('Принимаются наборы v1 и v2. Импорт создаёт копии элементов в вашей библиотеке.', 'Accepts v1 and v2 packs. Import creates copies in your library.')}</p>
    {error && <p className="error" role="alert">{error}</p>}<label>{t('Файл JSON', 'JSON file')}<input type="file" accept="application/json,.json" disabled={busy} onChange={e => { const file = e.target.files?.[0]; if (file) void file.text().then(setJson).catch(err => setError(String(err))) }} /></label>
    <label>{t('Содержимое JSON', 'JSON content')}<textarea rows={12} value={json} disabled={busy} onChange={e => setJson(e.target.value)} /></label>
  </DialogFrame>
}
function downloadJson(document: HomebrewPackDocument, name: string) {
  const url = URL.createObjectURL(new Blob([JSON.stringify(document, null, 2)], { type: 'application/json' }))
  const a = window.document.createElement('a'); a.href = url; a.download = `${name.replace(/[^\p{L}\p{N}_-]/gu, '-')}.json`; a.click(); URL.revokeObjectURL(url)
}
