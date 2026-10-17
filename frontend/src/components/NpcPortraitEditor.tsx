import { useRef, useState } from 'react'
import { api } from '../api/client'
import { usePortraitUpload } from '../hooks/usePortraitUpload'
import { t } from '../i18n'
import { Portrait } from './Portrait'

export function NpcPortraitEditor({ npcId, name, initialUrl }: {
  npcId: string; name: string; initialUrl?: string | null
}) {
  const [url, setUrl] = useState(initialUrl)
  const [error, setError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState(false)
  const fileInput = useRef<HTMLInputElement>(null)
  const { uploadPortrait, uploading } = usePortraitUpload(async file => {
    setError(null)
    const result = await api.uploadNpcPortrait(npcId, file)
    setUrl(result.portraitUrl)
  }, setError)

  async function remove() {
    setDeleting(true); setError(null)
    try { await api.deleteNpcPortrait(npcId); setUrl(null) }
    catch (err) { setError(err instanceof Error ? err.message : t('Ошибка удаления портрета', 'Portrait removal failed')) }
    finally { setDeleting(false) }
  }

  return <div className="npc-portrait-editor">
    <Portrait url={url} name={name} className="npc-portrait" />
    <div className="npc-portrait-controls">
      <button type="button" disabled={uploading || deleting} onClick={() => fileInput.current?.click()}>
        {uploading ? t('Загрузка…', 'Uploading…') : url ? t('Заменить портрет', 'Replace portrait') : t('Загрузить портрет', 'Upload portrait')}
      </button>
      {url && <button type="button" disabled={uploading || deleting} onClick={() => void remove()}>{t('Удалить портрет', 'Remove portrait')}</button>}
      <small className="muted">{t('JPEG / PNG / WebP, до 5 МБ. Портрет сохраняется сразу.', 'JPEG / PNG / WebP, up to 5 MB. Portrait changes are saved immediately.')}</small>
      <input ref={fileInput} type="file" accept="image/jpeg,image/png,image/webp" hidden
        aria-label={t('Файл портрета NPC', 'NPC portrait file')} disabled={uploading || deleting}
        onChange={event => void uploadPortrait(event)} />
      {error && <div className="error" role="alert">{error}</div>}
    </div>
  </div>
}
