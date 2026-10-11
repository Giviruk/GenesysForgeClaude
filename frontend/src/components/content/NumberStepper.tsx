import { t } from '../../i18n'

export function NumberStepper({ label, value, onChange, glyph, className = '', disabled = false, min = 0, max, suffix }: {
  label: string; value: number; onChange: (value: number) => void; glyph?: string; className?: string; disabled?: boolean; min?: number; max?: number; suffix?: string
}) {
  return <div className={`sheet-stepper ${className}`} role="group" aria-label={label}>
    <span>{glyph && <b>{glyph}</b>} {label}</span>
    <button type="button" disabled={disabled || value <= min} aria-label={t(`Уменьшить: ${label}`, `Decrease ${label}`)} onClick={() => onChange(Math.max(min, value - 1))}>−</button>
    <input type="number" aria-label={label} value={value} min={min} max={max} disabled={disabled} onChange={e => onChange(Math.min(max ?? Infinity, Math.max(min, Math.trunc(Number(e.target.value)) || 0)))} />
    {suffix && <small>{suffix}</small>}
    <button type="button" disabled={disabled || (max !== undefined && value >= max)} aria-label={t(`Увеличить: ${label}`, `Increase ${label}`)} onClick={() => onChange(Math.min(max ?? Infinity, value + 1))}>+</button>
  </div>
}
