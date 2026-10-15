import { Children, Fragment, isValidElement, useEffect, useId, useLayoutEffect, useMemo, useRef, useState, type ButtonHTMLAttributes, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { Icon } from './Icon'
import './Select.css'

type Props = Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'value' | 'onChange' | 'children' | 'type'> & {
  value: string | number; onChange: (value: string) => void; required?: boolean; name?: string
  children?: ReactNode
}
type Option = { value: string; label: string; disabled: boolean; group?: string; title?: string }

function text(node: ReactNode): string {
  return Children.toArray(node).map(child => isValidElement<{ children?: ReactNode }>(child) ? text(child.props.children) : String(child)).join('')
}
function readOptions(children: ReactNode, group?: string, disabled = false): Option[] {
  return Children.toArray(children).flatMap(child => {
    if (!isValidElement<{ value?: string | number; children?: ReactNode; label?: string; disabled?: boolean; title?: string }>(child)) return []
    if (child.type === Fragment) return readOptions(child.props.children, group, disabled)
    if (child.type === 'optgroup') return readOptions(child.props.children, child.props.label, disabled || !!child.props.disabled)
    if (child.type !== 'option') return []
    const label = child.props.label ?? text(child.props.children)
    return [{ value: String(child.props.value ?? label), label, group, title: child.props.title, disabled: disabled || !!child.props.disabled }]
  })
}

/** Select-only combobox. The offscreen native control participates only in form submission/validation. */
export function Select({ value, onChange, children, required, name, disabled, id: suppliedId, className = '', title, onKeyDown, ...props }: Props) {
  const generatedId = useId()
  const id = suppliedId ?? `select-${generatedId}`
  const listId = `${id}-list`
  const field = useRef<HTMLButtonElement>(null)
  const list = useRef<HTMLDivElement>(null)
  const native = useRef<HTMLSelectElement>(null)
  const prefix = useRef({ value: '', time: 0 })
  const [open, setOpen] = useState(false)
  const [portalHost, setPortalHost] = useState<HTMLElement | null>(null)
  const [active, setActive] = useState(-1)
  const [invalid, setInvalid] = useState(false)
  const [position, setPosition] = useState({ left: 16, top: 0, width: 180, maxHeight: 320 })
  const rows = useMemo(() => readOptions(children), [children])
  const chosen = rows.findIndex(o => o.value === String(value))
  const label = rows[chosen]?.label ?? ''
  const enabled = rows.map((o, i) => o.disabled ? -1 : i).filter(i => i >= 0)

  function show(index = chosen) {
    if (disabled) return
    // A body portal is inert outside a showModal() dialog. Keep its popup in the
    // dialog's accessible subtree and promote it to the top layer to escape clipping.
    setPortalHost(field.current?.closest('dialog') ?? document.body)
    setActive(enabled.includes(index) ? index : enabled[0] ?? -1)
    setOpen(true)
  }
  function choose(index: number) {
    const option = rows[index]
    if (!option || option.disabled || disabled) return
    onChange(option.value)
    setInvalid(false)
    setOpen(false)
    field.current?.focus()
  }
  useLayoutEffect(() => {
    if (!open || disabled) return
    const popup = list.current
    if (popup?.hasAttribute('popover') && popup.showPopover && !popup.matches(':popover-open')) popup.showPopover()
    const place = () => {
      const rect = field.current?.getBoundingClientRect()
      if (!rect) return
      const width = Math.min(Math.max(180, rect.width), 360, innerWidth - 32)
      // scrollHeight не учитывает рамку списка — без неё появляется лишняя прокрутка на 2px.
      const fullHeight = popup ? popup.scrollHeight + popup.offsetHeight - popup.clientHeight : 320
      const desiredHeight = Math.min(320, innerHeight * .6, fullHeight)
      const below = innerHeight - rect.bottom - 12
      const above = rect.top - 12
      const upwards = below < desiredHeight && above > below
      const maxHeight = Math.max(32, Math.min(desiredHeight, upwards ? above : below))
      const next = { width, maxHeight, left: Math.max(16, Math.min(rect.left, innerWidth - width - 16)),
        top: upwards ? Math.max(8, rect.top - maxHeight - 4) : rect.bottom + 4 }
      setPosition(previous => previous.width === next.width && previous.maxHeight === next.maxHeight
        && previous.left === next.left && previous.top === next.top ? previous : next)
    }
    place()
    window.addEventListener('resize', place)
    window.addEventListener('scroll', place, true)
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(place)
    if (field.current) observer?.observe(field.current)
    return () => { window.removeEventListener('resize', place); window.removeEventListener('scroll', place, true); observer?.disconnect() }
  }, [open, disabled, rows.length])
  useLayoutEffect(() => {
    if (open) document.getElementById(`${id}-option-${active}`)?.scrollIntoView?.({ block: 'nearest' })
  }, [active, open, id, position.top])
  useEffect(() => {
    if (!open) return
    const outside = (event: PointerEvent) => {
      const target = event.target as Node
      if (!field.current?.contains(target) && !list.current?.contains(target)) setOpen(false)
    }
    const invalidForm = () => { if (firstInvalid() !== native.current) setOpen(false) }
    document.addEventListener('pointerdown', outside)
    document.addEventListener('invalid', invalidForm, true)
    return () => { document.removeEventListener('pointerdown', outside); document.removeEventListener('invalid', invalidForm, true) }
  }, [open])

  function firstInvalid() {
    const control = native.current
    return control?.form ? Array.from(control.form.elements).find(element =>
      'willValidate' in element && element.willValidate && 'validity' in element
      && !(element as HTMLInputElement).validity.valid) : control
  }

  function keyDown(event: React.KeyboardEvent<HTMLButtonElement>) {
    onKeyDown?.(event)
    if (event.defaultPrevented || disabled) return
    const key = event.key
    if (key === 'Tab') { setOpen(false); return }
    if (key === 'Escape') {
      if (open) { event.preventDefault(); event.stopPropagation() }
      setOpen(false)
      return
    }
    if (key === 'Enter' || key === ' ') {
      event.preventDefault()
      if (open) choose(active); else show()
      return
    }
    if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'PageDown', 'PageUp'].includes(key)) {
      event.preventDefault()
      if (!open) { show(key === 'Home' ? enabled[0] : key === 'End' ? enabled.at(-1) : chosen); return }
      const current = Math.max(0, enabled.indexOf(active))
      const next = key === 'Home' ? 0 : key === 'End' ? enabled.length - 1
        : Math.max(0, Math.min(enabled.length - 1, current + (key === 'ArrowDown' ? 1 : key === 'ArrowUp' ? -1 : key === 'PageDown' ? 10 : -10)))
      setActive(enabled[next] ?? -1)
      return
    }
    if (key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
      event.preventDefault()
      const now = Date.now()
      const query = (now - prefix.current.time < 500 ? prefix.current.value : '') + key.toLocaleLowerCase()
      prefix.current = { value: query, time: now }
      const start = open ? active : chosen
      const ordered = [...enabled.filter(i => i > start), ...enabled.filter(i => i <= start)]
      const match = ordered.find(i => rows[i].label.toLocaleLowerCase().startsWith(query))
        ?? ordered.find(i => rows[i].label.toLocaleLowerCase().startsWith(key.toLocaleLowerCase()))
      if (match !== undefined) { if (open) setActive(match); else choose(match) }
    }
  }

  return <>
    <button {...props} ref={field} id={id} type="button" value={value} className={`select-control ${className}`}
      disabled={disabled} title={title ? `${label} · ${title}` : label} role="combobox"
      aria-haspopup="listbox" aria-expanded={open && !disabled} aria-controls={listId}
      aria-invalid={props['aria-invalid'] ?? (invalid && required && (chosen < 0 || !rows[chosen].value) || undefined)}
      aria-required={required || undefined} aria-activedescendant={open && !disabled && active >= 0 ? `${id}-option-${active}` : undefined}
      onClick={() => open ? setOpen(false) : show()} onKeyDown={keyDown}>
      <span className="select-value">{label || '\u00a0'}</span><Icon name="chevron-down" className="select-chevron" />
    </button>
    <select ref={native} className="select-native" tabIndex={-1} aria-hidden="true" name={name} value={chosen < 0 ? '' : value}
      disabled={disabled} required={required} onChange={event => { setInvalid(false); onChange(event.target.value) }}
      onInvalid={event => {
        event.preventDefault()
        setInvalid(true)
        // Native validation visits every invalid control. Only the first one owns focus.
        if (firstInvalid() === event.currentTarget) { field.current?.focus(); show() }
        else setOpen(false)
      }}>
      {chosen < 0 && <option value="" disabled hidden />}
      {rows.map((o, i) => <option key={i} value={o.value} title={o.title} disabled={o.disabled}>{o.label}</option>)}
    </select>
    {open && !disabled && portalHost && createPortal(<div ref={list} id={listId} role="listbox" aria-labelledby={id}
      popover={portalHost.tagName === 'DIALOG' ? 'manual' : undefined}
      className="select-popup" style={{ position: 'fixed', ...position }}>
      {rows.map((o, i) => <Fragment key={`${o.value}-${i}`}>
        {o.group && (i === 0 || rows[i - 1].group !== o.group) && <div className="select-group" role="presentation">{o.group}</div>}
        <div id={`${id}-option-${i}`} role="option" aria-selected={i === chosen} aria-disabled={o.disabled || undefined}
          data-value={o.value} title={o.title} className={`select-option${i === active ? ' active' : ''}`}
          onMouseDown={event => event.preventDefault()} onMouseMove={() => { if (!o.disabled) setActive(i) }} onClick={() => choose(i)}>{o.label}</div>
      </Fragment>)}
    </div>, portalHost)}
  </>
}
