import { useEffect, useId, useLayoutEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import type { TalentDef } from '../../api/types'
import { findNpcTalent } from '../../utils/bestiary'
import { localizedDescription, localizedName, secondaryName } from '../../utils/labels'
import { t } from '../../i18n'
import { Icon } from '../Icon'

export function TalentTags({ values, talents }: { values: string[]; talents: TalentDef[] }) {
  const [active, setActive] = useState<number | null>(null)
  const [pinned, setPinned] = useState(false)
  const [mobile, setMobile] = useState(() => window.matchMedia?.('(max-width: 759px)').matches ?? false)
  const root = useRef<HTMLDivElement>(null)
  const tip = useRef<HTMLDivElement>(null)
  const triggers = useRef<(HTMLButtonElement | null)[]>([])
  const id = useId()
  useEffect(() => {
    const mq = window.matchMedia?.('(max-width: 759px)')
    const update = () => { setMobile(mq?.matches ?? false); setActive(null); setPinned(false) }
    mq?.addEventListener('change', update)
    return () => mq?.removeEventListener('change', update)
  }, [])
  useEffect(() => {
    if (active === null) return
    const close = (e: PointerEvent) => {
      if (!root.current?.contains(e.target as Node) && !tip.current?.contains(e.target as Node)) { setActive(null); setPinned(false) }
    }
    const key = (e: KeyboardEvent) => { if (e.key === 'Escape') { setActive(null); setPinned(false) } }
    document.addEventListener('pointerdown', close)
    document.addEventListener('keydown', key)
    return () => { document.removeEventListener('pointerdown', close); document.removeEventListener('keydown', key) }
  }, [active])
  useLayoutEffect(() => {
    if (active === null || mobile) return
    const position = () => {
      const anchor = triggers.current[active]?.getBoundingClientRect()
      const element = tip.current
      if (!anchor || !element) return
      element.style.left = `${Math.max(12, Math.min(anchor.left, innerWidth - element.offsetWidth - 12))}px`
      const below = anchor.bottom + 6
      element.style.top = `${below + element.offsetHeight < innerHeight - 12 ? below : Math.max(12, anchor.top - element.offsetHeight - 6)}px`
    }
    position()
    window.addEventListener('resize', position)
    window.addEventListener('scroll', position, true)
    return () => { window.removeEventListener('resize', position); window.removeEventListener('scroll', position, true) }
  }, [active, mobile])
  const talent = active === null ? undefined : findNpcTalent(values[active], talents)
  const content = active !== null && <div ref={tip} id={id} role="tooltip" className={`bt-talent-tip${mobile ? ' inline' : ''}`}>
    <div className="bt-tip-title"><strong>{talent ? localizedName(talent) : values[active]}</strong>
      {talent && <span className="muted">{secondaryName(talent)}</span>}
      {mobile && <button type="button" aria-label={t('Закрыть описание таланта', 'Close talent description')} onClick={() => { setActive(null); setPinned(false) }}><Icon name="close" /></button>}
    </div>
    {talent && <small className="muted">{t('Ступень', 'Tier')} {talent.tier} · {(talent.activationEn || talent.activation).toLowerCase().includes('passive') || talent.activation.toLowerCase().includes('пассив') ? t('пассивный', 'passive') : t('активный', 'active')}{talent.isRanked && t(' · ранговый', ' · ranked')}</small>}
    <p>{talent ? localizedDescription(talent) || talent.safeDescription || t('Описание не найдено в справочнике.', 'No description found in the reference.') : t('Описание не найдено в справочнике.', 'No description found in the reference.')}</p>
  </div>
  return <div ref={root} className="bt-talents">
    <div className="bt-pills">{values.map((value, i) => <button key={`${value}-${i}`} type="button" ref={el => { triggers.current[i] = el }}
      className={`bt-talent${active === i && pinned ? ' pinned' : ''}`} aria-describedby={active === i ? id : undefined} aria-expanded={active === i}
      onMouseEnter={() => { if (!mobile && !pinned) setActive(i) }} onMouseLeave={() => { if (!mobile && !pinned) setActive(null) }}
      onFocus={() => { if (!mobile && !pinned) setActive(i) }} onBlur={() => { if (!mobile && !pinned) setActive(null) }}
      onClick={() => { const close = active === i && pinned; setActive(close ? null : i); setPinned(!close) }}>{value}</button>)}</div>
    {mobile ? content : content && createPortal(content, document.body)}
    <small className="muted">{mobile ? t('Нажмите на талант, чтобы увидеть описание', 'Tap a talent to read its description') : t('Наведите — подсказка, клик — закрепить', 'Hover to read, click to pin')}</small>
  </div>
}
