import { Select } from '../components/Select'
import { toneClass } from '../utils/tones'
import { RuleText } from '../components/RuleText'
import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { api } from '../api/client'
import type {
  Characteristic, CreatureTemplate, GameSystem, NpcAttackEntry, NpcCombatStyle, NpcDetail, NpcInput,
  NpcKind, NpcListItem, NpcPowerLevel, NpcRole, NpcVisibility, Quality, QuickDraftRequest, Reference, SkillDef,
} from '../api/types'
import {
  CHARACTERISTICS, CHARACTERISTIC_LABELS, CHARACTERISTIC_SHORT_LABELS, CREATURE_TEMPLATE_LABELS, CREATURE_TEMPLATES, dualName,
  ITEM_KIND_LABELS, localizedDescription, NPC_COMBAT_STYLE_LABELS, NPC_KIND_LABELS, NPC_KINDS, NPC_POWER_LABELS,
  NPC_ROLE_LABELS, NPC_ROLES, NPC_VISIBILITY_LABELS, secondaryName, SYSTEM_LABELS,
} from '../utils/labels'
import {
  buildPool, npcAttackViews, npcGearViews, npcSkillName, npcSkillViews, skillIndex, syncAttacksWithEquipment, weaponsByLabel,
  type NpcGearView,
} from '../utils/npcStats'
import { DicePoolView } from '../components/DicePoolView'
import { resolveQualityCosts } from '../utils/combat'
import { PropertyTags } from '../components/PropertyTags'
import { qualityProperties } from '../data/itemQualities'
import { PrintPreview } from '../components/print/PrintPreview'
import { AdversaryCard } from '../components/print/cards'
import { adversaryMarkdown } from '../components/print/markdown'
import { useDiceRoller } from '../dice-roller-store'
import { t } from '../i18n'
import { Icon, type IconName } from '../components/Icon'
import { NumberStepper } from '../components/content/NumberStepper'
import { TalentTags } from '../components/bestiary/TalentTags'
import { BestiaryFilters } from '../components/bestiary/BestiaryFilters'
import { bestiaryQuery, EMPTY_FILTERS, filterBestiary, readBestiaryFilters, suggestedNpcSoak, type BestiaryFilters as Filters } from '../utils/bestiary'
import { navigate, registerNavigationGuard, useSearch } from '../router'
import './bestiary.css'

const SYSTEMS: GameSystem[] = ['genesysCore', 'realmsOfTerrinoth']

interface Props {
  openId: string | null
  creating?: boolean
  edit?: boolean
  onOpen: (id: string) => void
  onBack: () => void
}

export function NpcsPage({ openId, creating = false, edit = false, onOpen, onBack }: Props) {
  const query = useSearch()
  const filters = useMemo(() => readBestiaryFilters(query), [query])
  const listQuery = bestiaryQuery(filters)
  if (creating || edit) return <NpcCreationPage key={edit ? openId : 'new'} id={edit ? openId : null} query={query} onBack={onBack}
    onSaved={n => navigate(`/npcs/${n.id}${listQuery}`)} />
  return <BestiaryPage openId={openId} filters={filters} onOpen={onOpen} onBack={onBack} />
}

function BestiaryPage({ openId, filters, onOpen, onBack }: Props & { filters: Filters }) {
  const [npcs, setNpcs] = useState<NpcListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [filterOpen, setFilterOpen] = useState(false)
  const [version, setVersion] = useState(0)
  const listRef = useRef<HTMLUListElement>(null)
  const query = bestiaryQuery(filters)
  const scrollKey = `bestiary-scroll:${query}`
  useEffect(() => {
    let cancelled = false
    api.npcs().then(items => { if (!cancelled) { setNpcs(items); setError(null) } })
      .catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : t('Ошибка загрузки', 'Loading failed')) })
    return () => { cancelled = true }
  }, [version])
  const shown = useMemo(() => filterBestiary(npcs ?? [], filters), [npcs, filters])
  useEffect(() => {
    if (listRef.current) listRef.current.scrollTop = Number(sessionStorage.getItem(scrollKey) ?? 0)
  }, [scrollKey, npcs, openId])
  const updateFilters = (f: Filters) => navigate(`${openId ? `/npcs/${openId}` : '/npcs'}${bestiaryQuery(f)}`, { replace: true })
  const create = (mode: string) => navigate(`/npcs/new${query ? `${query}&` : '?'}mode=${mode}`)
  const count = Number(!!filters.system) + Number(!!filters.role) + filters.kinds.length + Number(!!filters.source) + Number(filters.sort !== 'createdAt')
  const sources = filterBestiary(npcs ?? [], { ...filters, source: '' })
  return <div className={`bt-page${openId ? ' has-selection' : ''}`}>
    <div className="page-head bt-list-heading"><div><h2 className="display-title">{t('Бестиарий', 'Bestiary')}</h2><small className="muted">{npcs ? t(`${npcs.length} NPC · ${npcs.filter(n => n.isMine).length} моих`, `${npcs.length} NPCs · ${npcs.filter(n => n.isMine).length} mine`) : '…'}</small></div>
        <div className="bt-actions"><button type="button" className="bt-draft-button" title={t('Быстрый черновик', 'Quick draft')} aria-label={t('Быстрый черновик', 'Quick draft')} onClick={() => create('draft')}><Icon name="bolt" /><span>{t('Черновик', 'Draft')}</span></button>
        <button type="button" className="primary bt-create-desktop" aria-label={t('Создать NPC', 'Create NPC')} onClick={() => create('manual')}><Icon name="plus" />{t('Создать', 'Create')}</button></div></div>
    <aside className="bt-list" aria-label={t('Список NPC', 'NPC list')}>
      <div className="bt-search-row"><input type="search" aria-label={t('Поиск по имени', 'Search by name')} placeholder={t('Поиск по имени…', 'Search by name…')} value={filters.search} onChange={e => updateFilters({ ...filters, search: e.target.value })} />
        <button type="button" className="bt-mobile-filter bt-icon" aria-label={t('Фильтры', 'Filters')} aria-expanded={filterOpen} onClick={() => setFilterOpen(true)}><Icon name="adjustments" />{count > 0 && <span className="bt-filter-count">{count}</span>}</button></div>
      <div className="bt-filter-pills"><div className="bt-pills bt-source-pills">{(['', 'mine', 'builtin'] as const).map(source => <button key={source} type="button" aria-pressed={filters.source === source} onClick={() => updateFilters({ ...filters, source })}>{source === 'mine' ? t('Мои', 'Mine') : source === 'builtin' ? t('Встроенные', 'Built-in') : t('Все', 'All')}{source !== 'builtin' && <small>{source === 'mine' ? sources.filter(n => n.isMine).length : sources.length}</small>}</button>)}</div>
      <div className="bt-pills bt-kind-pills">{NPC_KINDS.map(kind => <button key={kind} type="button" className={`kind-${kind} ${toneClass('npc', kind)}`} aria-pressed={filters.kinds.includes(kind)} onClick={() => updateFilters({ ...filters, kinds: filters.kinds.includes(kind) ? filters.kinds.filter(k => k !== kind) : [...filters.kinds, kind] })}>{NPC_KIND_LABELS[kind]}</button>)}
        <button type="button" className="bt-desktop-filter" aria-expanded={filterOpen} onClick={() => setFilterOpen(true)}><Icon name="adjustments" />{t('Фильтры', 'Filters')}{count > 0 && <small>{count}</small>}</button></div></div>
      <div className="bt-inline-filters">
        <Select aria-label={t('Система', 'System')} value={filters.system} onChange={value => updateFilters({ ...filters, system: value as Filters['system'] })}>
          <option value="">{t('Все системы', 'All systems')}</option>{SYSTEMS.map(system => <option key={system} value={system}>{SYSTEM_LABELS[system]}</option>)}</Select>
        <Select aria-label={t('Роль', 'Role')} value={filters.role} onChange={value => updateFilters({ ...filters, role: value as Filters['role'] })}>
          <option value="">{t('Все роли', 'All roles')}</option>{NPC_ROLES.map(role => <option key={role} value={role}>{NPC_ROLE_LABELS[role]}</option>)}</Select>
      </div>
      <div className="bt-filter-summary"><div className="bt-pills">{filters.system && <button type="button" onClick={() => updateFilters({ ...filters, system: '' })}>{SYSTEM_LABELS[filters.system]} ×</button>}{filters.role && <button type="button" onClick={() => updateFilters({ ...filters, role: '' })}>{NPC_ROLE_LABELS[filters.role]} ×</button>}</div>
        <Select aria-label={t('Сортировка', 'Sort')} value={filters.sort} onChange={value => updateFilters({ ...filters, sort: value as Filters['sort'] })}><option value="createdAt">{t('Сначала новые', 'Newest first')}</option><option value="name">{t('По имени', 'By name')}</option></Select></div>
      {error && <div className="error" role="alert">{error}<button onClick={() => setVersion(v => v + 1)}>{t('Повторить', 'Retry')}</button></div>}
      <ul className="bt-items" ref={listRef} onScroll={e => sessionStorage.setItem(scrollKey, String(e.currentTarget.scrollTop))}>
        {!npcs && !error && Array.from({ length: 6 }, (_, i) => <li key={i} className="bt-skeleton" aria-label={t('Загрузка NPC', 'Loading NPC')} />)}
        {npcs && shown.map(n => <li key={n.id}><button type="button" className={`bt-item${openId === n.id ? ' active' : ''}`} aria-current={openId === n.id ? 'true' : undefined} onClick={() => onOpen(n.id)}>
          <div className="bt-item-copy"><div className="bt-item-title"><strong title={n.name}>{n.name}</strong><span className={`bt-kind kind-${n.kind} ${toneClass('npc', n.kind)}`}>{NPC_KIND_LABELS[n.kind]}</span></div>
          <small className="bt-item-meta">{NPC_ROLE_LABELS[n.role]} · <span className={n.system === 'genesysCore' ? 'bt-core' : 'bt-terrinoth'}>{n.system === 'genesysCore' ? 'Core' : 'Terrinoth'}</span> · <i>{n.isBuiltIn ? t('встроенный', 'built-in') : n.isMine ? t('мой', 'mine') : t('кампания', 'campaign')}</i></small></div>
          <div className="bt-item-stats"><span title={t('Поглощение', 'Soak')}><Icon name="shield" />{n.soak}</span><span title={t('Раны', 'Wounds')}><Icon name="heart" />{n.woundThreshold}</span></div><Icon className="bt-row-chevron" name="chevron-right" /></button></li>)}
        {npcs && !shown.length && <li className="bt-empty"><Icon name="skull" /><p>{npcs.length ? t('Ничего не найдено', 'No matches found') : t('В бестиарии пока нет NPC', 'No NPCs yet')}</p><button type="button" onClick={() => npcs.length ? updateFilters({ ...EMPTY_FILTERS }) : create('manual')}>{npcs.length ? t('Сбросить фильтры', 'Reset filters') : t('Создать NPC', 'Create NPC')}</button></li>}
      </ul>
      <button type="button" className="primary bt-fab" onClick={() => create('manual')}><Icon name="plus" />{t('Создать NPC', 'Create NPC')}</button>
    </aside>
    <section className="bt-detail" aria-label={t('Карточка NPC', 'NPC card')}>
      {openId ? <NpcDetailView key={openId} npcId={openId} reloadToken={version} onBack={onBack}
        onEdit={n => navigate(`/npcs/${n.id}/edit${query}`)} onDuplicated={id => { setVersion(v => v + 1); onOpen(id) }} onDeleted={() => { setVersion(v => v + 1); onBack() }} />
        : <div className="bt-empty bt-select-placeholder"><Icon name="skull" /><p>{t('Выберите NPC слева', 'Select an NPC on the left')}</p></div>}
    </section>
    {filterOpen && <BestiaryFilters filters={filters} count={shown.length} onChange={updateFilters} onClose={() => setFilterOpen(false)} />}
  </div>
}

function NpcDetailView({ npcId, reloadToken, onBack, onEdit, onDuplicated, onDeleted }: {
  npcId: string
  reloadToken: number
  onBack: () => void
  onEdit: (n: NpcDetail) => void
  onDuplicated: (id: string) => void
  onDeleted: () => void
}) {
  const [n, setN] = useState<NpcDetail | null>(null)
  const [reference, setReference] = useState<Reference | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [printing, setPrinting] = useState(false)
  const [busy, setBusy] = useState(false)
  const [retry, setRetry] = useState(0)

  // Грузим карточку при открытии и при росте reloadToken (после правки того же NPC).
  useEffect(() => {
    let cancelled = false
    api.npc(npcId)
      .then(d => { if (!cancelled) { setN(d); setError(null) } })
      .catch((e: unknown) => { if (!cancelled) setError(e instanceof Error ? e.message : t('Ошибка', 'Error')) })
    return () => { cancelled = true }
  }, [npcId, reloadToken, retry])

  // Справочник системы NPC нужен для характеристик навыков и боевых параметров оружия.
  const system = n?.system
  useEffect(() => {
    if (!system) return
    let cancelled = false
    api.reference(system)
      .then(r => { if (!cancelled) setReference(r) })
      .catch(() => { /* без справочника пулы кубов просто не покажем */ })
    return () => { cancelled = true }
  }, [system])

  if (!n) return <div className="bt-empty">
    <button type="button" onClick={onBack}><Icon name="arrow-left" />{t('Бестиарий', 'Bestiary')}</button>
    {error ? <><div className="error" role="alert">{error}</div><button type="button" onClick={() => { setError(null); setRetry(v => v + 1) }}>{t('Повторить', 'Retry')}</button></>
      : <div className="bt-skeleton bt-card-skeleton" aria-label={t('Загрузка карточки', 'Loading card')} />}
  </div>

  if (printing) {
    // GM видит обе версии; владелец считается GM. Игрок без доступа печатает только player-версию.
    const versions: ('gm' | 'player')[] = n.isMine ? ['gm', 'player'] : ['player']
    return (
      <PrintPreview title={t(`Карточка NPC — ${n.name}`, `NPC card — ${n.name}`)} versions={versions}
        markdown={(v) => adversaryMarkdown(n, v, reference)} onClose={() => setPrinting(false)}>
        {(v) => <AdversaryCard npc={n} version={v} reference={reference} />}
      </PrintPreview>
    )
  }

  async function action(fn: () => Promise<void>) {
    setBusy(true); setError(null)
    try { await fn() }
    catch (e) { setError(e instanceof Error ? e.message : t('Ошибка', 'Error')) }
    finally { setBusy(false) }
  }
  const duplicate = () => void action(async () => { const dup = await api.duplicateNpc(n.id); onDuplicated(dup.id) })
  const remove = () => {
    if (confirm(t(`Удалить NPC «${n.name}»?`, `Delete NPC "${n.name}"?`))) void action(async () => { await api.deleteNpc(n.id); onDeleted() })
  }

  return (
    <div className="npc-detail bt-card-view">
      <div className="bt-card-topbar"><button type="button" onClick={onBack}><Icon name="arrow-left" />{t('Бестиарий', 'Bestiary')}</button><button type="button" className="bt-icon" aria-label={t('Печать', 'Print')} onClick={() => setPrinting(true)}><Icon name="printer" /></button></div>
      <div className="bt-detail-head">
        <div className="npc-card-head">
          <h3>{n.name}</h3>
          <span className="bt-card-meta">
            <span className={`bt-kind kind-${n.kind} ${toneClass('npc', n.kind)}`}>{NPC_KIND_LABELS[n.kind]}</span>
            <span className="bt-meta-badge">{NPC_ROLE_LABELS[n.role]}</span>
            <span className={n.system === 'genesysCore' ? 'bt-core' : 'bt-terrinoth'}>{SYSTEM_LABELS[n.system]}</span>
            {n.isBuiltIn && <span className="badge builtin"> {t('встроенный · только чтение', 'built-in · read-only')}</span>}
          </span>
        </div>
      <div className="npc-detail-actions bt-card-actions no-print">
        <button className="bt-print" aria-label={t('Печать', 'Print')} title={t('Печать', 'Print')} onClick={() => setPrinting(true)}><Icon name="printer" /><span>{t('Печать', 'Print')}</span></button>
        {n.isMine && <>
          <button className="primary" onClick={() => onEdit(n)}>{t('Редактировать', 'Edit')}</button>
          <button className="bt-secondary" aria-label={t('Дублировать', 'Duplicate')} title={t('Дублировать', 'Duplicate')} disabled={busy}
            onClick={duplicate}>
            <Icon name="copy" /><span>{t('Дублировать', 'Duplicate')}</span>
          </button>
          <button className="danger bt-secondary" aria-label={t('Удалить', 'Delete')} title={t('Удалить', 'Delete')} disabled={busy}
            onClick={remove}>
            <Icon name="trash" /><span>{t('Удалить', 'Delete')}</span>
          </button>
          <NpcMoreActions busy={busy} onDuplicate={duplicate} onDelete={remove} />
        </>}
        {n.isBuiltIn && (
          <button className="primary" disabled={busy}
            onClick={duplicate}>
            {t('Клонировать в мою библиотеку', 'Clone into my library')}
          </button>
        )}
      </div>
      </div>
      {error && <div className="error" role="alert">{error}</div>}

      <NpcCard npc={n} reference={reference} />

    </div>
  )
}

function NpcCard({ npc: n, reference, canAttack = true }: { npc: NpcDetail; reference: Reference | null; canAttack?: boolean }) {
  const [tab, setTab] = useState('stats')
  const { openRoller } = useDiceRoller()
  const index = useMemo(() => skillIndex(reference), [reference])
  const skills = useMemo(() => npcSkillViews(n, index), [n, index])
  const attacks = useMemo(() => npcAttackViews(n, reference), [n, reference])
  const gear = useMemo(() => npcGearViews(n, reference), [n, reference])
  const chars: [Characteristic, number][] = [
    ['brawn', n.brawn], ['agility', n.agility], ['intellect', n.intellect],
    ['cunning', n.cunning], ['willpower', n.willpower], ['presence', n.presence],
  ]

  return (
      <div className={`npc-card bt-card tab-${tab}`}>
        {n.description && <p className="npc-desc"><RuleText symbolsOnly text={n.description} /></p>}

        <div className="npc-char-row bt-card-chars">
          {chars.map(([c, v]) => (
            <div key={c} className="npc-char">
              <span className="npc-char-val">{v}</span>
              <span className="npc-char-label" title={CHARACTERISTIC_LABELS[c]}>{CHARACTERISTIC_SHORT_LABELS[c]}</span>
            </div>
          ))}
        </div>

        <div className="npc-derived bt-card-derived">
          <span><Icon name="shield" /><strong>{n.soak}</strong><small>{t('Поглощение', 'Soak')}</small></span>
          <span className="bt-wounds"><Icon name="heart" /><strong>{n.woundThreshold}</strong><small>{t('Раны', 'Wounds')}</small></span>
          <span className="bt-strain"><Icon name="bolt" /><strong>{n.strainThreshold ?? '—'}</strong><small>{t('Усталость', 'Strain')}</small></span>
          <span className="bt-defense-desktop"><Icon name="sword" /><strong>{n.meleeDefense}</strong><small>{t('Бл. защита', 'Melee def.')}</small></span>
          <span className="bt-defense-desktop"><Icon name="target" /><strong>{n.rangedDefense}</strong><small>{t('Дал. защита', 'Ranged def.')}</small></span>
          <span className="bt-defense-mobile"><strong>{n.meleeDefense}/{n.rangedDefense}</strong><small>{t('Защита', 'Defense')}</small></span>
        </div>
        {n.silhouette !== 1 && <small className="muted">{t('Силуэт', 'Silhouette')} {n.silhouette}</small>}


        <div className="bt-pills bt-card-tabs" role="tablist" aria-label={t('Разделы NPC', 'NPC sections')}>
          {[['stats', t('Статы', 'Stats')], ['attacks', t(`Атаки · ${attacks.length}`, `Attacks · ${attacks.length}`)], ['more', t('Способности и снаряжение', 'Abilities and gear')]].map(([key, label]) => <button key={key} type="button" role="tab" aria-selected={tab === key} onClick={() => setTab(key)}>{label}</button>)}
        </div>
        <div className="bt-card-sections">
        {n.warnings.length > 0 && (
          <div className="npc-warnings bt-section-warnings">
            <b>{t('Предупреждения:', 'Warnings:')}</b>
            <ul>{n.warnings.map((w, i) => <li key={i}>{w}</li>)}</ul>
          </div>
        )}

        <div className="bt-card-left">
        {attacks.length > 0 && (
          <div className="npc-section bt-section-attacks">
            <h4>{t('Атаки', 'Attacks')}</h4>
            <div className="table-wrap"><table className="npc-weapon-table"><thead><tr>
              <th>{t('Название', 'Name')}</th><th>{t('Навык', 'Skill')}</th><th>{t('Урон', 'Damage')}</th>
              <th>{t('Крит', 'Crit')}</th><th>{t('Дистанция', 'Range')}</th><th>{t('Качества', 'Qualities')}</th><th>{t('Действие', 'Action')}</th>
            </tr></thead><tbody>{attacks.map((w, i) => <tr key={i} className="npc-weapon">
              <td><strong>{w.name}</strong></td><td>{w.pool
                      ? <span className="weapon-pool" title={w.skillLabel ? t(`Бросок: ${w.skillLabel}`, `Roll: ${w.skillLabel}`) : undefined}>
                          <DicePoolView pool={w.pool} />
                          {w.skillLabel && <span className="muted small-text">{w.skillLabel}</span>}
                        </span>
                      : w.skillLabel === null && <span className="muted small-text">{t('навык не указан', 'no skill set')}</span>}
                    </td><td>{w.damageText}</td><td>{w.crit || '—'}</td><td>{w.rangeBand || '—'}</td>
              <td>{w.qualities.length > 0 && (
                    <PropertyTags
                      properties={w.qualities.map(q => q.label).join(', ')}
                      className="weapon-props small-text"
                      qualityDefinitions={reference?.qualities ?? []} />
                  )}
                  {w.notes && <div className="muted small-text"><RuleText symbolsOnly text={w.notes} /></div>}</td><td>{canAttack && <button type="button" className="small no-print" onClick={() => openRoller({
                      kind: 'combat',
                      title: w.name,
                      skillLabel: w.skillLabel,
                      basePool: w.pool ?? {},
                      damage: n.attacks[i].damage,
                      brawn: n.brawn,
                      crit: n.attacks[i].critical,
                      rangeBand: n.attacks[i].rangeBand,
                      qualities: resolveQualityCosts(
                        n.attacks[i].qualities.map(q => ({ code: q.qualityCode, label: q.nameRu || q.qualityCode, rating: q.rating })),
                        reference),
                      onLog: n.isMine && n.campaignId
                        ? (req => { void api.createRoll(n.campaignId!, req) })
                        : undefined,
                      canSecret: n.isMine,
                    })}><Icon name="dice" />{t('Атаковать', 'Attack')}</button>}</td>
            </tr>)}</tbody></table></div>
          </div>
        )}

        {skills.length > 0 && (
          <div className="npc-section bt-section-stats">
            <h4>{n.kind === 'minion' ? t('Групповые навыки', 'Group skills') : t('Навыки', 'Skills')}</h4>
            <ul className="npc-skill-list">
              {skills.map((s, i) => (
                <li key={i} className="npc-skill-row">
                  <span className="npc-skill-name">
                    {s.name}
                    {(() => {
                      const original = index.get(s.name) ? secondaryName(index.get(s.name)!) : ''
                      return original && original !== s.name
                        ? <span className="muted small-text name-secondary"> · {original}</span>
                        : null
                    })()}
                    {' '}{n.kind !== 'minion' && <span className="muted">{s.ranks}</span>}
                    {s.characteristic && (
                      <span className="muted small-text"> · {CHARACTERISTIC_LABELS[s.characteristic]}</span>
                    )}
                  </span>
                  {s.pool
                    ? <DicePoolView pool={s.pool} />
                    : <span className="muted small-text">{t('пул не определён', 'pool undefined')}</span>}
                </li>
              ))}
            </ul>
          </div>
        )}

        </div>
        <div className="bt-card-right">
        {n.description && <p className="bt-mobile-desc npc-desc"><RuleText symbolsOnly text={n.description} /></p>}
        {n.abilities.length > 0 && (
          <div className="npc-section bt-section-abilities">
            <h4>{t('Способности', 'Abilities')}</h4>
            <ul>{n.abilities.map((a, i) => <li key={i}><b>{a.name}.</b> <RuleText symbolsOnly text={a.description} /></li>)}</ul>
          </div>
        )}
        {n.talents.length > 0 && (
          <div className="npc-section bt-section-talents"><h4>{t('Таланты', 'Talents')}</h4><div className="chips"><TalentTags values={n.talents} talents={reference?.talents ?? []} /></div></div>
        )}
        {gear.length > 0 && (
          <div className="npc-section bt-section-gear">
            <h4>{t('Снаряжение', 'Gear')}</h4>
            <ul className="npc-gear-list">
              {gear.map((g, i) => <NpcGearRow key={i} gear={g} qualityDefinitions={reference?.qualities ?? []} />)}
            </ul>
          </div>
        )}
        {n.tactics && (
          <div className="npc-section bt-section-tactics"><h4>{t('Тактика', 'Tactics')}</h4><p className="npc-desc">{n.tactics}</p></div>
        )}
        {n.tags.length > 0 && (
          <div className="npc-section bt-section-tags"><h4>{t('Теги', 'Tags')}</h4><div className="chips">{n.tags.map(t => <span key={t} className="chip">{t}</span>)}</div></div>
        )}
        {n.source && <div className="muted small-text npc-source bt-section-source">{t('Источник:', 'Source:')} {n.source}</div>}
        </div>
        </div>
      </div>
  )
}

/** Строка снаряжения NPC: имя, тип, бонусы брони и описание из каталога (если найдено). */
function NpcGearRow({ gear, qualityDefinitions }: {
  gear: NpcGearView
  qualityDefinitions: Reference['qualities']
}) {
  const { name, item } = gear
  const bonuses = item ? [
    item.soakBonus > 0 && t(`Поглощение +${item.soakBonus}`, `Soak +${item.soakBonus}`),
    item.meleeDefense > 0 && t(`Бл. защита +${item.meleeDefense}`, `Melee def. +${item.meleeDefense}`),
    item.rangedDefense > 0 && t(`Дал. защита +${item.rangedDefense}`, `Ranged def. +${item.rangedDefense}`),
    item.encumbranceThresholdBonus > 0 && t(`Порог веса +${item.encumbranceThresholdBonus}`, `Encumbrance +${item.encumbranceThresholdBonus}`),
  ].filter(Boolean) as string[] : []
  const description = item ? localizedDescription(item) : ''
  return (
    <li className="npc-gear">
      <div className="npc-gear-head">
        <strong>{name}</strong>
        {item && secondaryName(item) && <span className="muted small-text name-secondary">· {secondaryName(item)}</span>}
        {item && <span className="muted small-text">{ITEM_KIND_LABELS[item.kind]}</span>}
        {bonuses.length > 0 && <span className="npc-gear-bonus">{bonuses.join(' · ')}</span>}
      </div>
      {description && <div className="muted small-text npc-gear-desc">{description}</div>}
      {item && (item.properties || qualityProperties(item.qualities)) && <PropertyTags
        properties={item.properties || qualityProperties(item.qualities)} className="small-text"
        qualityDefinitions={qualityDefinitions} />}
    </li>
  )
}

function NpcMoreActions({ busy, onDuplicate, onDelete }: { busy: boolean; onDuplicate: () => void; onDelete: () => void }) {
  const [open, setOpen] = useState(false)
  const root = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    const close = (e: PointerEvent) => { if (!root.current?.contains(e.target as Node)) setOpen(false) }
    const key = (e: KeyboardEvent) => { if (e.key === 'Escape') { setOpen(false); root.current?.querySelector('button')?.focus() } }
    document.addEventListener('pointerdown', close)
    document.addEventListener('keydown', key)
    return () => { document.removeEventListener('pointerdown', close); document.removeEventListener('keydown', key) }
  }, [open])
  return <div ref={root} className="bt-more-actions">
    <button type="button" aria-expanded={open} onClick={() => setOpen(v => !v)}><Icon name="menu" />{t('Ещё', 'More')}</button>
    {open && <div className="bt-more-popup">
      <button type="button" disabled={busy} onClick={() => { setOpen(false); onDuplicate() }}><Icon name="copy" />{t('Дублировать', 'Duplicate')}</button>
      <button type="button" className="danger" disabled={busy} onClick={() => { setOpen(false); onDelete() }}><Icon name="trash" />{t('Удалить', 'Delete')}</button>
    </div>}
  </div>
}

const EMPTY_INPUT: NpcInput = {
  name: '', system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute', description: '', source: '',
  brawn: 2, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2,
  woundThreshold: 10, strainThreshold: 10, soak: 2, meleeDefense: 0, rangedDefense: 0,
  silhouette: 1, tactics: '',
  visibility: 'private', campaignId: null,
  skills: [], abilities: [], attacks: [], talents: [], equipment: [], tags: [],
}

function toInput(n: NpcDetail): NpcInput {
  const { id, isMine, isBuiltIn, createdAt, updatedAt, warnings, ...rest } = n
  void id; void isMine; void isBuiltIn; void createdAt; void updatedAt; void warnings
  return rest
}

const SECTIONS = [t('Основные данные', 'Basic information'), t('Характеристики', 'Characteristics'), t('Навыки и атаки', 'Skills and attacks'), t('Способности', 'Abilities'), t('Снаряжение', 'Gear'), t('Тактика', 'Tactics')]
const MOBILE_SECTIONS = [t('Основные данные', 'Basic information'), t('Характеристики', 'Characteristics'), t('Навыки', 'Skills'), t('Атаки', 'Attacks'), t('Ещё', 'More')]
const KIND_HINTS = { minion: t('группа, без усталости', 'group, no strain'), rival: t('одиночный противник', 'single adversary'), nemesis: t('босс, усталость', 'boss, has strain') }

function KindCards({ value, onChange }: { value: NpcKind; onChange: (kind: NpcKind) => void }) {
  return <div className="bt-kind-cards">{NPC_KINDS.map(kind => <button className={toneClass('npc', kind)} key={kind} type="button" aria-pressed={value === kind} onClick={() => onChange(kind)}><strong className={`kind-${kind} ${toneClass('npc', kind)}`}>{NPC_KIND_LABELS[kind]}</strong><small className="muted">{KIND_HINTS[kind]}</small></button>)}</div>
}

function NpcCreationPage({ id, query, onBack, onSaved }: { id: string | null; query: string; onBack: () => void; onSaved: (n: NpcDetail) => void }) {
  const [loaded, setLoaded] = useState<{ id: string; npc: NpcDetail } | null>(null)
  const [working, setWorking] = useState<NpcInput | null>(null)
  const [creationBaseline, setCreationBaseline] = useState(EMPTY_INPUT)
  const saved = useRef(false)
  const [request, setRequest] = useState<QuickDraftRequest>({ system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute', powerLevel: 'standard', primaryCharacteristic: null, combatStyle: 'melee', name: null, template: 'none', magicSkill: null, environment: null })
  const [error, setError] = useState<string | null>(null)
  useEffect(() => {
    if (!id) return
    let cancelled = false
    api.npc(id).then(npc => { if (!cancelled) setLoaded({ id, npc }) }).catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : t('Ошибка загрузки', 'Loading failed')) })
    return () => { cancelled = true }
  }, [id])
  const mode = id ? 'manual' : new URLSearchParams(query).get('mode') === 'draft' ? 'draft' : 'manual'
  const changeMode = (mode: string) => {
    const p = new URLSearchParams(query); p.set('mode', mode)
    navigate(`/npcs/new?${p}`)
  }
  const baseline = id && loaded?.id === id ? toInput(loaded.npc) : creationBaseline
  const dirty = working !== null && (JSON.stringify(working) !== JSON.stringify(baseline) || (!id && !!working.name.trim()))
  const dirtyRef = useRef(dirty)
  useEffect(() => { dirtyRef.current = dirty }, [dirty])
  useEffect(() => {
    const cleanup = registerNavigationGuard(to => saved.current || (!id && to.split('?')[0] === '/npcs/new') || !dirtyRef.current || confirm(t('Есть несохранённые изменения. Покинуть редактор?', 'You have unsaved changes. Leave the editor?')))
    const unload = (e: BeforeUnloadEvent) => { if (dirtyRef.current && !saved.current) { e.preventDefault(); e.returnValue = '' } }
    window.addEventListener('beforeunload', unload)
    return () => { cleanup(); window.removeEventListener('beforeunload', unload) }
  }, [id])
  if (error) return <div className="bt-builder"><button onClick={onBack}>{t('← Бестиарий', '← Bestiary')}</button><p role="alert" className="error">{error}</p></div>
  if (id && loaded?.id !== id) return <div className="bt-skeleton bt-card-skeleton" aria-label={t('Загрузка NPC', 'Loading NPC')} />
  if (id && !loaded?.npc.isMine) return <div className="bt-builder"><button onClick={onBack}>{t('← Бестиарий', '← Bestiary')}</button><p>{t('Этот NPC доступен только для чтения. Создайте копию в библиотеке.', 'This NPC is read-only. Clone it into your library first.')}</p></div>
  return mode === 'draft' ? <QuickDraftForm req={request} onRequestChange={setRequest} onModeChange={changeMode} onCancel={onBack}
    onCreated={n => { const input = toInput(n); setWorking(input); setCreationBaseline(input); changeMode('manual') }} />
    : <NpcEditor key={id ?? 'new'} initial={id ? toInput(loaded!.npc) : working ?? { ...EMPTY_INPUT, kind: request.kind }} savedId={id}
      dirty={dirty} onChange={setWorking} onKindChange={kind => setRequest(r => ({ ...r, kind }))} onModeChange={id ? undefined : changeMode}
      onCancel={id ? () => navigate(`/npcs/${id}${bestiaryQuery(readBestiaryFilters(query))}`) : onBack}
      onSaved={n => { saved.current = true; onSaved(n) }} />
}

function NpcEditor({ initial, savedId, dirty, onCancel, onSaved, onChange, onKindChange, onModeChange }: {
  initial: NpcInput; savedId: string | null; onCancel: () => void; onSaved: (saved: NpcDetail) => void
  dirty: boolean
  onChange: (input: NpcInput) => void; onKindChange: (kind: NpcKind) => void; onModeChange?: (mode: string) => void
}) {
  const [form, setForm] = useState<NpcInput>(initial)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [template, setTemplate] = useState<CreatureTemplate>('none')
  const [applying, setApplying] = useState(false)
  const [loaded, setLoaded] = useState<{ system: GameSystem; data: Reference } | null>(null)
  const [mobileTab, setMobileTab] = useState(0)
  const [activeSection, setActiveSection] = useState(0)
  const [expanded, setExpanded] = useState<number[]>([])
  const [previewOpen, setPreviewOpen] = useState(false)
  const [invalidSection, setInvalidSection] = useState<number | null>(null)
  const element = useRef<HTMLFormElement>(null)
  const invalidPending = useRef(false)
  const [manualSoak, setManualSoak] = useState(savedId !== null)
  const [initialForm] = useState(initial)
  const reference = loaded?.system === form.system ? loaded.data : null
  const weaponMap = useMemo(() => weaponsByLabel(reference), [reference])
  const index = useMemo(() => skillIndex(reference), [reference])
  const naturalSoak = !savedId && reference ? Math.max(0, initialForm.soak - suggestedNpcSoak(initialForm, reference)) : 0
  const minion = form.kind === 'minion'
  const hasStrain = form.kind === 'nemesis'
  function change(next: NpcInput) { setForm(next); setError(null); setInvalidSection(null); onChange(next) }
  const set = <K extends keyof NpcInput>(key: K, value: NpcInput[K]) => {
    const next = { ...form, [key]: value }
    if (key === 'soak') setManualSoak(true)
    if (key === 'brawn' && !manualSoak) next.soak = suggestedNpcSoak(next, reference) + naturalSoak
    if (key === 'kind') { onKindChange(value as NpcKind); if (value === 'minion') next.skills = next.skills.map(skill => ({ ...skill, ranks: 0 })) }
    change(next)
  }
  useEffect(() => {
    let cancelled = false
    api.reference(form.system).then(data => { if (!cancelled) setLoaded({ system: form.system, data }) }).catch(() => {})
    return () => { cancelled = true }
  }, [form.system])
  useEffect(() => {
    const shortcut = (e: KeyboardEvent) => { if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') { e.preventDefault(); element.current?.requestSubmit() } }
    window.addEventListener('keydown', shortcut)
    return () => window.removeEventListener('keydown', shortcut)
  }, [])
  useEffect(() => {
    if (typeof IntersectionObserver === 'undefined') return
    const observer = new IntersectionObserver(entries => {
      const first = entries.filter(e => e.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)[0]
      if (first) setActiveSection(Number((first.target as HTMLElement).dataset.section))
    }, { rootMargin: '-15% 0px -50% 0px' })
    element.current?.querySelectorAll('[data-section]').forEach(section => observer.observe(section))
    return () => observer.disconnect()
  }, [])
  const setEquipment = (equipment: string[]) => {
    const next = { ...form, equipment, attacks: syncAttacksWithEquipment(equipment, form.attacks, weaponMap) }
    if (!manualSoak) next.soak = suggestedNpcSoak(next, reference) + naturalSoak
    change(next)
  }
  async function applyTemplate() {
    if (template === 'none') return
    setApplying(true); setError(null)
    try { setManualSoak(true); change(toInput(await api.applyNpcTemplate({ ...form, strainThreshold: hasStrain ? form.strainThreshold : null }, template))) }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка применения шаблона', 'Failed to apply template')) }
    finally { setApplying(false) }
  }
  async function submit(e: FormEvent) {
    e.preventDefault()
    if (busy || !form.name.trim()) return
    setBusy(true); setError(null)
    try {
      const payload = { ...form, strainThreshold: hasStrain ? form.strainThreshold : null,
        skills: form.skills.map(s => ({ ...s, name: npcSkillName(s.name, index), ranks: minion ? 0 : s.ranks })) }
      const n = savedId ? await api.updateNpc(savedId, payload) : await api.createNpc(payload)
      onSaved(n)
    } catch (err) {
      const message = err instanceof Error ? err.message : t('Ошибка сохранения', 'Failed to save')
      setError(message)
      const attackError = /атака|attack|крит|critical/i.test(message)
      const section = attackError || /навык|skill|ранг|rank/i.test(message) ? 2 : /характеристик|characteristic|порог|threshold|защит|defense|поглощ|soak|силуэт|silhouette|усталост|strain/i.test(message) ? 1 : 0
      setInvalidSection(section); setActiveSection(section)
      setMobileTab(section === 2 ? (attackError ? 3 : 2) : section)
      requestAnimationFrame(() => {
        const panel = document.getElementById(`npc-section-${section}`)
        panel?.querySelector<HTMLElement>('[aria-invalid=true],input:not(:disabled),button[role=combobox]:not(:disabled),textarea')?.focus()
        panel?.scrollIntoView({ block: 'center', behavior: 'smooth' })
      })
    } finally { setBusy(false) }
  }
  const goSection = (i: number) => {
    setActiveSection(i)
    if (i > 2) setExpanded(x => [...new Set([...x, i])])
    requestAnimationFrame(() => document.getElementById(`npc-section-${i}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' }))
  }
  const preview: NpcDetail = { ...form, strainThreshold: hasStrain ? form.strainThreshold : null, id: '', isMine: true, isBuiltIn: false, warnings: [], createdAt: '', updatedAt: '' }
  const completed = [!!form.name.trim(), true, form.skills.some(s => s.name) || !!form.attacks.length, !!(form.abilities.length || form.talents.length), !!form.equipment.length, !!(form.tactics || form.tags.length || form.source)]
  return <form ref={element} className={`bt-builder bt-manual mobile-tab-${mobileTab}`} onSubmit={submit} onInvalid={e => {
      e.preventDefault()
      if (invalidPending.current) return
      invalidPending.current = true
      queueMicrotask(() => { invalidPending.current = false })
      const target = e.target as HTMLElement
      const section = Number(target.closest('[data-section]')?.getAttribute('data-section') ?? 0)
      setError(t('Проверьте выделенное поле перед сохранением.', 'Check the highlighted field before saving.'))
      setInvalidSection(section); setActiveSection(section)
      setMobileTab(section === 2 ? (target.closest('.bt-editor-attacks') ? 3 : 2) : section > 2 ? 4 : section)
      if (section > 2) setExpanded(x => [...new Set([...x, section])])
      requestAnimationFrame(() => { target.focus(); target.scrollIntoView({ block: 'center', behavior: 'smooth' }) })
    }}>
    <div className="bt-builder-head"><button type="button" className="bt-back" onClick={onCancel}><Icon name="arrow-left" /><span>{t('Бестиарий', 'Bestiary')}</span></button>
      <div className="bt-builder-title"><h2>{form.name.trim() || t('Новый NPC', 'New NPC')}</h2><small className="muted">{NPC_KIND_LABELS[form.kind]} · {NPC_ROLE_LABELS[form.role]}{dirty && t(' · не сохранён', ' · unsaved')}</small></div>
      {onModeChange && <div className="bt-pills bt-mode"><button type="button" onClick={() => onModeChange('draft')}><Icon name="bolt" />{t('Быстрый черновик', 'Quick draft')}</button><button type="button" aria-pressed="true">{t('Вручную', 'Manual')}</button></div>}
      <div className="bt-actions bt-desktop-save"><button type="button" onClick={onCancel}>{t('Отмена', 'Cancel')}</button><button className="primary" type="submit" disabled={busy || !form.name.trim()}>{t('Сохранить', 'Save')}</button></div>
    </div>
    <nav className="bt-pills bt-section-nav" aria-label={t('Разделы редактора', 'Editor sections')}>{SECTIONS.map((label, i) => <button type="button" key={label} aria-current={activeSection === i ? 'step' : undefined} onClick={() => goSection(i)}><b>{i + 1}</b>{label}{completed[i] && <Icon name="check" />}</button>)}</nav>
    <div className="bt-pills bt-editor-tabs" role="tablist" aria-label={t('Вкладки редактора', 'Editor tabs')}>{MOBILE_SECTIONS.map((label, i) => <button type="button" role="tab" key={label} aria-selected={mobileTab === i} onClick={() => setMobileTab(i)}>{label}</button>)}</div>
    {error && <div className="error bt-save-error" role="alert">{error}</div>}
    <div className="bt-manual-layout"><div className="bt-editor-body" data-invalid-section={invalidSection ?? undefined}>
      <section id="npc-section-0" data-section="0" className="bt-editor-section bt-editor-basics"><h4>1 · {t('Основные данные', 'Basic information')}</h4>
        <div className="bt-base-grid"><label>{t('Имя', 'Name')}<input name="npc-name" aria-invalid={!!error && !form.name.trim()} required value={form.name} onChange={e => set('name', e.target.value)} /></label>
          <div><span className="bt-field-label">{t('Система', 'System')}</span><div className="bt-system-options">{SYSTEMS.map(system => <button type="button" key={system} aria-pressed={form.system === system} onClick={() => set('system', system)}>{system === 'genesysCore' ? 'Genesys Core' : 'Terrinoth'}</button>)}</div></div></div>
        <div><span className="bt-field-label">{t('Тип', 'Type')}</span><KindCards value={form.kind} onChange={kind => set('kind', kind)} /></div>
        <div className="bt-two-fields"><label>{t('Роль', 'Role')}<Select value={form.role} onChange={value => set('role', value as NpcRole)}>{NPC_ROLES.map(role => <option key={role} value={role}>{NPC_ROLE_LABELS[role]}</option>)}</Select></label>
          <label>{t('Видимость', 'Visibility')}<Select value={form.visibility} onChange={value => set('visibility', value as NpcVisibility)}>{(['private', 'campaignVisible', 'publicTemplate'] as NpcVisibility[]).map(v => <option key={v} value={v}>{NPC_VISIBILITY_LABELS[v]}</option>)}</Select></label></div>
        <label>{t('Описание', 'Description')}<textarea rows={2} value={form.description} onChange={e => set('description', e.target.value)} /></label>
        <div className="bt-template"><Icon name="sparkles" /><span className="muted">{t('Шаблон добавит теги, способности и природную атаку', 'A template adds tags, abilities and a natural attack')}</span><Select aria-label={t('Тип существа (шаблон)', 'Creature type (template)')} value={template} onChange={value => setTemplate(value as CreatureTemplate)}>{CREATURE_TEMPLATES.map(c => <option key={c} value={c}>{CREATURE_TEMPLATE_LABELS[c]}</option>)}</Select><button type="button" disabled={template === 'none' || applying} onClick={() => void applyTemplate()}>{t('Применить', 'Apply')}</button></div>
      </section>
      <section id="npc-section-1" data-section="1" className="bt-editor-section bt-editor-stats"><h4>2 · {t('Характеристики и пороги', 'Characteristics and thresholds')}</h4>
        <div className="bt-stat-grid">{CHARACTERISTICS.map(c => <NumberStepper key={c} className="bt-stat-stepper" label={CHARACTERISTIC_LABELS[c]} value={form[c]} min={1} max={6} onChange={v => set(c, v)} />)}</div>
        <div className="bt-derived-grid">
          <label>{t('Раны', 'Wounds')}<input type="number" inputMode="numeric" min={1} value={form.woundThreshold} onChange={e => set('woundThreshold', Math.max(1, +e.target.value))} /></label>
          <label>{t('Усталость', 'Strain')}<input type="number" inputMode="numeric" min={form.kind === 'nemesis' ? 1 : 0} required={form.kind === 'nemesis'} disabled={!hasStrain} value={hasStrain ? form.strainThreshold ?? '' : ''} placeholder={hasStrain ? '' : '—'} onChange={e => set('strainThreshold', e.target.value === '' ? null : Math.max(0, +e.target.value))} /></label>
          <label>{t('Поглощение', 'Soak')}<input type="number" inputMode="numeric" min={0} value={form.soak} onChange={e => set('soak', Math.max(0, +e.target.value))} /></label>
          <label>{t('Бл. защита', 'Melee def.')}<input aria-invalid={invalidSection === 1 && /защит|defense/i.test(error ?? '')} type="number" inputMode="numeric" min={0} value={form.meleeDefense} onChange={e => set('meleeDefense', Math.max(0, +e.target.value))} /></label>
          <label>{t('Дал. защита', 'Ranged def.')}<input aria-invalid={invalidSection === 1 && /защит|defense/i.test(error ?? '')} type="number" inputMode="numeric" min={0} value={form.rangedDefense} onChange={e => set('rangedDefense', Math.max(0, +e.target.value))} /></label>
          <label>{t('Силуэт', 'Silhouette')}<input type="number" inputMode="numeric" min={0} max={10} value={form.silhouette} onChange={e => set('silhouette', clamp(+e.target.value, 0, 10))} /></label>
        </div>
        <div className="bt-stat-note"><Icon name="info-circle" /><span>{manualSoak ? t('Поглощение задано вручную.', 'Soak is set manually.') : naturalSoak ? t('Поглощение учитывает Силу, броню и природный бонус черновика.', 'Soak includes Brawn, armor and the draft’s natural bonus.') : t('Поглощение пересчитывается из Силы и бонусов брони.', 'Soak follows Brawn and armor bonuses.')} {t('Усталость доступна только главарю.', 'Only nemeses have strain.')}</span><button type="button" onClick={() => { setManualSoak(false); change({ ...form, soak: suggestedNpcSoak(form, reference) + naturalSoak }) }}>{t('Авто', 'Auto')}</button></div>
      </section>
      <section id="npc-section-2" data-section="2" className="bt-editor-section bt-editor-combat"><h4>3 · {t('Навыки и атаки', 'Skills and attacks')}</h4>
        <div className="bt-editor-skills"><SkillsEditor skills={form.skills} available={reference?.skills ?? []} characteristics={form} groupSkills={minion} onChange={s => set('skills', s)} /></div>
        <div className="bt-editor-attacks"><AttacksEditor attacks={form.attacks} skills={reference?.skills ?? []} qualities={reference?.qualities ?? []} onChange={a => set('attacks', a)} /></div>
      </section>
      {[3, 4, 5].map(i => <section id={`npc-section-${i}`} data-section={i} key={i} className={`bt-editor-section bt-editor-more${expanded.includes(i) ? ' expanded' : ''}`}>
        <button type="button" className="bt-accordion" aria-expanded={expanded.includes(i)} onClick={() => setExpanded(x => x.includes(i) ? x.filter(n => n !== i) : [...x, i])}><h4>{i + 1} · {SECTIONS[i]}</h4><small className="muted">{i === 3 ? form.abilities.length + form.talents.length : i === 4 ? form.equipment.length : form.tags.length}</small><Icon name={expanded.includes(i) ? 'chevron-down' : 'chevron-right'} /></button>
        <div className="bt-accordion-content">{i === 3 ? <><AbilitiesEditor abilities={form.abilities} onChange={a => set('abilities', a)} />
          <PickListEditor label={t('Таланты', 'Talents')} values={form.talents} options={reference?.talents ?? []} onChange={v => set('talents', v)} placeholder={t('Выберите талант из списка', 'Pick a talent from the list')}>
            <TalentTags values={form.talents} talents={reference?.talents ?? []} onRemove={i => set('talents', form.talents.filter((_, j) => j !== i))} />
          </PickListEditor></> : i === 4 ? <>
          <PickListEditor label={t('Снаряжение', 'Gear')} values={form.equipment} options={reference?.items ?? []} onChange={setEquipment} placeholder={t('Выберите предмет из списка', 'Pick an item from the list')} />
        </> : <><label>{t('Тактика (1–3 раунда)', 'Tactics (1–3 rounds)')}<textarea rows={2} value={form.tactics} onChange={e => set('tactics', e.target.value)} /></label><StringListEditor label={t('Теги', 'Tags')} values={form.tags} onChange={v => set('tags', v)} placeholder={t('Тег', 'Tag')} /><label>{t('Источник', 'Source')}<input value={form.source} onChange={e => set('source', e.target.value)} /></label></>}</div>
      </section>)}
    </div>
    <aside className="bt-manual-preview"><NpcEditorSummary npc={preview} index={index} title={t('Карточка NPC', 'NPC card')} /></aside></div>
    <div className="bt-mobile-save"><button type="button" onClick={() => setPreviewOpen(true)}>{t('Карточка', 'Card')}</button><button className="primary" type="submit" disabled={busy || !form.name.trim()}>{t('Сохранить', 'Save')}</button></div>
    {previewOpen && <NpcInputPreview npc={preview} reference={reference} onClose={() => setPreviewOpen(false)} />}
  </form>
}

function NpcEditorSummary({ npc, index, title }: { npc: NpcDetail; index: Map<string, SkillDef>; title: string }) {
  const skills = npcSkillViews(npc, index)
  return <div className="npc-editor-summary">
    <header className="npc-preview-plate plate"><small>{title}</small><h3>{npc.name.trim() || t('Новый NPC', 'New NPC')}</h3>
      <p>{NPC_KIND_LABELS[npc.kind]} · {NPC_ROLE_LABELS[npc.role]} · {SYSTEM_LABELS[npc.system]}</p></header>
    <div className="npc-preview-chars">{CHARACTERISTICS.map(c => <div key={c}><small title={CHARACTERISTIC_LABELS[c]}>{CHARACTERISTIC_SHORT_LABELS[c]}</small><strong>{npc[c]}</strong></div>)}</div>
    <div className="npc-preview-derived">{[[t('Раны', 'Wounds'), npc.woundThreshold], [t('Усталость', 'Strain'), npc.strainThreshold ?? '—'], [t('Поглощение', 'Soak'), npc.soak], [t('Защита', 'Defense'), `${npc.meleeDefense} / ${npc.rangedDefense}`]].map(([label, value]) => <div key={label}><strong>{value}</strong><small>{label}</small></div>)}</div>
    <dl className="npc-preview-details">
      <div><dt>{t('Навыки', 'Skills')}</dt><dd>{skills.map(s => s.name).join(', ') || '—'}</dd></div>
      <div><dt>{t('Атаки', 'Attacks')}</dt><dd>{npc.attacks.map(a => a.name).join(', ') || '—'}</dd></div>
      <div><dt>{t('Таланты', 'Talents')}</dt><dd>{npc.talents.join(', ') || '—'}</dd></div>
      <div><dt>{t('Снаряжение', 'Gear')}</dt><dd>{npc.equipment.join(', ') || '—'}</dd></div>
    </dl>
  </div>
}

function NpcInputPreview({ npc, reference, onClose }: { npc: NpcDetail; reference: Reference | null; onClose: () => void }) {
  const dialog = useRef<HTMLDialogElement>(null)
  useEffect(() => { const d = dialog.current; d?.showModal(); return () => d?.close() }, [])
  return <dialog ref={dialog} className="bt-preview-dialog" onCancel={onClose} aria-label={t('Карточка NPC', 'NPC card')}>
    <div className="bt-detail-head"><h3>{npc.name || t('Новый NPC', 'New NPC')}</h3>
      <button type="button" className="bt-icon" aria-label={t('Закрыть карточку', 'Close card')} onClick={onClose}><Icon name="close" /></button></div>
    <span className="bt-card-meta"><span className={`bt-kind kind-${npc.kind} ${toneClass('npc', npc.kind)}`}>{NPC_KIND_LABELS[npc.kind]}</span>
      <span className="bt-meta-badge">{NPC_ROLE_LABELS[npc.role]}</span><span className={npc.system === 'genesysCore' ? 'bt-core' : 'bt-terrinoth'}>{SYSTEM_LABELS[npc.system]}</span></span>
    <NpcCard npc={npc} reference={reference} canAttack={false} />
  </dialog>
}

// Отображаемое имя справочной записи на языке интерфейса.
// ВАЖНО: используется и как сохраняемое значение equipment NPC — сопоставление
// при чтении принимает оба имени (см. npcStats.matchesRef).
const refLabel = (o: { name: string; nameRu: string }) => o.nameRu?.trim() && t(o.nameRu, o.name) || o.name

function SkillsEditor({ skills, available, characteristics, groupSkills = false, onChange }: {
  skills: NpcInput['skills']; available: SkillDef[]; characteristics: NpcInput
  groupSkills?: boolean
  onChange: (s: NpcInput['skills']) => void
}) {
  const index = skillIndex({ skills: available })
  return (
    <div className="list-editor">
      <div className="label-line">{groupSkills ? t('Групповые навыки', 'Group skills') : t('Навыки', 'Skills')}</div>
      {groupSkills && <div className="muted small-text">{t('Приспешники используют групповые навыки без рангов (ранг = размер группы − 1).', 'Minions use group skills without ranks (rank = group size − 1).')}</div>}
      {skills.map((s, i) => {
        // Текущее значение оставляем выбираемым, даже если его нет в справочнике (кастом/смена системы).
        const def = index.get(s.name)
        const orphan = s.name !== '' && !def
        const label = def ? refLabel(def) : s.name || String(i + 1)
        return (
          <div key={i} className="bt-skill-edit">
            <Select required aria-label={t(`Навык ${i + 1}`, `Skill ${i + 1}`)} className="grow" value={def?.name ?? s.name}
              onChange={value => onChange(skills.map((x, j) => j === i ? { ...x, name: value } : x))}>
              <option value="">{t('— выберите навык —', '— pick a skill —')}</option>
              {orphan && <option value={s.name}>{s.name} {t('(вне справочника)', '(not in reference)')}</option>}
              {available.map(sk => <option key={sk.id} value={sk.name}>{dualName(sk)}</option>)}
            </Select>
            {!groupSkills && <NumberStepper className="bt-rank-stepper" label={t(`Ранг: ${label}`, `Rank: ${label}`)} value={s.ranks} min={0} max={5} onChange={ranks => onChange(skills.map((x, j) => j === i ? { ...x, ranks } : x))} />}
            {def ? <span className="bt-skill-pool"><small>{CHARACTERISTIC_LABELS[def.characteristic]}</small><DicePoolView pool={buildPool(characteristics[def.characteristic], groupSkills ? 0 : s.ranks)} /></span> : <span className="muted small-text">—</span>}
            <button type="button" className="danger bt-remove" aria-label={t(`Удалить навык ${i + 1}`, `Remove skill ${i + 1}`)} onClick={() => onChange(skills.filter((_, j) => j !== i))}>×</button>
          </div>
        )
      })}
      <button type="button" className="small" disabled={available.length === 0}
        onClick={() => onChange([...skills, { name: '', ranks: groupSkills ? 0 : 1 }])}>
        {groupSkills ? t('+ Групповой навык', '+ Group skill') : t('+ Навык', '+ Skill')}
      </button>
    </div>
  )
}

/** Выбор значений из справочника (таланты, снаряжение): чипы выбранного + выпадающий список доступного. */
function PickListEditor({ label, values, options, onChange, placeholder, children }: {
  label: string; values: string[]; options: { name: string; nameRu: string }[]
  onChange: (v: string[]) => void; placeholder: string; children?: ReactNode
}) {
  const remaining = options.filter(o => !values.includes(refLabel(o)))
  return (
    <div className="list-editor">
      <div className="label-line">{label}</div>
      {children ?? <div className="chips">
        {values.map((v, i) => (
          <span key={i} className="chip removable">
            {v}<button type="button" onClick={() => onChange(values.filter((_, j) => j !== i))}>×</button>
          </span>
        ))}
      </div>}
      <Select className="grow" aria-label={label} value="" disabled={remaining.length === 0}
        onChange={value => { if (value) onChange([...values, value]) }}>
        <option value="">
          {options.length === 0 ? t('Справочник загружается…', 'Reference loading…')
            : remaining.length === 0 ? t('Все доступные уже добавлены', 'Everything available is already added') : placeholder}
        </option>
        {remaining.map(o => <option key={o.name} value={refLabel(o)}>{dualName(o)}</option>)}
      </Select>
    </div>
  )
}

function AbilitiesEditor({ abilities, onChange }: { abilities: NpcInput['abilities']; onChange: (a: NpcInput['abilities']) => void }) {
  return (
    <div className="list-editor">
      <div className="label-line">{t('Способности', 'Abilities')}</div>
      {abilities.map((a, i) => (
        <div key={i} className="ability-edit">
          <div className="form-row">
            <input className="grow" placeholder={t('Название способности', 'Ability name')} value={a.name}
              onChange={e => onChange(abilities.map((x, j) => j === i ? { ...x, name: e.target.value } : x))} />
            <button type="button" className="danger small" onClick={() => onChange(abilities.filter((_, j) => j !== i))}>×</button>
          </div>
          <textarea placeholder={t('Описание', 'Description')} rows={2} value={a.description}
            onChange={e => onChange(abilities.map((x, j) => j === i ? { ...x, description: e.target.value } : x))} />
        </div>
      ))}
      <button type="button" className="small" onClick={() => onChange([...abilities, { name: '', description: '' }])}>{t('+ Способность', '+ Ability')}</button>
    </div>
  )
}

const EMPTY_ATTACK: NpcAttackEntry = {
  name: '', skillName: '', damage: '', critical: '', rangeBand: '', notes: '', qualities: [], sourceWeapon: '',
}

/** Редактор структурных атак NPC: производные из снаряжения (бейдж) + кастомные (имя, навык, урон…). */
function AttacksEditor({ attacks, skills, qualities, onChange }: {
  attacks: NpcAttackEntry[]; skills: SkillDef[]; qualities: Quality[]
  onChange: (a: NpcAttackEntry[]) => void
}) {
  const upd = (i: number, patch: Partial<NpcAttackEntry>) =>
    onChange(attacks.map((x, j) => j === i ? { ...x, ...patch } : x))
  return (
    <div className="list-editor">
      <div className="label-line">{t('Атаки', 'Attacks')}</div>
      <div className="muted small-text">{t('Оружие из «Снаряжения» добавляет атаку автоматически; «+ Атака» — кастомная.', 'Weapons from “Gear” add an attack automatically; “+ Attack” is custom.')}</div>
      {attacks.map((a, i) => {
        const remaining = qualities.filter(q => !a.qualities.some(x => x.qualityCode === q.code))
        const derived = a.sourceWeapon !== ''
        return (
          <div key={i} className="ability-edit">
            {derived && <div className="muted small-text icon-text"><Icon name="sword" className="button-icon" />{t(`из снаряжения: «${a.sourceWeapon}» (убрать — удалить оружие из инвентаря)`, `from gear: "${a.sourceWeapon}" (to remove it, delete the weapon from gear)`)}</div>}
            <div className="form-row">
              <input required className="grow" placeholder={t('Название атаки/оружия', 'Attack/weapon name')} value={a.name}
                onChange={e => upd(i, { name: e.target.value })} />
              {!derived && <button type="button" className="danger small" onClick={() => onChange(attacks.filter((_, j) => j !== i))}>×</button>}
            </div>
            <div className="form-row">
              <Select required aria-label={t(`Навык атаки ${i + 1}`, `Attack skill ${i + 1}`)} className="grow" value={a.skillName} onChange={value => upd(i, { skillName: value })}>
                <option value="">{t('— навык броска —', '— roll skill —')}</option>
                {a.skillName !== '' && !skills.some(s => s.name === a.skillName) &&
                  <option value={a.skillName}>{a.skillName} {t('(вне справочника)', '(not in reference)')}</option>}
                {skills.filter(s => s.kind === 'combat' || s.kind === 'magic').map(s => (
                  <option key={s.name} value={s.name}>{dualName(s)}</option>
                ))}
              </Select>
            </div>
            <div className="form-row">
              <label className="char-input">{t('Урон', 'Damage')}<input required value={a.damage} placeholder="+3 / 7"
                onChange={e => upd(i, { damage: e.target.value })} /></label>
              <label className="char-input">{t('Крит', 'Crit')}<input value={a.critical} placeholder="2"
                onChange={e => upd(i, { critical: e.target.value })} /></label>
              <label className="char-input grow">{t('Дистанция', 'Range')}<input required value={a.rangeBand} placeholder={t('Вплотную', 'Engaged')}
                onChange={e => upd(i, { rangeBand: e.target.value })} /></label>
            </div>
            <div className="chips npc-attack-qualities">
              {a.qualities.map((q, k) => (
                <span key={k} className="chip removable">
                  {q.nameRu || q.qualityCode}
                  {q.rating != null && <input type="number" min={1} className="ranks-input" value={q.rating}
                    onChange={e => upd(i, { qualities: a.qualities.map((x, m) => m === k ? { ...x, rating: Math.max(1, +e.target.value) } : x) })} />}
                  <button type="button" onClick={() => upd(i, { qualities: a.qualities.filter((_, m) => m !== k) })}>×</button>
                </span>
              ))}
            </div>
            <Select className="grow" value="" disabled={remaining.length === 0}
              onChange={value => {
                const q = qualities.find(x => x.code === value)
                if (q) upd(i, { qualities: [...a.qualities, { qualityCode: q.code, nameRu: q.nameRu || q.nameEn, rating: q.hasRating ? 1 : null }] })
              }}>
              <option value="">{qualities.length === 0 ? t('Справочник качеств загружается…', 'Qualities reference loading…') : remaining.length === 0 ? t('Все качества добавлены', 'All qualities added') : t('Добавить качество', 'Add quality')}</option>
              {remaining.map(q => <option key={q.code} value={q.code}>{t(q.nameRu || q.nameEn, q.nameEn || q.nameRu)}</option>)}
            </Select>
            <input placeholder={t('Заметки по атаке', 'Attack notes')} value={a.notes}
              onChange={e => upd(i, { notes: e.target.value })} />
          </div>
        )
      })}
      <button type="button" className="small" onClick={() => onChange([...attacks, { ...EMPTY_ATTACK }])}>{t('+ Атака', '+ Attack')}</button>
    </div>
  )
}

function StringListEditor({ label, values, onChange, placeholder }: {
  label: string; values: string[]; onChange: (v: string[]) => void; placeholder: string
}) {
  const [draft, setDraft] = useState('')
  function add() {
    const v = draft.trim()
    if (!v) return
    onChange([...values, v]); setDraft('')
  }
  return (
    <div className="list-editor">
      <div className="label-line">{label}</div>
      <div className="chips">
        {values.map((v, i) => (
          <span key={i} className="chip removable">
            {v}<button type="button" onClick={() => onChange(values.filter((_, j) => j !== i))}>×</button>
          </span>
        ))}
      </div>
      <div className="form-row">
        <input className="grow" placeholder={placeholder} value={draft} onChange={e => setDraft(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); add() } }} />
        <button type="button" className="small" onClick={add}>+</button>
      </div>
    </div>
  )
}

/** Подсказки ролевых профилей: чем занята роль и какой боевой стиль ей соответствует. */
const ROLE_HINTS: Record<NpcRole, { sub: string; style: NpcCombatStyle }> = t({
  brute: { sub: 'ближний бой, давление, живучесть', style: 'melee' },
  skirmisher: { sub: 'мобильность, фланги, защита', style: 'melee' },
  archer: { sub: 'дистанция, укрытие, фокус', style: 'ranged' },
  caster: { sub: 'заклинания, воля, дальняя угроза', style: 'magic' },
  leader: { sub: 'лидерство, приказы, координация', style: 'melee' },
  social: { sub: 'обман, переговоры, влияние', style: 'social' },
  support: { sub: 'лечение, поддержка, помощь', style: 'magic' },
  monster: { sub: 'природные атаки, шаблон существа', style: 'melee' },
  custom: { sub: 'свой профиль — настройте вручную', style: 'melee' },
}, {
  brute: { sub: 'melee, pressure, durability', style: 'melee' },
  skirmisher: { sub: 'mobility, flanking, defense', style: 'melee' },
  archer: { sub: 'range, cover, focus fire', style: 'ranged' },
  caster: { sub: 'spells, willpower, ranged threat', style: 'magic' },
  leader: { sub: 'leadership, orders, coordination', style: 'melee' },
  social: { sub: 'deception, negotiation, influence', style: 'social' },
  support: { sub: 'healing, support, assistance', style: 'magic' },
  monster: { sub: 'natural attacks, creature template', style: 'melee' },
  custom: { sub: 'your own profile — tune manually', style: 'melee' },
})

// Приоритет характеристик роли — зеркалит NpcDraftGenerator.PrimaryOf/SecondaryOf для подписи профиля.
const ROLE_PRIMARY: Record<NpcRole, Characteristic> = {
  brute: 'brawn', monster: 'brawn', skirmisher: 'agility', archer: 'agility',
  caster: 'willpower', support: 'willpower', leader: 'presence', social: 'presence', custom: 'brawn',
}
/**
 * Быстрый черновик NPC по прототипу npc-quick-draft: ролевые карточки с профилем,
 * параметры генерации и live preview результата (что именно будет создано и почему).
 */
const ROLE_ICONS: Record<NpcRole, IconName> = { brute: 'hammer', skirmisher: 'feather', archer: 'target', caster: 'magic', leader: 'crown', social: 'scroll', support: 'heart', monster: 'ghost-2', custom: 'adjustments' }

function QuickDraftForm({ req, onRequestChange, onCancel, onCreated, onModeChange }: {
  req: QuickDraftRequest; onRequestChange: (req: QuickDraftRequest) => void; onCancel: () => void; onCreated: (n: NpcDetail) => void; onModeChange: (mode: string) => void
}) {
  const name = req.name ?? ''
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [step, setStep] = useState(0)
  const [retry, setRetry] = useState(0)
  const set = <K extends keyof QuickDraftRequest>(key: K, value: QuickDraftRequest[K]) => onRequestChange({ ...req, [key]: value })
  const powerLevels: NpcPowerLevel[] = ['weak', 'standard', 'strong', 'elite']
  const pickRole = (role: NpcRole) => onRequestChange({ ...req, role, combatStyle: ROLE_HINTS[role].style })
  const [loaded, setLoaded] = useState<{ system: GameSystem; data: Reference } | null>(null)
  useEffect(() => {
    let cancelled = false
    api.reference(req.system).then(data => { if (!cancelled) setLoaded({ system: req.system, data }) }).catch(() => {})
    return () => { cancelled = true }
  }, [req.system])
  const reference = loaded?.system === req.system ? loaded.data : null
  const magicSkills = (reference?.skills ?? []).filter(s => s.kind === 'magic')
  const [result, setResult] = useState<{ request: string; npc: NpcDetail } | null>(null)
  const requestKey = JSON.stringify(req)
  const previewCurrent = result?.request === requestKey
  const preview = result?.npc ?? null
  useEffect(() => {
    let cancelled = false
    const timer = setTimeout(() => {
      api.previewQuickDraftNpc({ ...req, name: req.name?.trim() || null })
        .then(npc => { if (!cancelled) { setResult({ request: requestKey, npc }); setError(null) } })
        .catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : t('Ошибка предпросмотра', 'Preview failed')) })
    }, 250)
    return () => { cancelled = true; clearTimeout(timer) }
  }, [req, requestKey, retry])
  const index = useMemo(() => skillIndex(reference), [reference])
  const previewSkills = useMemo(() => preview ? npcSkillViews(preview, index) : [], [preview, index])
  async function submit(e: FormEvent) {
    e.preventDefault(); if (busy) return
    setBusy(true); setError(null)
    try { onCreated(previewCurrent && preview ? preview : await api.previewQuickDraftNpc({ ...req, name: name.trim() || null })) }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка генерации', 'Generation failed')); setBusy(false) }
  }
  return <form className={`bt-builder bt-draft draft-step-${step}`} onSubmit={submit}>
    <div className="bt-builder-head"><button type="button" className="bt-back" onClick={onCancel}><Icon name="arrow-left" /><span>{t('Бестиарий', 'Bestiary')}</span></button><div className="bt-builder-title"><h2><span className="bt-desktop-title">{t('Новый NPC', 'New NPC')}</span><span className="bt-mobile-step">{t('Быстрый черновик', 'Quick draft')}</span></h2><small className="muted bt-mobile-step">{t(`Шаг ${step + 1} из 3`, `Step ${step + 1} of 3`)} · {[t('Роль', 'Role'), t('Тип и сила', 'Type and power'), t('Уточнения', 'Refinements')][step]}</small></div><div className="bt-pills bt-mode"><button type="button" aria-pressed="true"><Icon name="bolt" />{t('Быстрый черновик', 'Quick draft')}</button><button type="button" onClick={() => onModeChange('manual')}>{t('Вручную', 'Manual')}</button></div></div>
    <div className="bt-wizard-progress" aria-label={t(`Шаг ${step + 1} из 3`, `Step ${step + 1} of 3`)}>{[0, 1, 2].map(i => <span key={i} className={i <= step ? 'active' : ''} />)}</div>
    {error && <div role="alert" className="error bt-save-error">{error}<button type="button" onClick={() => setRetry(v => v + 1)}>{t('Повторить', 'Retry')}</button></div>}
    <div className="bt-draft-layout"><div className="bt-draft-form">
      <section className="bt-draft-step bt-draft-roles"><h3><b>1</b> {t('Роль', 'Role')}</h3><p className="muted small-text">{t('Роль задаёт характеристики, навыки и стиль боя', 'The role defines characteristics, skills and combat style')}</p><div className="bt-role-grid">{NPC_ROLES.map(role => <button type="button" className={toneClass('role', role)} key={role} aria-pressed={req.role === role} onClick={() => pickRole(role)}><span className="bt-role-icon"><Icon name={ROLE_ICONS[role]} /></span><span><strong>{NPC_ROLE_LABELS[role]}</strong><small>{ROLE_HINTS[role].sub}</small></span></button>)}</div></section>
      <section className="bt-draft-step bt-draft-power"><h3><b>2</b> {t('Тип и сила', 'Type and power')}</h3><KindCards value={req.kind} onChange={kind => set('kind', kind)} /><div className="bt-power-grid">{powerLevels.map(power => <button type="button" key={power} aria-pressed={req.powerLevel === power} onClick={() => set('powerLevel', power)}>{NPC_POWER_LABELS[power]}</button>)}</div></section>
      <section className="bt-draft-step bt-draft-refinements"><h3><b>3</b> {t('Уточнения', 'Refinements')} <small className="muted">{t('необязательно', 'optional')}</small></h3><div className="bt-refinement-grid">
        <label>{t('Система', 'System')}<Select value={req.system} onChange={value => set('system', value as GameSystem)}>{SYSTEMS.map(system => <option key={system} value={system}>{SYSTEM_LABELS[system]}</option>)}</Select></label>
        <label>{t('Боевой стиль', 'Combat style')}<Select value={req.combatStyle} onChange={value => set('combatStyle', value as NpcCombatStyle)}>{(['melee', 'ranged', 'magic', 'social'] as NpcCombatStyle[]).map(style => <option key={style} value={style}>{NPC_COMBAT_STYLE_LABELS[style]}</option>)}</Select></label>
        <label>{t('Основная характеристика', 'Primary characteristic')}<Select value={req.primaryCharacteristic ?? ''} onChange={value => set('primaryCharacteristic', (value || null) as Characteristic | null)}><option value="">{t('По роли:', 'By role:')} {CHARACTERISTIC_LABELS[ROLE_PRIMARY[req.role]]}</option>{CHARACTERISTICS.map(c => <option key={c} value={c}>{CHARACTERISTIC_LABELS[c]}</option>)}</Select></label>
        <label>{t('Тип существа', 'Creature type')}<Select value={req.template ?? 'none'} onChange={value => set('template', value as CreatureTemplate)}>{CREATURE_TEMPLATES.map(c => <option key={c} value={c}>{CREATURE_TEMPLATE_LABELS[c]}</option>)}</Select></label>
        <label>{t('Имя (необязательно)', 'Name (optional)')}<input value={name} onChange={e => set('name', e.target.value || null)} placeholder={t('Сгенерируется автоматически', 'Generated automatically')} /></label>
        <label>{t('Окружение / тег (необязательно)', 'Environment / tag (optional)')}<input value={req.environment ?? ''} onChange={e => set('environment', e.target.value || null)} placeholder={t('лес, подземелье…', 'forest, dungeon…')} /></label>
        {req.combatStyle === 'magic' && <label>{t('Магшкола', 'Magic school')}<Select value={req.magicSkill ?? ''} onChange={value => set('magicSkill', value || null)}><option value="">{t('По умолчанию', 'Default')}</option>{magicSkills.map(s => <option key={s.name} value={s.name}>{dualName(s)}</option>)}</Select></label>}
      </div></section>
    </div>
          <aside className="qd-preview bt-draft-preview">
            {(!preview || !previewCurrent) && <><div className="npc-preview-loading">{t('Предпросмотр · обновляется…', 'Preview · updating…')}</div><div className="bt-skeleton bt-card-skeleton" aria-label={t('Загрузка предпросмотра', 'Loading preview')} /></>}
            {preview && previewCurrent && <NpcEditorSummary npc={preview} index={index} title={t('Предпросмотр', 'Preview')} />}
            {preview && previewCurrent && <><div className="muted small-text">{t('Будет добавлено ролью и стилем', 'Added by role and style')}</div><div className="bt-draft-counts">{[[preview.skills.length, t('навыков', 'skills'), previewSkills.map(s => s.name).join(', ')], [preview.equipment.length, t('предметов', 'items'), preview.equipment.join(', ')], [preview.abilities.length, t('способностей', 'abilities'), preview.abilities.map(a => a.name).join(', ')]].map(([count, label, names]) => <div key={label}><b>{count} {label}</b><small>{names || '—'}</small></div>)}</div></>}
            <button className="primary bt-create-draft-desktop" type="submit" disabled={busy || !previewCurrent}>{t('Создать черновик', 'Create draft')}</button><p className="muted small-text">{t('Откроется редактор. Сохраните NPC после проверки.', 'The editor opens next. Save the NPC after reviewing it.')}</p>
          </aside>
    </div>
    <div className="bt-wizard-actions"><div className="bt-wizard-summary" aria-live="polite"><span className={`bt-kind kind-${req.kind}`}>{NPC_KIND_LABELS[req.kind]}</span><span>{NPC_ROLE_LABELS[req.role]} · {CHARACTERISTIC_LABELS[req.primaryCharacteristic ?? ROLE_PRIMARY[req.role]]}</span><small>{previewCurrent && preview ? t(`${preview.woundThreshold} ран · погл. ${preview.soak}`, `${preview.woundThreshold} wounds · soak ${preview.soak}`) : t('Обновляется…', 'Updating…')}</small></div><div className="bt-actions">{step > 0 ? <button type="button" onClick={() => setStep(step - 1)}>{t('Назад', 'Back')}</button> : <button type="button" onClick={() => setStep(1)}>{t('Пропустить', 'Skip')}</button>}{step < 2 ? <button type="button" className="primary" onClick={event => { event.preventDefault(); setStep(step + 1) }}>{step === 0 ? t('Далее: тип и сила', 'Next: type and power') : t('Далее: уточнения', 'Next: refinements')}<Icon name="chevron-right" /></button> : <button type="submit" className="primary" disabled={busy || !previewCurrent}>{t('Создать черновик', 'Create draft')}</button>}</div></div>
  </form>
}

function clamp(v: number, min: number, max: number) {
  return Number.isNaN(v) ? min : Math.min(max, Math.max(min, v))
}
