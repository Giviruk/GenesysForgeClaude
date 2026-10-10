import { useCallback, useEffect, useState } from 'react'
import { api } from '../../api/client'
import type { BaseContentCategory, CampaignBaseEntry, CampaignContent, CampaignHomebrewPack, GameSystem, HomebrewPackListItem, LibraryEntry } from '../../api/types'
import { localizedName, SYSTEM_LABELS } from '../../utils/labels'
import { t } from '../../i18n'
import { contentKey, contentMeta, type PickerRow } from '../../utils/contentLibrary'
import { HomebrewPackHistory, PackChangeStatus } from '../HomebrewPackHistory'
import { Icon } from '../Icon'
import { CheckRow, DialogFrame, FilterChip, PickerModal, SectionCard, SideNavRow, Switch, SystemBadge } from './ContentUi'
import { BASE_CATEGORIES, CATEGORY_LABELS, ENTRY_LABELS, ENTRY_ICONS } from './contentLabels'

type ContentSection = 'overview' | 'systems' | 'packs' | 'items'
interface ContentView { section: ContentSection; system: GameSystem; category: BaseContentCategory }
function readView(): ContentView {
  const p = new URLSearchParams(window.location.search)
  const section = p.get('section')
  const category = p.get('category')
  return { section: section === 'systems' || section === 'packs' || section === 'items' ? section : 'overview',
    system: p.get('system') === 'realmsOfTerrinoth' ? 'realmsOfTerrinoth' : 'genesysCore',
    category: BASE_CATEGORIES.includes(category as BaseContentCategory) ? category as BaseContentCategory : 'skill' }
}
const SECTION_LABELS = { overview: t('Сводка', 'Summary'), systems: t('Системы', 'Systems'), packs: t('Наборы', 'Packs'), items: t('Отдельные элементы', 'Individual entries') }
export function CampaignContentTab({ campaignId, refreshSignal = 0 }: { campaignId: string; refreshSignal?: number }) {
  const [view, setView] = useState(readView)
  const [data, setData] = useState<CampaignContent | null>(null)
  const [base, setBase] = useState<{ system: GameSystem; rows: CampaignBaseEntry[] } | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [search, setSearch] = useState('')
  const [source, setSource] = useState('all')
  const [bookSource, setBookSource] = useState<'all' | 'core' | 'rot'>('all')
  const [attentionOnly, setAttentionOnly] = useState(false)
  const [targetId, setTargetId] = useState<string | null>(null)
  const [history, setHistory] = useState<CampaignHomebrewPack | null>(null)
  const [picker, setPicker] = useState<{ kind: 'pack' | 'item'; rows: PickerRow[]; packs?: HomebrewPackListItem[]; items?: LibraryEntry[] } | null>(null)
  const [shared, setShared] = useState(false)
  const [sharedLink, setSharedLink] = useState('')
  const reload = useCallback(async () => setData(await api.campaignContent(campaignId)), [campaignId])
  useEffect(() => {
    let cancelled = false
    api.campaignContent(campaignId).then(x => { if (!cancelled) setData(x) }).catch(err => { if (!cancelled) setError(String(err)) })
    return () => { cancelled = true }
  }, [campaignId, refreshSignal])
  useEffect(() => {
    if (view.section !== 'systems') return
    let cancelled = false
    api.campaignBase(campaignId, view.system).then(rows => { if (!cancelled) setBase({ system: view.system, rows }) })
      .catch(err => { if (!cancelled) setError(String(err)) })
    return () => { cancelled = true }
  }, [campaignId, view.section, view.system, data, refreshSignal])
  useEffect(() => { const pop = () => setView(readView()); window.addEventListener('popstate', pop); return () => window.removeEventListener('popstate', pop) }, [])
  function change(next: Partial<ContentView>) {
    const value = { ...view, ...next }; setView(value); setSearch(''); setSource('all'); setBookSource('all')
    const p = new URLSearchParams({ section: value.section, system: value.system, category: value.category })
    window.history.replaceState(null, '', `${window.location.pathname}?${p}`)
  }
  async function run(action: () => Promise<unknown>) {
    setBusy(true); setError(null)
    try { await action(); await reload(); return true }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка', 'Error')); return false }
    finally { setBusy(false) }
  }
  async function openPicker(kind: 'pack' | 'item') {
    setError(null)
    try {
      if (kind === 'pack') {
        const packs = (await api.homebrewPacks()).filter(x => !data?.packs.some(p => p.id === x.id))
        setPicker({ kind, packs, rows: packs.map(x => ({ key: x.id, name: x.name, group: x.system, groupLabel: SYSTEM_LABELS[x.system], meta: `${SYSTEM_LABELS[x.system]} · ${t(`${x.entryCount} элементов · ${x.exclusionCount} ограничений`, `${x.entryCount} entries · ${x.exclusionCount} exclusions`)} · ${x.description}`, editedAt: x.updatedAt })) })
      } else {
        const items = (await api.library()).filter(x => !data?.items.some(p => p.entryType === x.entryType && p.entryId === x.id))
        setPicker({ kind, items, rows: items.map(x => ({ key: contentKey(x.entryType, x.id), name: localizedName(x), group: x.entryType, groupLabel: ENTRY_LABELS[x.entryType], meta: `${SYSTEM_LABELS[x.system]} · ${contentMeta(x.meta)}`, editedAt: x.lastEditedAt })) })
      }
    } catch (err) { setError(String(err)) }
  }
  if (!data) return <div>{error && <p className="error" role="alert">{error}</p>}<p>{t('Загрузка контента…', 'Loading content…')}</p></div>
  const selectedSystem = data.systems.find(x => x.system === view.system)!
  const categoryRows = base?.system === view.system ? base.rows.filter(x => x.category === view.category) : []
  const visibleBase = categoryRows.filter(x => (bookSource === 'all' || (bookSource === 'core' ? x.isSharedWithCore : !x.isSharedWithCore)) && (source === 'all' || x.source === source || (source === 'disabled' && !x.enabled))
    && `${localizedName(x)} ${x.meta}`.toLocaleLowerCase().includes(search.toLocaleLowerCase()))
  const packVisible = data.packs.filter(x => (!attentionOnly || x.changedAfterConnection || x.status === 'pending' || x.entries?.some(e => e.state === 'pending'))
    && `${x.name} ${x.ownerName}`.toLocaleLowerCase().includes(search.toLocaleLowerCase()))
  function jump(section: ContentSection, id?: string | null) {
    change({ section }); setAttentionOnly(false); setTargetId(id ?? null)
    if (id) window.setTimeout(() => document.getElementById(`rd-content-${id}`)?.scrollIntoView({ behavior:'smooth', block:'center' }), 100)
  }
  function setBaseEntries(rows: CampaignBaseEntry[], enabled: boolean) {
    const used = !enabled ? [...new Set(rows.flatMap(x => x.usedBy))] : []
    if (used.length && !window.confirm(t(`Контент используют: ${used.join(', ')}. Он останется в листах, новые покупки будут закрыты. Отключить?`, `Used by: ${used.join(', ')}. Existing choices remain; new purchases will be blocked. Disable?`))) return
    void run(() => api.setCampaignBase(campaignId, view.system, rows.map(x => ({ category: x.category, key: x.key, enabled }))))
  }
  return <div className="rd-campaign-content">
    {error && <p className="error" role="alert">{error}</p>}{notice && <p className="notice" role="status">{notice}</p>}
    <header className="rd-page-head"><div><h3>{t('Контент кампании','Campaign content')}</h3><p>{t('Что игроки могут выбрать при создании и развитии персонажей. Отключение не отнимает уже купленное — только закрывает новые покупки.','What players can choose when creating and developing characters. Disabling retains existing choices and blocks new purchases.')}</p></div><span>{t('Доступно игрокам:','Available to players:')} <b>{data.availableCount}</b> {t('элементов','entries')}</span></header>
    <nav className="rd-content-nav" aria-label={t('Разделы контента', 'Content sections')}>{(['overview', 'systems', 'packs', 'items'] as ContentSection[]).map(section => <button key={section}
      className={`rd-section-link${view.section === section ? ' active' : ''}`} aria-label={SECTION_LABELS[section]} aria-current={view.section === section ? 'page' : undefined} onClick={() => change({ section })}>
      <Icon name={section === 'overview' ? 'globe' : section === 'systems' ? 'map' : 'book'} /><strong>{SECTION_LABELS[section]}</strong>
      <small>{section === 'overview' ? t(`${data.availableCount} доступно`, `${data.availableCount} available`) : section === 'systems' ? t(`${data.systems.filter(x => x.isOpen).length} из 2 открыто · ${data.overrideCount} ручн.`, `${data.systems.filter(x => x.isOpen).length} of 2 open · ${data.overrideCount} overrides`) : section === 'packs' ? t(`${data.packs.length} подключено · ${data.packs.filter(x => x.isEnabled && x.status === 'active').flatMap(x => x.entries ?? []).filter(x => x.state === 'enabled').length} выбрано`, `${data.packs.length} connected · ${data.packs.filter(x => x.isEnabled && x.status === 'active').flatMap(x => x.entries ?? []).filter(x => x.state === 'enabled').length} selected`) : t(`${data.items.filter(x => x.isEnabled && x.status === 'active').length} включено · ${data.items.filter(x => x.status === 'pending').length} ждёт`, `${data.items.filter(x => x.isEnabled && x.status === 'active').length} enabled · ${data.items.filter(x => x.status === 'pending').length} pending`)}</small>
    </button>)}</nav>
    {view.section === 'overview' && <>
      {data.alerts.length ? <SectionCard icon="alert" title={t('Требует внимания', 'Needs attention')}>
        {data.alerts.map((alert, i) => <button key={i} className="rd-alert" onClick={() => { jump(alert.kind === 'pendingItem' ? 'items' : alert.kind === 'disabledInUse' ? 'systems' : 'packs', alert.targetId); if (alert.system) { change({ section:'systems', system:alert.system, category:alert.entries?.[0]?.category ?? 'skill' }); setSource('disabled') } }}>
          <Icon name="alert" /><span>{alert.kind === 'disabledInUse' ? t(`${alert.count} отключённых элементов используются: ${alert.entries?.map(x => localizedName(x)).join(', ') || alert.usedBy?.join(', ')}`, `${alert.count} disabled entries are used by: ${alert.entries?.map(x => localizedName(x)).join(', ') || alert.usedBy?.join(', ')}`) : alert.kind === 'pendingItem' ? t(`${data.items.find(x => x.id === alert.targetId)?.ownerName ?? 'Игрок'} предлагает элемент`, `${data.items.find(x => x.id === alert.targetId)?.ownerName ?? 'Player'} proposed an entry`) : alert.kind === 'pendingPack' ? t(`${data.packs.find(x => x.id === alert.targetId)?.ownerName ?? 'Игрок'} предлагает набор`, `${data.packs.find(x => x.id === alert.targetId)?.ownerName ?? 'Player'} proposed a pack`) : alert.kind === 'pendingEntries' ? t('Новые элементы ждут одобрения', 'New entries await approval') : t('Набор изменился после подключения', 'A pack changed after connection')} {alert.targetId && (data.packs.find(p => p.id === alert.targetId)?.name ?? localizedName(data.items.find(i => i.id === alert.targetId) ?? {name:'',nameRu:''}))}</span><span>→</span>
        </button>)}
      </SectionCard> : <p className="rd-success">{t('Все предложения рассмотрены, конфликтов с используемым контентом нет.', 'All proposals reviewed; no conflicts with content in use.')}</p>}
      <div className="rd-overview-columns">        <SectionCard title={t('Системы','Systems')} actions={<button onClick={() => jump('systems')}>{t('Настроить →','Configure →')}</button>}>
          {data.systems.map(s => <div key={s.system} className="rd-overview-system"><SystemBadge system={s.system}/><p className="hint">{s.characters.length ? t(`Персонажи: ${s.characters.map(c=>c.name).join(', ')}`,`Characters: ${s.characters.map(c=>c.name).join(', ')}`) : t('Персонажей этой системы нет.','No characters in this system.')}</p>
            {!s.isOpen ? <p className="muted">{t('Закрыта для новых персонажей','Closed to new characters')}</p> : s.categories.map(c => <button key={c.category} className="rd-category-bar" onClick={() => change({section:'systems',system:s.system,category:c.category})}><span>{CATEGORY_LABELS[c.category]}</span><span className="rd-bar"><i style={{width:`${c.total ? c.enabled/c.total*100:0}%`,background:c.enabled<c.total ? 'var(--accent-dark)' : undefined}}/></span><b className={c.enabled<c.total ? 'rd-warning' : ''}>{c.enabled}/{c.total}</b></button>)}
          </div>)}
        </SectionCard>

        <SectionCard title={t('Подключённые наборы', 'Connected packs')} actions={<button onClick={() => jump('packs')}>{t('Управлять', 'Manage')}</button>}>
          {!data.packs.length && <p className="hint">{t('Наборы ещё не подключены.', 'No packs connected yet.')}</p>}
          {data.packs.map(p => <button key={p.id} className="rd-overview-row" onClick={() => jump('packs', p.id)}><span><strong>{p.name}</strong><small>{p.ownerName} · {SYSTEM_LABELS[p.system]} · +{p.entries?.filter(x => x.state === 'enabled').length ?? 0}/{p.entryCount} · −{p.exclusionCount} {t('из книг','from books')}</small></span><span>{p.status === 'pending' ? t('Ожидает', 'Pending') : p.status === 'declined' ? t('Отклонён', 'Declined') : p.isEnabled ? t('Включён', 'Enabled') : t('Выключен', 'Disabled')} →</span></button>)}
        </SectionCard>
        <SectionCard title={t('Отдельные элементы', 'Individual entries')} actions={<button onClick={() => jump('items')}>{t('Управлять', 'Manage')}</button>}>
          {!data.items.length && <p className="hint">{t('Отдельных элементов нет.', 'No individual entries.')}</p>}
          {data.items.map(x => <button key={x.id} className="rd-overview-row" onClick={() => jump('items', x.id)}><span><strong>{localizedName(x)}</strong><small>{ENTRY_LABELS[x.entryType]} · {SYSTEM_LABELS[x.system]} · {x.ownerName}</small></span><span>{x.status === 'pending' ? t('Ожидает', 'Pending') : x.status === 'declined' ? t('Отклонён', 'Declined') : x.isEnabled ? t('Включён', 'Enabled') : t('Выключен', 'Disabled')} →</span></button>)}
        </SectionCard>
      </div>
    </>}
    {view.section === 'systems' && <>
      <div className="rd-toolbar">{data.systems.map(s => <FilterChip key={s.system} active={view.system === s.system} onClick={() => change({ system: s.system })}>{SYSTEM_LABELS[s.system]} · {t(`${s.characters.length} перс.`,`${s.characters.length} characters`)}</FilterChip>)}</div>
      <div className="rd-toolbar"><span className="muted">{t('Ограничения из наборов:','Restrictions from packs:')}</span>{data.packs.filter(p => p.system===view.system && p.isEnabled && p.status==='active' && p.exclusionCount>0).map(p => <button className="rd-filter-chip" key={p.id} onClick={() => jump('packs',p.id)}>{p.name} · −{p.exclusionCount}</button>)}{!data.packs.some(p => p.system===view.system && p.isEnabled && p.status==='active' && p.exclusionCount>0) && <span className="hint">{t('нет','none')}</span>}</div>
      <SectionCard title={<SystemBadge system={view.system} />} subtitle={view.system === 'genesysCore' ? t('Базовый каталог движка: навыки для любого сеттинга, общие таланты и снаряжение.','The base catalogue: skills for any setting, general talents and equipment.') : t('Собственный каталог: свой список навыков (ближний бой разделён, знания по областям, руны и стих), виды и карьеры Терринота, героические способности. Общие таланты и снаряжение Core входят в него копиями.','A dedicated fantasy catalogue with split melee skills, knowledge fields, runes, verse, Terrinoth species and careers, and heroic abilities. Shared Core talents and equipment have their own catalogue entries.')} actions={<Switch checked={selectedSystem.isOpen} label={t('Открыта для новых персонажей', 'Open to new characters')} disabled={busy} onChange={on => void run(() => api.setCampaignSystem(campaignId, view.system, on))} />}>
        {!!selectedSystem.characters.length && <p className="hint">{t('Персонажи этой системы:', 'Characters in this system:')} {selectedSystem.characters.map(c => c.name).join(', ')}</p>}
        <p className={selectedSystem.isOpen ? 'hint' : 'rd-warning'}>{selectedSystem.isOpen ? t(`${selectedSystem.categories.reduce((n,c)=>n+c.enabled,0)} из ${selectedSystem.categories.reduce((n,c)=>n+c.total,0)} элементов доступно`,`${selectedSystem.categories.reduce((n,c)=>n+c.enabled,0)} of ${selectedSystem.categories.reduce((n,c)=>n+c.total,0)} entries available`) : t('Новых персонажей этой системы создать нельзя. Существующие персонажи остаются со своими листами.','New characters in this system cannot be created. Existing characters retain their sheets.')}</p>
        <div className="rd-toolbar"><button disabled={busy} onClick={() => { if (window.confirm(t('Сбросить ручные правки этой системы? Ограничения наборов останутся.', 'Reset manual overrides for this system? Pack restrictions remain.'))) void run(() => api.resetCampaignBase(campaignId, view.system)) }}>{t('Сбросить ручные правки', 'Reset overrides')}</button>
          <button disabled={busy} title={t('Создать набор с текущими ограничениями книг, чтобы применить их в другой кампании','Create a pack with the current book restrictions for use in another campaign')} onClick={() => { const name = window.prompt(t('Название набора ограничений', 'Restriction pack name'), t('Правила кампании', 'Campaign rules')); if (name?.trim()) void run(() => api.saveCampaignBaseAsPack(campaignId, view.system, name)).then(ok => { if (ok) setNotice(t(`Создан набор «${name}» — он подключён и лежит в библиотеке. Необходимые разрешающие правки сохранены.`,`Pack “${name}” created, connected and available in your library. Necessary allow overrides are preserved.`)) }) }}>{t('Сохранить как набор', 'Save as pack')}</button></div>
      </SectionCard>
      <fieldset disabled={!selectedSystem.isOpen} className={`rd-book-layout${selectedSystem.isOpen ? '' : ' rd-book-closed'}`}><nav className="rd-sidebar" aria-label={t('Категории книг', 'Book categories')}>{selectedSystem.categories.map(c => <SideNavRow key={c.category} active={view.category === c.category} onClick={() => change({ category:c.category })}>{CATEGORY_LABELS[c.category]} {c.enabled}/{c.total}</SideNavRow>)}</nav><main className="rd-workspace">
      <div className="rd-toolbar"><input type="search" aria-label={t('Поиск в книгах', 'Search books')} placeholder={t('Поиск…', 'Search…')} value={search} onChange={e => setSearch(e.target.value)} />
        <select value={source} onChange={e => setSource(e.target.value)} aria-label={t('Источник ограничения', 'Restriction source')}><option value="all">{t('Все источники', 'All sources')}</option><option value="disabled">{t('Отключённые', 'Disabled')}</option><option value="pack">{t('Наборы', 'Packs')}</option><option value="manual">{t('Ручные', 'Manual')}</option><option value="restored">{t('Возвращённые', 'Restored')}</option></select>
        {view.system === 'realmsOfTerrinoth' && <><FilterChip active={bookSource === 'all'} onClick={() => setBookSource('all')}>{t('Все', 'All')}</FilterChip><FilterChip active={bookSource === 'rot'} onClick={() => setBookSource('rot')}>{t('Только Terrinoth', 'Terrinoth only')}</FilterChip><FilterChip active={bookSource === 'core'} onClick={() => setBookSource('core')}>{t('Общее с Core', 'Shared with Core')}</FilterChip></>}
        <button disabled={busy || !visibleBase.length} onClick={() => setBaseEntries(visibleBase, true)}>{t('Включить видимые', 'Enable visible')}</button><button disabled={busy || !visibleBase.length} onClick={() => setBaseEntries(visibleBase, false)}>{t('Отключить видимые', 'Disable visible')}</button>
      </div>
      {base?.system !== view.system ? <p>{t('Загрузка…', 'Loading…')}</p> : <div className="rd-section-card">{!visibleBase.length && <p className="rd-empty">{t('Подходящих элементов нет.', 'No matching entries.')}</p>}
        {visibleBase.map(x => <div key={x.key} className={`rd-content-row${x.enabled ? '' : ' rd-disabled-entry'}`} role="button" tabIndex={selectedSystem.isOpen ? 0 : -1} aria-disabled={busy || !selectedSystem.isOpen} onClick={() => { if (!busy && selectedSystem.isOpen) setBaseEntries([x],!x.enabled) }} onKeyDown={e => { if (e.target === e.currentTarget && !busy && selectedSystem.isOpen && (e.key === 'Enter' || e.key === ' ')) { e.preventDefault(); setBaseEntries([x],!x.enabled) } }}><div className="rd-entry-description"><strong>{localizedName(x)}</strong><small>{contentMeta(x.meta)}</small>{x.isSharedWithCore && <span className="badge" title={t('Общий элемент с Genesys Core','Shared catalogue entry with Genesys Core')}>{t('из Core','from Core')}</span>}
          {x.source && <small className={x.source === 'restored' ? 'rd-restored' : 'muted'}>{x.source === 'pack' ? t(`Убрано набором: ${x.sourcePackName}`, `Excluded by pack: ${x.sourcePackName}`) : x.source === 'manual' ? t('Ручная правка', 'Manual override') : t('Возвращено мастером', 'Restored by GM')}</small>}
          {!!x.usedBy.length && <small className={!x.enabled ? 'rd-warning' : 'muted'}>{t('Используется:', 'Used by:')} {x.usedBy.join(', ')}</small>}</div>
          <Switch checked={x.enabled} disabled={busy} label={t(`Доступен: ${localizedName(x)}`, `Available: ${localizedName(x)}`)} onChange={on => setBaseEntries([x], on)} />
        </div>)}
      </div>}
      </main></fieldset>
      {categoryRows.some(x => !x.enabled && x.usedBy.length>0) && <p className="rd-warning rd-dashed">{t('Отключённые элементы уже есть у персонажей:','Disabled entries are already used by characters:')} {categoryRows.filter(x=>!x.enabled && x.usedBy.length>0).map(x=>`${localizedName(x)} (${x.usedBy.join(', ')})`).join('; ')}. {t('Они останутся на листах, но новые покупки недоступны.','They remain on the sheets; new purchases are unavailable.')}</p>}
    </>}
    {view.section === 'packs' && <>
      <div className="rd-toolbar"><h3>{t('Наборы кампании', 'Campaign packs')}</h3><span className="rd-spacer" /><button onClick={() => void openPicker('pack')}><Icon name="plus" />{t('Из библиотеки', 'From library')}</button><button onClick={() => setShared(true)}><Icon name="share" />{t('По ссылке игрока', 'From player link')}</button></div>
      <p className="hint">{t('Подключается сам набор автора. Изменения его элементов видны кампании без создания копий.', 'Connects the author’s original pack. Entry edits are visible to the campaign without copying.')}</p>
      <div className="rd-toolbar"><input type="search" placeholder={t('Набор или автор…', 'Pack or author…')} aria-label={t('Поиск наборов', 'Search packs')} value={search} onChange={e => setSearch(e.target.value)} /><FilterChip active={attentionOnly} onClick={() => setAttentionOnly(x => !x)}>{t('Требует внимания', 'Needs attention')}</FilterChip></div>
      {!packVisible.length && <p className="rd-empty rd-dashed">{t('Нет подходящих наборов.', 'No matching packs.')}</p>}
      <div className="rd-pack-grid">{packVisible.map(p => <SectionCard id={`rd-content-${p.id}`} className={`${targetId === p.id ? 'rd-target' : ''} ${p.isEnabled ? '' : 'rd-pack-disabled'}`} key={p.id} title={p.name} subtitle={<><SystemBadge system={p.system} /> · {p.ownerName}{p.isMine ? t(' · ваш набор', ' · your pack') : ''}{!p.ownerIsMember ? t(' · автор покинул кампанию', ' · author left the campaign') : ''}</>} actions={<Switch checked={p.isEnabled} disabled={busy || p.status !== 'active'} label={t(`Подключён: ${p.name}`, `Connected: ${p.name}`)} onChange={on => void run(() => api.setCampaignHomebrewPack(campaignId, p.id, on))} />}>
        <PackChangeStatus pack={p} /><p className="hint">{t(`${p.entryCount} элементов · ${p.exclusionCount} ограничений`, `${p.entryCount} entries · ${p.exclusionCount} exclusions`)}</p>
        {p.status === 'pending' ? <div className="rd-alert"><span>{t('Предложен игроком — ожидает решения', 'Proposed by a player — awaiting decision')}</span><div className="rd-actions"><button disabled={busy} className="primary" onClick={() => void run(() => api.decideCampaignContent(campaignId, 'pack', p.id, 'approve'))}>{t('Одобрить', 'Approve')}</button><button disabled={busy} onClick={() => void run(() => api.decideCampaignContent(campaignId, 'pack', p.id, 'decline'))}>{t('Отклонить', 'Decline')}</button></div></div>
          : p.status === 'declined' && <p className="rd-warning">{t('Предложение отклонено', 'Proposal declined')}</p>}
        <details className="rd-pack-entries" open={targetId === p.id || p.entries?.some(x => x.state === 'pending')}><summary>{t('Состав набора', 'Pack contents')}</summary>
          {p.entries?.map(x => <CheckRow className={x.state === 'pending' ? 'rd-pending-row' : ''} key={contentKey(x.entryType, x.entryId)} label={localizedName(x)} checked={x.state === 'enabled'} disabled={busy || p.status !== 'active'} meta={<>{ENTRY_LABELS[x.entryType]}{x.state === 'pending' && <span className="rd-warning"> · {t('Новый, ожидает решения', 'New, awaiting decision')}</span>}</>}
            onChange={on => void run(() => api.setCampaignPackEntries(campaignId, p.id, [{ entryType: x.entryType, entryId: x.entryId, enabled: on }]))}
            suffix={x.state === 'pending' && <button disabled={busy} onClick={() => void run(() => api.setCampaignPackEntries(campaignId, p.id, [{ entryType: x.entryType, entryId: x.entryId, enabled: false }]))}>{t('Отклонить', 'Decline')}</button>} />)}
          {p.exclusions?.map(x => <div className="rd-content-row" key={contentKey(x.category, x.key)}><span>{localizedName(x)}</span><small className="rd-warning">{t('Убирает из книг', 'Excluded from books')}</small></div>)}
          {p.status === 'active' && <><div className="rd-toolbar"><button disabled={busy || !p.entries?.length} onClick={() => void run(() => api.setCampaignPackEntries(campaignId,p.id,p.entries!.map(x => ({...x,enabled:true}))))}>{t('Выбрать все','Select all')}</button><button disabled={busy || !p.entries?.length} onClick={() => void run(() => api.setCampaignPackEntries(campaignId,p.id,p.entries!.map(x => ({...x,enabled:false}))))}>{t('Снять все','Deselect all')}</button></div><p className="hint">{t('Новые элементы, которые автор добавит в набор:','New entries the author adds to the pack:')}</p><div className="rd-chips">{(['auto','manual'] as const).map(policy => <FilterChip key={policy} active={p.updatePolicy===policy} disabled={busy} onClick={() => void run(() => api.setCampaignHomebrewPack(campaignId,p.id,p.isEnabled,policy))}>{policy==='auto' ? t('Включать сразу','Enable immediately') : t('Ждать моего решения','Await my decision')}</FilterChip>)}</div></>}
        </details><div className="rd-toolbar"><button onClick={() => setHistory(p)}>{t('История', 'History')}</button><button disabled={busy} onClick={() => { if (window.confirm(t('Отключить набор от кампании?', 'Disconnect pack from campaign?'))) void run(() => api.disconnectCampaignPack(campaignId, p.id)) }}>{t('Убрать из кампании', 'Disconnect')}</button></div>
      </SectionCard>)}</div>
    </>}
    {view.section === 'items' && <SectionCard title={t('Отдельные элементы', 'Individual entries')} subtitle={t('Оригинальные элементы библиотеки, подключённые без набора.', 'Original library entries connected without a pack.')} actions={<button onClick={() => void openPicker('item')}><Icon name="plus" />{t('Добавить из библиотеки', 'Add from library')}</button>}>
      {!data.items.length && <p className="rd-empty">{t('Отдельные элементы не подключены.', 'No individual entries connected.')}</p>}
      {data.items.map(x => <div id={`rd-content-${x.id}`} className={`rd-content-row${targetId === x.id ? ' rd-target' : ''}`} key={x.id}><span className="rd-entry-icon"><Icon name={ENTRY_ICONS[x.entryType]}/></span><div className="rd-entry-description"><strong>{localizedName(x)}</strong><small>{ENTRY_LABELS[x.entryType]} · {contentMeta(x.meta)}</small><span className={`badge rd-proposal-${x.status}`}>{x.isMine ? t('Моя библиотека','My library') : x.ownerName}</span><SystemBadge system={x.system} />
        {x.status === 'pending' && <small className="rd-warning">{t('Предложен игроком', 'Proposed by a player')}</small>}{x.status === 'declined' && <small>{t('Отклонено', 'Declined')}</small>}</div>
        {x.status === 'pending' ? <div className="rd-actions"><button className="primary" disabled={busy} onClick={() => void run(() => api.decideCampaignContent(campaignId, 'item', x.entryId, 'approve'))}>{t('Одобрить', 'Approve')}</button><button disabled={busy} onClick={() => void run(() => api.decideCampaignContent(campaignId, 'item', x.entryId, 'decline'))}>{t('Отклонить', 'Decline')}</button></div>
          : <Switch checked={x.isEnabled && x.status === 'active'} label={t(`Доступен: ${localizedName(x)}`, `Available: ${localizedName(x)}`)} disabled={busy} onChange={on => void run(() => api.setCampaignItem(campaignId, x.id, on))} />}
        <button className="rd-icon-button" disabled={busy} aria-label={t('Убрать из кампании', 'Remove from campaign')} onClick={() => { if (window.confirm(t('Убрать элемент из кампании?', 'Remove entry from campaign?'))) void run(() => api.removeCampaignItem(campaignId, x.id)) }}><Icon name="trash" /></button>
      </div>)}
    </SectionCard>}
    {history && <HomebrewPackHistory packId={history.id} name={history.name} campaignId={campaignId} onClose={() => setHistory(null)} />}
    {picker && <PickerModal actionLabel={t('Подключить','Connect')} hint={picker.kind === 'pack' ? t('Подключается весь исходный набор; лишние элементы можно выключить.','Connects the whole original pack; individual entries can be disabled.') : t('Элемент будет доступен без подключения всего набора.','The entry will be available without connecting its whole pack.')} title={picker.kind === 'pack' ? t('Подключить наборы', 'Connect packs') : t('Подключить элементы', 'Connect entries')} rows={picker.rows} onClose={() => setPicker(null)} onConfirm={async keys => {
      if (picker.kind === 'pack') { for (const key of keys) await api.setCampaignHomebrewPack(campaignId, key, true) }
      else await api.connectCampaignItems(campaignId, picker.items!.filter(x => keys.includes(contentKey(x.entryType, x.id))).map(x => ({ entryType: x.entryType, entryId: x.id })))
      await reload()
    }} />}
    {shared && <DialogFrame title={t('Подключить набор по ссылке', 'Connect a pack by link')} onClose={() => setShared(false)} busy={busy} footer={<><button disabled={busy} onClick={() => setShared(false)}>{t('Отмена', 'Cancel')}</button><button className="primary" disabled={busy || !sharedLink.trim()} onClick={() => {
      const token = sharedLink.trim().split('/').filter(Boolean).pop()!.split(/[?#]/)[0]
      void run(() => api.connectSharedCampaignHomebrewPack(campaignId, decodeURIComponent(token))).then(ok => { if (ok) { setShared(false); setSharedLink('') } })
    }}>{t('Подключить', 'Connect')}</button></>}>
      <p className="hint">{t('Автор предоставляет ссылку на свой набор. Кампания использует оригинал.', 'The author shares their pack link. The campaign uses the original.')}</p>{error && <p className="error">{error}</p>}
      <label>{t('Ссылка или токен', 'Link or token')}<input value={sharedLink} disabled={busy} onChange={e => setSharedLink(e.target.value)} /></label>
    </DialogFrame>}
  </div>
}
