import { useEffect, useState, type ReactNode } from 'react'

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
        <button onClick={onClose}>← Назад</button>
        <strong>{title}</strong>
        <span className="spacer" />
        {versions && versions.length > 1 && (
          <div className="system-switch">
            {versions.map(v => (
              <button key={v} className={version === v ? 'tab active' : 'tab'} onClick={() => { setVersion(v); setCopied(false) }}>
                {v === 'gm' ? 'Версия мастера' : 'Версия игрока'}
              </button>
            ))}
          </div>
        )}
        {markdown && <button onClick={copy}>{copied ? 'Скопировано ✓' : 'Скопировать как Markdown'}</button>}
        <button className="primary" onClick={() => window.print()}>🖨 Печать</button>
      </div>
      <div className="print-area">{children(version)}</div>
    </div>
  )
}
