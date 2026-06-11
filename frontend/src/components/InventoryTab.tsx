import { useState } from 'react'
import { api } from '../api/client'
import type { CharacterSheet, ItemState, Reference } from '../api/types'
import { ITEM_KIND_LABELS, ITEM_STATE_LABELS } from '../utils/labels'

interface Props {
  sheet: CharacterSheet
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

const STATES: ItemState[] = ['equipped', 'carried', 'backpack']

export function InventoryTab({ sheet, reference, onError, refresh }: Props) {
  const [pickId, setPickId] = useState('')
  const [quantity, setQuantity] = useState(1)

  async function run(action: () => Promise<unknown>) {
    try {
      await action()
      await refresh()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Ошибка')
    }
  }

  const d = sheet.derived
  const picked = reference.items.find(i => i.id === pickId)

  return (
    <div>
      <section className="panel">
        <div className={d.encumbered ? 'enc-bar warn' : 'enc-bar'}>
          Переносимый вес: <strong>{d.encumbranceLoad} / {d.encumbranceThreshold}</strong>
          {d.encumbered && <span className="error"> — перегружен!</span>}
          <span className="muted"> · Поглощение {d.soak} · Защита {d.meleeDefense}/{d.rangedDefense}</span>
        </div>

        <div className="inline-form">
          <select value={pickId} onChange={e => setPickId(e.target.value)}>
            <option value="" disabled>— добавить предмет —</option>
            {(['weapon', 'armor', 'gear'] as const).map(kind => (
              <optgroup key={kind} label={ITEM_KIND_LABELS[kind]}>
                {reference.items.filter(i => i.kind === kind).map(i => (
                  <option key={i.id} value={i.id}>{i.name}{i.isCustom ? ' (кастом)' : ''}</option>
                ))}
              </optgroup>
            ))}
          </select>
          <input type="number" min={1} value={quantity} style={{ width: '4rem' }}
            onChange={e => setQuantity(Math.max(1, Number(e.target.value)))} />
          <button className="primary" disabled={!pickId}
            onClick={() => run(async () => {
              await api.addItem(sheet.id, pickId, quantity, 'carried')
              setPickId('')
              setQuantity(1)
            })}>
            Добавить
          </button>
        </div>
        {picked && <p className="hint">{picked.description} · Вес {picked.encumbrance}
          {picked.soakBonus > 0 && ` · Поглощение +${picked.soakBonus}`}
          {picked.meleeDefense > 0 && ` · Защита ближ. ${picked.meleeDefense}`}
          {picked.rangedDefense > 0 && ` · Защита дальн. ${picked.rangedDefense}`}
          {picked.encumbranceThresholdBonus > 0 && ` · Порог веса +${picked.encumbranceThresholdBonus}`}</p>}
      </section>

      <section className="panel">
        <h3>Инвентарь</h3>
        {sheet.items.length === 0 && <p className="muted">Пусто. Добавьте предметы из каталога или создайте кастомные на вкладке «Кастом».</p>}
        {sheet.items.length > 0 && (
          <table className="skills">
            <thead>
              <tr>
                <th>Предмет</th>
                <th>Тип</th>
                <th>Кол-во</th>
                <th>Вес (факт.)</th>
                <th>Состояние</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {sheet.items.map(item => (
                <tr key={item.id} className={item.state === 'equipped' ? 'equipped-row' : ''}>
                  <td>
                    <strong>{item.name}</strong>
                    {(item.soakBonus > 0 || item.meleeDefense > 0 || item.rangedDefense > 0 || item.encumbranceThresholdBonus > 0) && (
                      <div className="muted small-text">
                        {item.soakBonus > 0 && `Поглощение +${item.soakBonus} `}
                        {item.meleeDefense > 0 && `Защ.ближ ${item.meleeDefense} `}
                        {item.rangedDefense > 0 && `Защ.дальн ${item.rangedDefense} `}
                        {item.encumbranceThresholdBonus > 0 && `Порог веса +${item.encumbranceThresholdBonus} `}
                        {item.state !== 'equipped' && '(не действует — предмет не используется)'}
                      </div>
                    )}
                  </td>
                  <td className="muted">{ITEM_KIND_LABELS[item.kind]}</td>
                  <td>
                    <button className="tiny" onClick={() => item.quantity > 1 &&
                      run(() => api.updateItem(sheet.id, item.id, { quantity: item.quantity - 1 }))}>−</button>
                    {' '}{item.quantity}{' '}
                    <button className="tiny" onClick={() =>
                      run(() => api.updateItem(sheet.id, item.id, { quantity: item.quantity + 1 }))}>+</button>
                  </td>
                  <td>{item.load}</td>
                  <td>
                    <div className="state-switch">
                      {STATES.map(s => (
                        <button key={s}
                          className={item.state === s ? 'chip active' : 'chip'}
                          onClick={() => item.state !== s && run(() => api.updateItem(sheet.id, item.id, { state: s }))}>
                          {ITEM_STATE_LABELS[s]}
                        </button>
                      ))}
                    </div>
                  </td>
                  <td>
                    <button className="danger small" onClick={() => run(() => api.removeItem(sheet.id, item.id))}>✕</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        <p className="hint">
          «Используется» — бонусы предмета активны, надетая броня весит на 3 меньше.
          «Не используется» и «В рюкзаке» — вес учитывается полностью, бонусы не действуют.
        </p>
      </section>
    </div>
  )
}
