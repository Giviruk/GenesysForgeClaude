import { useState, type ReactNode } from 'react'

/** A new URL remounts the image so a failed old portrait never hides its replacement. */
export function Portrait({ url, name, className, fallback = null, children }: {
  url?: string | null; name: string; className?: string; fallback?: ReactNode; children?: ReactNode
}) {
  return url ? <PortraitImage key={url} url={url} name={name} className={className} fallback={fallback}>{children}</PortraitImage> : fallback
}

function PortraitImage({ url, name, className, fallback, children }: {
  url: string; name: string; className?: string; fallback: ReactNode; children?: ReactNode
}) {
  const [failed, setFailed] = useState(false)
  return failed ? fallback : <><img src={url} alt={name} className={className} draggable={false} onError={() => setFailed(true)} />{children}</>
}
