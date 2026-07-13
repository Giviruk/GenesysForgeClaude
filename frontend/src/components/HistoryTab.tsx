import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { CharacterAuditAction, CharacterAuditEntry } from '../api/types'
import { lang, t } from '../i18n'

interface Props {
  characterId: string
  onError: (message: string) => void
  /** Перечитать лист (после выдачи XP меняется totalXp в шапке). */
  refresh: () => Promise<void>
}

const ACTION_LABELS: Record<CharacterAuditAction, string> = t({
  xpAwarded: 'Выдача XP',
  characteristicBought: 'Характеристика',
  characteristicRefunded: 'Возврат хар-ки',
  skillRankBought: 'Навык',
  skillRankRefunded: 'Возврат навыка',
  talentBought: 'Талант',
  talentRefunded: 'Возврат таланта',
  itemBought: 'Предмет +',
  itemSold: 'Продажа',
  itemRemoved: 'Предмет −',
  heroicAbilityChanged: 'Героика',
  creationCompleted: 'Создание',
  manualEdit: 'Правка',
}, {
  xpAwarded: 'XP award',
  characteristicBought: 'Characteristic',
  characteristicRefunded: 'Char. refund',
  skillRankBought: 'Skill',
  skillRankRefunded: 'Skill refund',
  talentBought: 'Talent',
  talentRefunded: 'Talent refund',
  itemBought: 'Item +',
  itemSold: 'Sale',
  itemRemoved: 'Item −',
  heroicAbilityChanged: 'Heroic ability',
  creationCompleted: 'Creation',
  manualEdit: 'Edit',
})

export function HistoryTab({ characterId, onError, refresh }: Props) {
  const [entries, setEntries] = useState<CharacterAuditEntry[] | null>(null)
  const [amount, setAmount] = useState('')
  const [note, setNote] = useState('')

  const reload = useCallback(() =>
    api.characterAudit(characterId)
      .then(setEntries)
      .catch((e: unknown) => onError(e instanceof Error ? e.message : t('Ошибка загрузки истории', 'Failed to load history'))),
    [characterId, onError])

  useEffect(() => { void reload() }, [reload])

  async function award() {
    const value = Math.trunc(Number(amount))
    if (!Number.isFinite(value) || value === 0) return
    try {
      await api.awardXp(characterId, { amount: value, note: note.trim() || undefined })
      setAmount('')
      setNote('')
      await reload()
      await refresh()
    } catch (e) {
      onError(e instanceof Error ? e.message : t('Ошибка выдачи XP', 'Failed to award XP'))
    }
  }

  return (
    <div className="history-tab">
      <section className="panel award-xp">
        <h3>{t('Выдать XP', 'Award XP')}</h3>
        <div className="form-row">
          <input className="ranks-input" type="number" placeholder="±XP" value={amount}
            onChange={e => setAmount(e.target.value)} title={t('Сколько XP выдать (можно отрицательное для коррекции)', 'How much XP to award (negative values allowed for corrections)')} />
          <input className="grow" placeholder={t('Комментарий (необязательно)', 'Comment (optional)')} value={note}
            onChange={e => setNote(e.target.value)} />
          <button className="primary small" onClick={() => void award()}
            disabled={!Number.isFinite(Number(amount)) || Math.trunc(Number(amount)) === 0}>
            {t('Выдать', 'Award')}
          </button>
        </div>
      </section>

      <section className="panel">
        <h3>{t('История изменений', 'Change history')}</h3>
        {entries === null && <p className="muted">{t('Загрузка…', 'Loading…')}</p>}
        {entries !== null && entries.length === 0 && <p className="muted">{t('Записей пока нет.', 'No entries yet.')}</p>}
        {entries !== null && entries.length > 0 && (
          <table className="audit-table">
            <thead>
              <tr>
                <th>{t('Дата', 'Date')}</th>
                <th>{t('Тип', 'Type')}</th>
                <th>{t('Описание', 'Description')}</th>
                <th className="right">ΔXP</th>
                <th className="right" title={t('Доступно / Всего после операции', 'Available / Total after the operation')}>{t('После', 'After')}</th>
              </tr>
            </thead>
            <tbody>
              {entries.map(e => (
                <tr key={e.id}>
                  <td className="muted small-text nowrap">{new Date(e.createdAt).toLocaleString(lang === 'ru' ? 'ru-RU' : 'en-US')}</td>
                  <td><span className="badge">{ACTION_LABELS[e.action]}</span></td>
                  <td>{e.summary}</td>
                  <td className="right">
                    {e.xpDelta != null && e.xpDelta !== 0 && (
                      <span className={e.xpDelta > 0 ? 'xp-pos' : 'xp-neg'}>
                        {e.xpDelta > 0 ? '+' : ''}{e.xpDelta}
                      </span>
                    )}
                  </td>
                  <td className="right muted small-text nowrap">
                    {e.totalXpAfter - e.spentXpAfter} / {e.totalXpAfter}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  )
}
