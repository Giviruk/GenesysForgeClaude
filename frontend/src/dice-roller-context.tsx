import { useMemo, useRef, useState, type ReactNode } from 'react'
import type { RollOutcome } from './utils/diceRoller'
import { combatTotal, expandDamage } from './utils/combat'
import { DiceRoller, RollSymbolsView } from './components/DiceRoller'
import {
  DiceRollerContext, type DiceRollerContextValue, type DiceRollerRequest,
} from './dice-roller-store'

type DrawerState = DiceRollerRequest & { id: number }

const DEFAULT_REQUEST: DiceRollerRequest = {
  kind: 'roll',
  title: 'Дайсроллер',
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
    <aside className="dice-drawer no-print" aria-label="Дайсроллер">
      <div className="dice-drawer-head">
        <div>
          <h3>{drawer.kind === 'combat' ? `Атака — ${drawer.title}` : drawer.title ?? 'Дайсроллер'}</h3>
          {drawer.kind === 'roll' && drawer.label && <p className="muted small-text">Бросок: {drawer.label}</p>}
        </div>
        <button type="button" className="small" onClick={onClose}>Закрыть</button>
      </div>

      {drawer.kind === 'combat' ? (
        <CombatRollerContent
          key={drawer.id}
          request={drawer}
        />
      ) : (
        <>
          <p className="hint">Соберите пул, добавьте сложность/бонусы/помехи и бросьте. Основной экран остаётся доступным.</p>
          <DiceRoller
            key={drawer.id}
            initialPool={drawer.initialPool}
            label={drawer.label}
            onLog={drawer.onLog}
            canSecret={drawer.canSecret}
          />
        </>
      )}
    </aside>
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
        {request.skillLabel && <span className="muted">Навык: {request.skillLabel}</span>}
        <div className="npc-weapon-stats">
          <span className="weapon-stat">Урон <strong>{dmg.text}</strong></span>
          {request.crit && <span className="weapon-stat">Крит <strong>{request.crit}</strong></span>}
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

      <p className="hint">Базовый пул собран по навыку. Добавьте сложность/бонусы/помехи и бросьте — урон не решает за вас.</p>
      <DiceRoller
        initialPool={request.basePool}
        label={request.title}
        onResult={onOutcome}
        onLog={request.onLog}
        canSecret={request.canSecret}
      />

      {outcome && (
        <div className="combat-damage">
          <RollSymbolsView symbols={outcome.net} />
          {total != null
            ? <div className="combat-damage-calc">
                Урон: <strong>{dmg.base}</strong> + успехов <strong>{netSuccess}</strong> = <strong>{total}</strong>
                {netSuccess === 0 && outcome.net.failure > 0 && <span className="muted small-text"> (промах — нет успехов)</span>}
              </div>
            : <div className="muted small-text">Урон оружия задан текстом — посчитайте вручную.</div>}
        </div>
      )}
    </>
  )
}
