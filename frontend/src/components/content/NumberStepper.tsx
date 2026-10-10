import { t } from '../../i18n'

export function NumberStepper({ label, value, onChange, glyph, className = '', disabled = false, min = 0, suffix }: {
  label: string; value: number; onChange: (value: number) => void; glyph?: string; className?: string; disabled?: boolean; min?: number; suffix?: string
}) {
  return <div className={`sheet-stepper ${className}`} role="group" aria-label={label}>
    <span>{glyph && <b>{glyph}</b>} {label}</span>
    <button type="button" disabled={disabled || value <= min} aria-label={t(`Уменьшить: ${label}`, `Decrease ${label}`)} onClick={() => onChange(Math.max(min, value - 1))}>−</button>
    <input type="number" aria-label={label} value={value} min={min} disabled={disabled} onChange={e => onChange(Math.max(min, Math.trunc(Number(e.target.value)) || 0))} />
    {suffix && <small>{suffix}</small>}
    <button type="button" disabled={disabled} aria-label={t(`Увеличить: ${label}`, `Increase ${label}`)} onClick={() => onChange(value + 1)}>+</button>
  </div>
}
