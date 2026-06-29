import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { CharacterSheet, RulesResponse } from '../api/types'
import { CriticalInjuriesSection } from './CriticalInjuriesSection'

const rulesMock = vi.fn()
const addMock = vi.fn()
const removeMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    rules: () => rulesMock(),
    addCriticalInjury: (...a: unknown[]) => addMock(...a),
    removeCriticalInjury: (...a: unknown[]) => removeMock(...a),
  },
}))

const rules: RulesResponse = {
  entries: [
    { id: 'r1', kind: 'criticalInjury', code: 'crit-ci_001_005', nameRu: 'Небольшая царапина', nameEn: 'Minor Nick',
      groupRu: 'Лёгкая', sortOrder: 1, rollRange: '01-05', symbolCost: '', body: 'Цель получает 1 усталость.',
      notes: '', source: '', sourcePage: '' },
    { id: 'r2', kind: 'difficulty', code: 'diff', nameRu: 'Простая', nameEn: '', groupRu: '', sortOrder: 1,
      rollRange: '', symbolCost: '', body: '', notes: '', source: '', sourcePage: '' },
  ],
}

const baseSheet = {
  id: 'char-1',
  criticalInjuries: [
    { id: 'ci-1', ruleCode: 'crit-ci_001_005', nameRu: 'Небольшая царапина', severity: 'Лёгкая', rollResult: 3, notes: null },
  ],
} as unknown as CharacterSheet

describe('CriticalInjuriesSection (U-23)', () => {
  beforeEach(() => {
    rulesMock.mockResolvedValue(rules)
    addMock.mockResolvedValue({ id: 'new' })
    removeMock.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('показывает существующие ранения и снимает их', async () => {
    const refresh = vi.fn().mockResolvedValue(undefined)
    render(<CriticalInjuriesSection sheet={baseSheet} onError={() => {}} refresh={refresh} />)

    expect(screen.getByText('Небольшая царапина')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Снять' }))
    await waitFor(() => expect(removeMock).toHaveBeenCalledWith('char-1', 'ci-1'))
    expect(refresh).toHaveBeenCalled()
  })

  it('добавляет ранение из таблицы U-11 по коду', async () => {
    const refresh = vi.fn().mockResolvedValue(undefined)
    render(<CriticalInjuriesSection sheet={baseSheet} onError={() => {}} refresh={refresh} />)

    // дождаться загрузки таблицы (optgroup из criticalInjury, без difficulty)
    await waitFor(() => expect(screen.getByRole('option', { name: /Небольшая царапина/ })).toBeTruthy())
    fireEvent.change(screen.getByLabelText('Крит-ранение'), { target: { value: 'crit-ci_001_005' } })
    fireEvent.click(screen.getByRole('button', { name: 'Добавить' }))

    await waitFor(() => expect(addMock).toHaveBeenCalledWith('char-1', expect.objectContaining({ ruleCode: 'crit-ci_001_005' })))
  })
})
