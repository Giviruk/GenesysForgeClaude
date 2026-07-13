import { useCallback, useEffect, useRef, useState, type ChangeEvent } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, Reference } from '../api/types'
import { SYSTEM_LABELS } from '../utils/labels'
import { SheetTab } from '../components/SheetTab'
import { TalentsTab } from '../components/TalentsTab'
import { InventoryTab } from '../components/InventoryTab'
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

interface Props {
  characterId: string
  /** Открыт ли печатный лист (deep link /characters/:id/print). */
  printing: boolean
  onOpenPrint: () => void
  onClosePrint: () => void
  onBack: () => void
}

type Tab = 'sheet' | 'talents' | 'inventory' | 'magic' | 'bio' | 'history' | 'notes' | 'custom'

export function SheetPage({ characterId, printing, onOpenPrint, onClosePrint, onBack }: Props) {
  const [sheet, setSheet] = useState<CharacterSheet | null>(null)
  const [reference, setReference] = useState<Reference | null>(null)
  const [tab, setTab] = useState<Tab>('sheet')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [shareUrl, setShareUrl] = useState<string | null>(null)
  const [xpEdit, setXpEdit] = useState<string | null>(null)
  const portraitFileRef = useRef<HTMLInputElement>(null)

  const refresh = useCallback(
    () => api.sheet(characterId).then(next =>
      api.reference(next.system).then(ref => {
        setSheet(next)
        setReference(ref)
      })),
    [characterId])

  useEffect(() => {
    refresh().catch((err: unknown) => setError(err instanceof Error ? err.message : t('Ошибка загрузки', 'Failed to load')))
  }, [refresh])

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
      setSheet({ ...sheet, portraitUrl })
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
            <button className="small" title={t('Печать листа персонажа / сохранение в PDF', 'Print the character sheet / save as PDF')}
              onClick={onOpenPrint}>
              <Icon name="printer" className="button-icon" />
              {t('Печать', 'Print')}
            </button>
            <button className="small" title={t('Создать копию персонажа', 'Create a copy of the character')}
              onClick={() => void duplicateCurrent()}>
              <Icon name="copy" className="button-icon" />
              {t('Клонировать', 'Duplicate')}
            </button>
            <button className="small" title={t('Создать публичную read-only ссылку', 'Create a public read-only link')}
              onClick={() => void shareCurrent()}>
              <Icon name="share" className="button-icon" />
              {t('Ссылка', 'Share link')}
            </button>
            <button className="small" title={t('Отозвать все публичные ссылки этого персонажа', 'Revoke all public links for this character')}
              onClick={() => void revokeShares()}>
              {t('Отозвать ссылки', 'Revoke links')}
            </button>
            <button className="small" title={t('Скачать персонажа в JSON (бэкап / перенос между аккаунтами)', 'Download the character as JSON (backup / transfer between accounts)')}
              onClick={() => void exportJson()}>
              <Icon name="file-import" className="button-icon" />
              {t('Экспорт JSON', 'Export JSON')}
            </button>
            {sheet.isCreationPhase && (
              <button className="small" title={t('Завершить создание: зафиксировать характеристики и снять лимит рангов', 'Complete creation: lock characteristics and lift the rank limit')}
                onClick={async () => { await api.completeCreation(sheet.id); await refresh() }}>
                {t('Завершить создание', 'Complete creation')}
              </button>
            )}
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
        <button className={tab === 'sheet' ? 'tab active' : 'tab'} onClick={() => setTab('sheet')}>{t('Лист', 'Sheet')}</button>
        <button className={tab === 'talents' ? 'tab active' : 'tab'} onClick={() => setTab('talents')}>{t('Таланты', 'Talents')}</button>
        <button className={tab === 'inventory' ? 'tab active' : 'tab'} onClick={() => setTab('inventory')}>{t('Инвентарь', 'Inventory')}</button>
        <button className={tab === 'magic' ? 'tab active' : 'tab'} onClick={() => setTab('magic')}>{t('Магия', 'Magic')}</button>
        <button className={tab === 'bio' ? 'tab active' : 'tab'} onClick={() => setTab('bio')}>{t('Образ', 'Bio')}</button>
        <button className={tab === 'history' ? 'tab active' : 'tab'} onClick={() => setTab('history')}>{t('История', 'History')}</button>
        <button className={tab === 'notes' ? 'tab active' : 'tab'} onClick={() => setTab('notes')}>{t('Заметки', 'Notes')}</button>
        <button className={tab === 'custom' ? 'tab active' : 'tab'} onClick={() => setTab('custom')}>{t('Кастом', 'Custom')}</button>
      </div>

      {tab === 'sheet' && <SheetTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
      {tab === 'talents' && <TalentsTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
      {tab === 'inventory' && <InventoryTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}
      {tab === 'magic' && <MagicTab sheet={sheet} onError={setError} />}
      {tab === 'bio' && <BioTab sheet={sheet} onError={setError} refresh={refresh} />}
      {tab === 'history' && <HistoryTab characterId={sheet.id} onError={setError} refresh={refresh} />}
      {tab === 'notes' && <NotesTab characterId={sheet.id} onError={setError} />}
      {tab === 'custom' && <CustomTab sheet={sheet} reference={reference} onError={setError} refresh={refresh} />}

      {printing && (
        <PrintPreview title={t(`Лист персонажа — ${sheet.name}`, `Character sheet — ${sheet.name}`)} onClose={onClosePrint}>
          {() => <CharacterSheetPrint sheet={sheet} reference={reference} />}
        </PrintPreview>
      )}
    </div>
  )
}
