import { useEffect, useState, type ReactNode } from 'react'
import { t } from '../../i18n'

export type PrintVersion = 'gm' | 'player'

interface Props {
  title: string
  /** Если заданы — показывается переключатель версий (GM/Player). */
  versions?: PrintVersion[]
  defaultVersion?: PrintVersion
  /** Текст для «Скопировать как Markdown» (по версии). Если не задан — кнопка скрыта. */
  markdown?: (version: PrintVersion) => string
  onClose: () => void
  children: (version: PrintVersion) => ReactNode
}

/**
 * Полноэкранный режим печати: показывает только карточки, прячет навигацию.
 * На время показа на body вешается класс `printing`, чтобы @media print печатал лишь `.print-area`.
 */
export function PrintPreview({ title, versions, defaultVersion, markdown, onClose, children }: Props) {
  const [version, setVersion] = useState<PrintVersion>(defaultVersion ?? versions?.[0] ?? 'gm')
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    document.body.classList.add('printing')
    return () => document.body.classList.remove('printing')
  }, [])

  const copy = async () => {
    if (!markdown) return
    try { await navigator.clipboard.writeText(markdown(version)); setCopied(true) }
    catch { /* буфер недоступен — молча игнорируем */ }
  }

  return (
    <div className="print-overlay">
      <div className="print-toolbar no-print">
        <button onClick={onClose}>{t('← Назад', '← Back')}</button>
        <strong>{title}</strong>
        <span className="spacer" />
        {versions && versions.length > 1 && (
          <div className="system-switch">
            {versions.map(v => (
              <button key={v} className={version === v ? 'tab active' : 'tab'} onClick={() => { setVersion(v); setCopied(false) }}>
                {v === 'gm' ? t('Версия мастера', 'GM version') : t('Версия игрока', 'Player version')}
              </button>
            ))}
          </div>
        )}
        {markdown && <button onClick={copy}>{copied ? t('Скопировано ✓', 'Copied ✓') : t('Скопировать как Markdown', 'Copy as Markdown')}</button>}
        <button className="primary" onClick={() => window.print()}>{t('🖨 Печать', '🖨 Print')}</button>
      </div>
      <div className="print-area">{children(version)}</div>
    </div>
  )
}
