import { useMemo, useRef, useState, type ReactNode } from 'react'
import type { RollOutcome } from './utils/diceRoller'
import {
  combatTotal, criticalAdvantageSpend, expandDamage, isHit, qualityAdvantageSpends,
} from './utils/combat'
import { DiceRoller, RollSymbolsView } from './components/DiceRoller'
import { t } from './i18n'
import {
  DiceRollerContext, type DiceRollerContextValue, type DiceRollerRequest,
} from './dice-roller-store'

type DrawerState = DiceRollerRequest & { id: number }

const DEFAULT_REQUEST: DiceRollerRequest = {
  kind: 'roll',
  title: t('Дайсроллер', 'Dice roller'),
}

export function DiceRollerProvider({ children }: { children: ReactNode }) {
  const [drawer, setDrawer] = useState<DrawerState | null>(null)
  const nextId = useRef(1)

  const value = useMemo<DiceRollerContextValue>(() => ({
    openRoller: request => {
      setDrawer({ ...(request ?? DEFAULT_REQUEST), id: nextId.current++ })
    },
    closeRoller: () => setDrawer(null),
  }), [])

  return (
    <DiceRollerContext.Provider value={value}>
      {children}
      <DiceRollerDrawer drawer={drawer} onClose={value.closeRoller} />
    </DiceRollerContext.Provider>
  )
}

function DiceRollerDrawer({ drawer, onClose }: { drawer: DrawerState | null; onClose: () => void }) {
  if (!drawer) return null

  return (
    <aside className="dice-drawer no-print" aria-label={t('Дайсроллер', 'Dice roller')}>
      <div className="dice-drawer-head">
        <div>
          <h3>{drawer.kind === 'combat'
            ? t(`Атака — ${drawer.title}`, `Attack — ${drawer.title}`)
            : drawer.title ?? t('Дайсроллер', 'Dice roller')}</h3>
          {drawer.kind === 'roll' && drawer.label && <p className="muted small-text">{t('Бросок:', 'Roll:')} {drawer.label}</p>}
        </div>
        <button type="button" className="small" onClick={onClose}>{t('Закрыть', 'Close')}</button>
      </div>

      {drawer.kind === 'combat' ? (
        <CombatRollerContent
          key={drawer.id}
          request={drawer}
        />
      ) : drawer.kind === 'magic' ? (
        <MagicRollerContent key={drawer.id} request={drawer} />
      ) : (
        <>
          <p className="hint">{t('Соберите пул, добавьте сложность/бонусы/помехи и бросьте. Основной экран остаётся доступным.', 'Build the pool, add difficulty/boosts/setbacks and roll. The main screen stays available.')}</p>
          <DiceRoller
            key={drawer.id}
            initialPool={drawer.initialPool}
            label={drawer.label}
            onLog={drawer.onLog}
            canSecret={drawer.canSecret}
            spendContext={drawer.spendContext}
          />
        </>
      )}
    </aside>
  )
}

function MagicRollerContent({ request }: {
  request: Extract<DrawerState, { kind: 'magic' }>
}) {
  const [outcome, setOutcome] = useState<RollOutcome | null>(null)
  const netSuccess = outcome?.net.success ?? 0
  const total = request.damage && isHit(netSuccess)
    ? request.damage.base + netSuccess * request.damage.successMultiplier
    : null
  const conditionalTotal = request.damage?.conditionalSuccessMultiplier && isHit(netSuccess)
    ? request.damage.base + netSuccess * request.damage.conditionalSuccessMultiplier
    : null

  return (
    <>
      <div className="combat-roller-head">
        <span className="muted">{t('Навык:', 'Skill:')} {request.skillLabel}</span>
        {request.damage && (
          <div className="npc-weapon-stats">
            <span className="weapon-stat">{t('Базовый урон', 'Base damage')} <strong>{request.damage.base}</strong></span>
            <span className="muted small-text">
              {request.damage.characteristicMultiplier > 1
                ? `${request.damage.characteristic} × ${request.damage.characteristicMultiplier}`
                : request.damage.characteristic}
              {request.damage.implementBonus > 0
                ? t(` + ${request.damage.implementBonus} от инструмента/руны`,
                    ` + ${request.damage.implementBonus} from implement/rune`)
                : ''}
            </span>
          </div>
        )}
      </div>
      <p className="hint">{t(
        'Пул собран по листу и выбранным эффектам. Итоговый урон остаётся подсказкой до поглощения цели.',
        'The pool is built from the character sheet and selected effects. Final damage remains a guide before target soak.',
      )}</p>
      <DiceRoller
        initialPool={request.basePool}
        label={request.label}
        onResult={setOutcome}
        onLog={request.onLog}
        canSecret={request.canSecret}
        spendContext="magic"
        advantageSpends={request.advantageSpends}
      />

      {outcome && request.damage && (
        <div className="combat-damage">
          {!isHit(netSuccess)
            ? <div className="combat-damage-calc">
                {t('Промах: обычный урон не применяется.', 'Miss: ordinary damage does not apply.')}
              </div>
            : (
              <>
                <div className="combat-damage-calc">
                  {t('Урон:', 'Damage:')} <strong>{request.damage.base}</strong>
                  {' + '}
                  <strong>{netSuccess}</strong>
                  {request.damage.successMultiplier > 1 && ` × ${request.damage.successMultiplier}`}
                  {' = '}<strong>{total}</strong>
                  <span className="muted small-text"> {t('(до поглощения цели)', '(before the target’s soak)')}</span>
                </div>
                {conditionalTotal != null && (
                  <div className="muted small-text">
                    {t(request.damage.conditionalLabelRu || '', request.damage.conditionalLabelEn || '')}:{' '}
                    <strong>{conditionalTotal}</strong>
                    {' '}({request.damage.base} + {netSuccess} × {request.damage.conditionalSuccessMultiplier})
                  </div>
                )}
              </>
            )}
        </div>
      )}
    </>
  )
}

function CombatRollerContent({ request }: {
  request: Extract<DrawerState, { kind: 'combat' }>
}) {
  const [outcome, onOutcome] = useState<RollOutcome | null>(null)
  const dmg = expandDamage(request.damage, request.brawn)
  const netSuccess = outcome?.net.success ?? 0
  const total = combatTotal(dmg.base, netSuccess)

  return (
    <>
      <div className="combat-roller-head">
        {request.skillLabel && <span className="muted">{t('Навык:', 'Skill:')} {request.skillLabel}</span>}
        <div className="npc-weapon-stats">
          <span className="weapon-stat">{t('Урон', 'Damage')} <strong>{dmg.text}</strong></span>
          {request.crit && <span className="weapon-stat">{t('Крит', 'Crit')} <strong>{request.crit}</strong></span>}
          {request.rangeBand && <span className="weapon-stat">{request.rangeBand}</span>}
        </div>
        {request.qualities.length > 0 && (
          <ul className="combat-qualities">
            {request.qualities.map((q, i) => (
              <li key={i}>
                <strong>{q.label}</strong>
                {q.activationCost && <span className="muted small-text"> — {q.activationCost}</span>}
              </li>
            ))}
          </ul>
        )}
      </div>

      <p className="hint">{t('Базовый пул собран по навыку. Добавьте сложность/бонусы/помехи и бросьте — урон не решает за вас.', 'The base pool is built from the skill. Add difficulty/boosts/setbacks and roll — damage is not applied automatically.')}</p>
      <DiceRoller
        initialPool={request.basePool}
        label={request.title}
        onResult={onOutcome}
        onLog={request.onLog}
        canSecret={request.canSecret}
        spendContext="combat"
        advantageSpends={[
          ...criticalAdvantageSpend(request.crit),
          ...qualityAdvantageSpends(request.qualities),
        ]}
      />

      {outcome && (
        <div className="combat-damage">
          <RollSymbolsView symbols={outcome.net} />
          {!isHit(netSuccess)
            ? <div className="combat-damage-calc">
                {t('Промах: обычный урон не применяется.', 'Miss: ordinary damage does not apply.')}
              </div>
            : total != null
              ? <div className="combat-damage-calc">
                  {t('Урон:', 'Damage:')} <strong>{dmg.base}</strong> {t('+ успехов', '+ successes')} <strong>{netSuccess}</strong> = <strong>{total}</strong>
                  <span className="muted small-text"> {t('(до поглощения цели)', '(before the target’s soak)')}</span>
                </div>
              : <div className="muted small-text">{t('Урон оружия задан текстом — посчитайте вручную.', 'Weapon damage is text-only — calculate it manually.')}</div>}
        </div>
      )}
    </>
  )
}
