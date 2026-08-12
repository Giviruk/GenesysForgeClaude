import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent } from 'react'
import { api, setActiveSlices, takeFreshSlices } from '../api/client'
import type { CharacterSheet, Reference, SheetSliceName, SheetSlices } from '../api/types'
import { SYSTEM_LABELS } from '../utils/labels'
import { SheetTab } from '../components/SheetTab'
import { TalentsTab } from '../components/TalentsTab'
import { HeroicTab } from '../components/HeroicTab'
import { InventoryTab } from '../components/InventoryTab'
import { AttachmentsTab } from '../components/AttachmentsTab'
import { TransportTab } from '../components/TransportTab'
import { CraftingTab } from '../components/CraftingTab'
import { CustomTab } from '../components/CustomTab'
import { NotesTab } from '../components/NotesTab'
import { BioTab } from '../components/BioTab'
import { HistoryTab } from '../components/HistoryTab'
import { MagicTab } from '../components/MagicTab'
import { PrintPreview } from '../components/print/PrintPreview'
import { CharacterSheetPrint } from '../components/print/CharacterSheetPrint'
import { Icon } from '../components/Icon'
import { navigate } from '../router'
import { t } from '../i18n'
import { readSheetTab, writeSheetTab, type CharacterSheetTab } from '../utils/uiPreferences'

interface Props {
  characterId: string
  /** Открыт ли печатный лист (deep link /characters/:id/print). */
  printing: boolean
  onOpenPrint: () => void
  onClosePrint: () => void
  onBack: () => void
}

/**
 * Что каждой вкладке нужно с сервера. Лист играющего персонажа весит около 116 КБ, и две трети из
 * них — инвентарь: платить за него на вкладке заметок незачем.
 *
 * <p>Таблица — единственное место, где это знание живёт. Начали читать на вкладке новую коллекцию —
 * впишите её сюда, иначе вкладка увидит пустой список вместо данных.</p>
 */
const SLICES_BY_TAB: Record<CharacterSheetTab, SheetSliceName[]> = {
  sheet: ['base'],
  talents: ['base', 'talents'],
  heroic: ['base'],
  inventory: ['base', 'items'],
  attachments: ['base', 'items', 'attachments'],
  transport: ['base', 'items', 'mounts'],
  crafting: ['base', 'items'],
  magic: ['base', 'items'],
  bio: ['base'],
  history: ['base'],
  notes: ['base'],
  custom: ['base'],
}

/**
 * Загружена ли часть. Сравнение именно с `null`, а не с `undefined`: незапрошенное приезжает с
 * сервера как `"items": null`, и проверка на `undefined` считала бы его загруженным — вкладка
 * молча рисовала бы пустой список и никогда ничего не запрашивала.
 */
const hasSlice = (slices: SheetSlices, name: SheetSliceName) => slices[name] != null

/**
 * Накладывает пришедшие части поверх уже загруженных.
 *
 * <p>Незапрошенное приезжает с сервера как `null`, а не отсутствующим полем, поэтому обычный
 * spread не годится: ответ на запрос одного инвентаря затёр бы `null`-ами и базовую часть, и всё
 * остальное. Здесь пришедшее перекрывает старое, только если оно действительно приехало.</p>
 *
 * <p>`createdId` не переносится: он относится к одному ответу, а не к состоянию листа.</p>
 */
function withSlices(prev: SheetSlices, got: SheetSlices): SheetSlices {
  return {
    base: got.base ?? prev.base,
    items: got.items ?? prev.items,
    talents: got.talents ?? prev.talents,
    talentTierCounts: got.talentTierCounts ?? prev.talentTierCounts,
    mounts: got.mounts ?? prev.mounts,
    attachments: got.attachments ?? prev.attachments,
  }
}

/**
 * Лист для печати. Печати нужно всё сразу, поэтому она берёт лист целиком одним запросом, а не
 * собирает его из частей. Отдельным компонентом — чтобы он монтировался вместе с окном печати:
 * так лист перечитывается при каждом открытии и показать устаревший нечем.
 */
function PrintSheet({ characterId, reference, onError }: {
  characterId: string; reference: Reference; onError: (message: string) => void
}) {
  const [sheet, setSheet] = useState<CharacterSheet | null>(null)
  useEffect(() => {
    let cancelled = false
    api.sheet(characterId)
      .then(full => { if (!cancelled) setSheet(full) })
      .catch((err: unknown) => {
        if (!cancelled) onError(err instanceof Error ? err.message : t('Ошибка загрузки', 'Failed to load'))
      })
    return () => { cancelled = true }
  }, [characterId, onError])

  return sheet
    ? <CharacterSheetPrint sheet={sheet} reference={reference} />
    : <p className="muted">{t('Загрузка…', 'Loading…')}</p>
}

/** Постоянная ссылка: она уходит в зависимости эффекта загрузки, новая каждый раз зациклила бы его. */
const NOTHING_LOADED: SheetSlices = {}

export function SheetPage({ characterId, printing, onOpenPrint, onClosePrint, onBack }: Props) {
  /**
   * Загруженные части вместе с тем, чьи они. Персонаж хранится рядом, а не сбрасывается эффектом:
   * иначе при переходе к другому персонажу один кадр показывал бы данные предыдущего.
   */
  const [loaded, setLoaded] = useState<{ characterId: string; slices: SheetSlices }>(
    { characterId, slices: NOTHING_LOADED })
  const slices = loaded.characterId === characterId ? loaded.slices : NOTHING_LOADED

  const mergeSlices = useCallback((got: SheetSlices) => setLoaded(prev => ({
    characterId,
    slices: withSlices(prev.characterId === characterId ? prev.slices : NOTHING_LOADED, got),
  })), [characterId])
  const [reference, setReference] = useState<Reference | null>(null)
  // Последняя вкладка хранится отдельно для каждого персонажа и переживает уход в другой раздел
  // приложения и обновление страницы. Карта также корректно работает при смене characterId без
  // размонтирования SheetPage.
  const [tabs, setTabs] = useState<Record<string, CharacterSheetTab>>(() => ({
    [characterId]: readSheetTab(characterId),
  }))
  const tab = tabs[characterId] ?? readSheetTab(characterId)
  const selectTab = (next: CharacterSheetTab) => {
    writeSheetTab(characterId, next)
    setTabs(prev => ({ ...prev, [characterId]: next }))
  }
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [shareUrl, setShareUrl] = useState<string | null>(null)
  const [xpEdit, setXpEdit] = useState<string | null>(null)
  const [actionsOpen, setActionsOpen] = useState(false)
  const portraitFileRef = useRef<HTMLInputElement>(null)
  const actionsMenuRef = useRef<HTMLDivElement>(null)

  const needed = SLICES_BY_TAB[tab]

  // Правка вернёт ровно то, что сейчас на экране, — остальное перечитается при открытии вкладки.
  useEffect(() => { setActiveSlices(needed) }, [needed])

  /**
   * Догружает недостающие части. Уже загруженные не перезапрашиваются, поэтому возврат на вкладку
   * бесплатен, а первое открытие стоит одного маленького запроса.
   */
  useEffect(() => {
    const missing = needed.filter(name => !hasSlice(slices, name))
    if (missing.length === 0) return
    let cancelled = false
    api.sheetSlices(characterId, missing)
      .then(got => { if (!cancelled) mergeSlices(got) })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка загрузки', 'Failed to load'))
      })
    return () => { cancelled = true }
  }, [characterId, needed, slices, mergeSlices])

  // Справочник берётся один раз на систему и дальше живёт в кэше `api.reference`.
  const system = slices.base?.system
  useEffect(() => {
    if (!system) return
    let cancelled = false
    api.reference(system)
      .then(next => { if (!cancelled) setReference(next) })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : t('Ошибка загрузки', 'Failed to load'))
      })
    return () => { cancelled = true }
  }, [system])

  /**
   * Обновление после правки. Если правка вернула части вместе с ответом, запроса не будет вовсе:
   * раньше на каждое действие уходило три последовательных обращения к серверу.
   *
   * Пришедшее заменяет состояние целиком, а не дополняет его: правка могла задеть и то, чего
   * сейчас нет на экране, — эти части просто выбрасываются и перечитаются при открытии вкладки.
   */
  const refresh = useCallback(async () => {
    const fresh = takeFreshSlices(characterId)
    setLoaded({ characterId, slices: fresh ?? await api.sheetSlices(characterId, needed) })
  }, [characterId, needed])

  /**
   * Лист, каким его видят вкладки. Части, которые этой вкладке не нужны, подставляются пустыми:
   * читать их здесь всё равно некому — рендер ниже ждёт, пока приедет всё нужное по таблице.
   */
  const sheet: CharacterSheet | null = useMemo(() => slices.base ? {
    ...slices.base,
    items: slices.items ?? [],
    talents: slices.talents ?? [],
    talentTierCounts: slices.talentTierCounts ?? {},
    mounts: slices.mounts ?? [],
    attachments: slices.attachments ?? [],
  } : null, [slices])

  const ready = needed.every(name => hasSlice(slices, name))

  // Ошибка действия показывается и сама скрывается
  useEffect(() => {
    if (!error) return
    const timer = setTimeout(() => setError(null), 6000)
    return () => clearTimeout(timer)
  }, [error])

  useEffect(() => {
    if (!notice) return
    const timer = setTimeout(() => setNotice(null), 6000)
    return () => clearTimeout(timer)
  }, [notice])

  useEffect(() => {
    if (!actionsOpen) return
    const closeOnOutside = (event: PointerEvent) => {
      if (!actionsMenuRef.current?.contains(event.target as Node)) setActionsOpen(false)
    }
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setActionsOpen(false)
    }
    document.addEventListener('pointerdown', closeOnOutside)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOnOutside)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [actionsOpen])

  if (!sheet || !reference) {
    return (
      <div className="page">
        <button onClick={onBack}>{t('← Назад', '← Back')}</button>
        {error ? <div className="error">{error}</div> : <p className="muted">{t('Загрузка…', 'Loading…')}</p>}
      </div>
    )
  }

  async function exportJson() {
    if (!sheet) return
    try {
      const data = await api.exportCharacter(sheet.id)
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `${sheet.name.replace(/[^\p{L}\p{N}_-]+/gu, '_') || 'character'}.genesysforge.json`
      a.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка экспорта', 'Export failed'))
    }
  }

  async function duplicateCurrent() {
    if (!sheet) return
    try {
      const copy = await api.duplicateCharacter(sheet.id)
      navigate(`/characters/${copy.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка клонирования', 'Failed to duplicate'))
    }
  }

  async function uploadPortrait(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    e.target.value = '' // повторный выбор того же файла снова вызывает onChange
    if (!file || !sheet) return
    if (file.size > 5 * 1024 * 1024) { setError(t('Файл больше 5 МБ.', 'File is larger than 5 MB.')); return }
    try {
      const { portraitUrl } = await api.uploadCharacterPortrait(sheet.id, file)
      mergeSlices({ base: { ...sheet, portraitUrl } })
      setNotice(t('Портрет обновлён.', 'Portrait updated.'))
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка загрузки портрета', 'Portrait upload failed'))
    }
  }

  async function shareCurrent() {
    if (!sheet) return
    try {
      const share = await api.shareCharacter(sheet.id)
      const url = `${window.location.origin}${share.path}`
      setShareUrl(url)
      if (navigator.clipboard?.writeText) {
        try {
          await navigator.clipboard.writeText(url)
          setNotice(t('Ссылка скопирована в буфер обмена.', 'Link copied to clipboard.'))
        } catch {
          setNotice(t('Ссылка создана. Скопируйте её из поля ниже.', 'Link created. Copy it from the field below.'))
        }
      } else {
        setNotice(t('Ссылка создана. Скопируйте её из поля ниже.', 'Link created. Copy it from the field below.'))
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка создания ссылки', 'Failed to create link'))
    }
  }

  async function revokeShares() {
    if (!sheet) return
    try {
      await api.revokeCharacterShares(sheet.id)
      setShareUrl(null)
      setNotice(t('Все публичные ссылки этого персонажа отозваны.', 'All public links for this character have been revoked.'))
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка отзыва ссылки', 'Failed to revoke links'))
    }
  }

  async function saveXp() {
    if (xpEdit === null || !sheet) return
    const value = Number(xpEdit)
    setXpEdit(null)
    if (!Number.isFinite(value) || value === sheet.totalXp) return
    try {
      await api.updateCharacter(sheet.id, { totalXp: Math.trunc(value) })
      await refresh()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
    }
  }

  return (
    <div className="page">
      <div className="page-head">
        <div>
          <button className="back-link" onClick={onBack}>
            <Icon name="arrow-left" className="button-icon" />
            {t('Персонажи', 'Characters')}
          </button>
          <div className="sheet-title-row">
            <button type="button" className="sheet-portrait" title={t('Загрузить портрет (JPEG/PNG/WebP, до 5 МБ)', 'Upload portrait (JPEG/PNG/WebP, up to 5 MB)')}
              onClick={() => portraitFileRef.current?.click()}>
              {sheet.portraitUrl
                ? <img src={sheet.portraitUrl} alt={t(`Портрет: ${sheet.name}`, `Portrait: ${sheet.name}`)} />
                : <Icon name="user" className="sheet-portrait-placeholder" />}
            </button>
            <input ref={portraitFileRef} type="file" accept="image/jpeg,image/png,image/webp" hidden
              data-testid="portrait-file" onChange={e => void uploadPortrait(e)} />
            <h2>{sheet.name}</h2>
            <span className={`badge ${sheet.system}`}>{SYSTEM_LABELS[sheet.system]}</span>
          </div>
          <div className="page-sub">{sheet.archetype.name} · {sheet.career.name}</div>
        </div>
        <div className="sheet-head-controls">
          <div className="xp-block">
            <span title={t('Суммарный опыт — кликните, чтобы изменить (награды ГМа)', 'Total XP — click to edit (GM awards)')}>
              XP: {xpEdit !== null ? (
                <input autoFocus className="xp-input" value={xpEdit}
                  onChange={e => setXpEdit(e.target.value)}
                  onBlur={() => void saveXp()}
                  onKeyDown={e => e.key === 'Enter' && void saveXp()} />
              ) : (
                <button className="linklike" onClick={() => setXpEdit(String(sheet.totalXp))}>{sheet.totalXp}</button>
              )}
            </span>
            <span className="muted"> {t('потрачено', 'spent')} {sheet.spentXp} · </span>
            <strong className="xp-available">{t('доступно', 'available')} {sheet.availableXp}</strong>
          </div>
          <div className="sheet-action-buttons">
            {sheet.isCreationPhase && (
              <button className="small" title={t('Завершить создание: зафиксировать характеристики и снять лимит рангов', 'Complete creation: lock characteristics and lift the rank limit')}
                onClick={async () => { await api.completeCreation(sheet.id); await refresh() }}>
                {t('Завершить создание', 'Complete creation')}
              </button>
            )}
            <div className="sheet-actions-menu" ref={actionsMenuRef}>
              <button type="button" className="small sheet-actions-trigger"
                aria-label={t('Дополнительные действия', 'More actions')}
                aria-haspopup="menu" aria-expanded={actionsOpen}
                onClick={() => setActionsOpen(open => !open)}>•••</button>
              {actionsOpen && (
                <div className="sheet-actions-popover" role="menu">
                  <button type="button" role="menuitem" onClick={() => { setActionsOpen(false); onOpenPrint() }}>
                    <Icon name="printer" className="button-icon" />{t('Печать', 'Print')}
                  </button>
                  <button type="button" role="menuitem" onClick={() => { setActionsOpen(false); void duplicateCurrent() }}>
                    <Icon name="copy" className="button-icon" />{t('Клонировать', 'Duplicate')}
                  </button>
                  <button type="button" role="menuitem" onClick={() => { setActionsOpen(false); void shareCurrent() }}>
                    <Icon name="share" className="button-icon" />{t('Ссылка', 'Share link')}
                  </button>
                  <button type="button" role="menuitem" onClick={() => { setActionsOpen(false); void revokeShares() }}>
                    {t('Отозвать ссылки', 'Revoke links')}
                  </button>
                  <button type="button" role="menuitem" onClick={() => { setActionsOpen(false); void exportJson() }}>
                    <Icon name="file-import" className="button-icon" />{t('Экспорт JSON', 'Export JSON')}
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {error && <div className="error floating">{error}</div>}
      {notice && <div className="notice">{notice}</div>}
      {shareUrl && (
        <div className="notice share-link">
          {t('Публичная ссылка:', 'Public link:')} <input readOnly value={shareUrl} onFocus={e => e.currentTarget.select()} />
        </div>
      )}

      <div className="tabs main-tabs">
        <button className={tab === 'sheet' ? 'tab active' : 'tab'} onClick={() => selectTab('sheet')}>{t('Лист', 'Sheet')}</button>
        <button className={tab === 'inventory' ? 'tab active' : 'tab'} onClick={() => selectTab('inventory')}>{t('Инвентарь', 'Inventory')}</button>
        <button className={tab === 'talents' ? 'tab active' : 'tab'} onClick={() => selectTab('talents')}>{t('Таланты', 'Talents')}</button>
        <button className={tab === 'magic' ? 'tab active' : 'tab'} onClick={() => selectTab('magic')}>{t('Магия', 'Magic')}</button>
        <button className={tab === 'notes' ? 'tab active' : 'tab'} onClick={() => selectTab('notes')}>{t('Заметки', 'Notes')}</button>
        {sheet.system === 'realmsOfTerrinoth' && (
          <button className={tab === 'heroic' ? 'tab active' : 'tab'} onClick={() => selectTab('heroic')}>{t('Героика', 'Heroic')}</button>
        )}
        <button className={tab === 'attachments' ? 'tab active' : 'tab'} onClick={() => selectTab('attachments')}>{t('Улучшения', 'Attachments')}</button>
        <button className={tab === 'transport' ? 'tab active' : 'tab'} onClick={() => selectTab('transport')}>{t('Транспорт', 'Transport')}</button>
        <button className={tab === 'crafting' ? 'tab active' : 'tab'} onClick={() => selectTab('crafting')}>{t('Ремесло', 'Crafting')}</button>
        <button className={tab === 'bio' ? 'tab active' : 'tab'} onClick={() => selectTab('bio')}>{t('Образ', 'Bio')}</button>
        <button className={tab === 'history' ? 'tab active' : 'tab'} onClick={() => selectTab('history')}>{t('История', 'History')}</button>
        <button className={tab === 'custom' ? 'tab active' : 'tab'} onClick={() => selectTab('custom')}>{t('Кастом', 'Custom')}</button>
      </div>

      {/* Шапка уже на экране — ждём только те части, которые нужны самой вкладке. */}
      {!ready ? <p className="muted">{t('Загрузка…', 'Loading…')}</p> : (
        <>
          {tab === 'sheet' && <SheetTab sheet={sheet} onError={setError} refresh={refresh} />}
          {tab === 'talents' && <TalentsTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
          {tab === 'heroic' && <HeroicTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
          {tab === 'inventory' && <InventoryTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
          {tab === 'attachments' && <AttachmentsTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
          {tab === 'transport' && <TransportTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
          {tab === 'crafting' && <CraftingTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
          {tab === 'magic' && <MagicTab sheet={sheet} onError={setError} refresh={refresh} />}
          {tab === 'bio' && <BioTab sheet={sheet} onError={setError} refresh={refresh} />}
          {tab === 'history' && <HistoryTab characterId={sheet.id} onError={setError} refresh={refresh} />}
          {tab === 'notes' && <NotesTab characterId={sheet.id} onError={setError} />}
          {tab === 'custom' && <CustomTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
        </>
      )}

      {printing && (
        <PrintPreview title={t(`Лист персонажа — ${sheet.name}`, `Character sheet — ${sheet.name}`)} onClose={onClosePrint}>
          {() => <PrintSheet characterId={characterId} reference={reference} onError={setError} />}
        </PrintPreview>
      )}
    </div>
  )
}
