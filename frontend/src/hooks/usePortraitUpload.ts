import { useRef, useState, type ChangeEvent } from 'react'
import { t } from '../i18n'

/** Shared raw-file upload flow for character and NPC portraits. */
export function usePortraitUpload(upload: (file: File) => Promise<unknown>, onError: (message: string) => void) {
  const [uploading, setUploading] = useState(false)
  const pending = useRef(false)

  async function uploadPortrait(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file || pending.current) return
    if (file.size > 5 * 1024 * 1024) { onError(t('Файл больше 5 МБ.', 'File is larger than 5 MB.')); return }
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
      onError(t('Поддерживаются только изображения JPEG, PNG и WebP.', 'Only JPEG, PNG and WebP images are supported.'))
      return
    }
    pending.current = true
    setUploading(true)
    try { await upload(file) }
    catch (err) { onError(err instanceof Error ? err.message : t('Ошибка загрузки портрета', 'Portrait upload failed')) }
    finally { pending.current = false; setUploading(false) }
  }

  return { uploadPortrait, uploading }
}
