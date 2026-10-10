import { t } from '../../i18n'
import { vitalState } from '../../utils/contentLibrary'
import { Icon } from '../Icon'

export function VitalCard({ kind, current, threshold, onChange, disabled = false, label: customLabel }: {
  label?: string
  kind: 'wounds' | 'strain'; current: number; threshold: number; onChange?: (value: number) => void; disabled?: boolean
}) {
  const label = customLabel ?? (kind === 'wounds' ? t('Раны', 'Wounds') : t('Усталость', 'Strain'))
  const state = vitalState(current, threshold)
  const note = state === 'over' ? t('Порог превышен', 'Threshold exceeded') : state === 'threshold' ? t('На пороге', 'At threshold') : state === 'near' ? t('Близко к порогу', 'Near threshold') : t('В норме', 'Normal')
  return <section className={`rd-vital ${kind} ${state}`} aria-label={label}>
    <header><Icon name={kind === 'wounds' ? 'heart' : 'bolt'} /><span>{label}</span>
      {onChange && <button type="button" aria-label={t(`Уменьшить: ${label}`, `Decrease ${label}`)} disabled={disabled || current <= 0} onClick={() => onChange(Math.max(0, current - 1))}>−</button>}
      <div><b>{current}</b><span> / {threshold}</span></div>
      {onChange && <button type="button" aria-label={t(`Увеличить: ${label}`, `Increase ${label}`)} disabled={disabled} onClick={() => onChange(current + 1)}>+</button>}
    </header>
    <div className="rd-vital-segments" role="meter" aria-label={label} aria-valuemin={0} aria-valuemax={Math.max(1, threshold)} aria-valuenow={Math.min(current, Math.max(1, threshold))} aria-valuetext={`${current} / ${threshold}: ${note}`}>
      {Array.from({ length: Math.max(0, threshold) }, (_, i) => <i key={i} className={i < current ? 'filled' : ''} />)}
    </div><small>{note}</small>
  </section>
}
