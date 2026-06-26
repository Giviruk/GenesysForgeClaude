import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { RulesResponse, SearchResponse } from '../api/types'
import { ReferencePage } from './ReferencePage'

const rules: RulesResponse = {
  entries: [
    { id: '1', kind: 'difficulty', code: 'diff-easy', nameRu: 'Лёгкая', nameEn: 'Easy',
      groupRu: '', sortOrder: 1, rollRange: '', symbolCost: '1 ♦', body: 'Один куб сложности.',
      notes: '', source: 'Core', sourcePage: '20' },
    { id: '2', kind: 'criticalInjury', code: 'crit-1', nameRu: 'Царапина', nameEn: 'Nick',
      groupRu: 'Лёгкая', sortOrder: 1, rollRange: '01-05', symbolCost: '♦ (1)',
      body: 'Цель получает 1 усталость.', notes: '', source: 'Core', sourcePage: '115' },
  ],
}

const searchResult: SearchResponse = {
  hits: [{ type: 'rule', group: 'Правила', title: 'Царапина', subtitle: 'Лёгкая',
    snippet: 'Цель получает 1 усталость.', route: '/reference' }],
}

const rulesMock = vi.fn().mockResolvedValue(rules)
const searchMock = vi.fn().mockResolvedValue(searchResult)
vi.mock('../api/client', () => ({
  api: {
    rules: () => rulesMock(),
    search: (...args: unknown[]) => searchMock(...args),
  },
}))

describe('ReferencePage', () => {
  it('shows the default section and switches sections on tab click', async () => {
    render(<ReferencePage onNavigate={() => {}} />)
    // По умолчанию открыт раздел «Сложности»; криты скрыты до переключения.
    await waitFor(() => expect(screen.getByRole('heading', { name: /Сложности/ })).toBeTruthy())
    expect(screen.queryByRole('heading', { name: /Критические ранения/ })).toBeNull()

    fireEvent.click(screen.getByRole('tab', { name: /Криты/ }))
    expect(screen.getByRole('heading', { name: /Критические ранения/ })).toBeTruthy()
    expect(screen.getByText('01-05')).toBeTruthy()
    // Раздел «Сложности» при этом скрывается (показываем по одному).
    expect(screen.queryByRole('heading', { name: /^Сложности/ })).toBeNull()
  })

  it('shows every section when "Все" is selected', async () => {
    render(<ReferencePage onNavigate={() => {}} />)
    await waitFor(() => expect(screen.getByRole('tab', { name: /Все/ })).toBeTruthy())
    fireEvent.click(screen.getByRole('tab', { name: /Все/ }))
    expect(screen.getByRole('heading', { name: /Сложности/ })).toBeTruthy()
    expect(screen.getByRole('heading', { name: /Критические ранения/ })).toBeTruthy()
  })

  it('runs global search and navigates on hit click', async () => {
    const onNavigate = vi.fn()
    render(<ReferencePage onNavigate={onNavigate} />)
    await waitFor(() => expect(screen.getByRole('heading', { name: /Сложности/ })).toBeTruthy())

    const box = screen.getByPlaceholderText(/Глобальный поиск/)
    fireEvent.change(box, { target: { value: 'усталость' } })

    await waitFor(() => expect(screen.getByRole('heading', { name: /Правила/ })).toBeTruthy())
    expect(searchMock).toHaveBeenCalledWith('realmsOfTerrinoth', 'усталость')

    fireEvent.click(screen.getByText('Царапина', { selector: '.hit-title' }))
    expect(onNavigate).toHaveBeenCalledWith('/reference')
  })
})
