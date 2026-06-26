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
    { id: '3', kind: 'symbolSpend', code: 'spend-pos', nameRu: 'Восстановить усталость', nameEn: '',
      groupRu: 'Бой', sortOrder: 1, rollRange: '', symbolCost: '1 Advantage или 1 Triumph',
      body: 'Снять 1 усталость.', notes: '', source: 'Core', sourcePage: '104' },
    { id: '4', kind: 'symbolSpend', code: 'spend-neg', nameRu: 'Получить усталость', nameEn: '',
      groupRu: 'Бой', sortOrder: 2, rollRange: '', symbolCost: '1 Threat или 1 Despair',
      body: 'Цель получает 1 усталость.', notes: '', source: 'Core', sourcePage: '104' },
    { id: '5', kind: 'symbolSpend', code: 'spend-social', nameRu: 'Светская деталь', nameEn: '',
      groupRu: 'Социальная сцена', sortOrder: 3, rollRange: '', symbolCost: '2 Advantage',
      body: 'Узнать деталь о собеседнике.', notes: '', source: 'Core', sourcePage: '110' },
    { id: '6', kind: 'rangeBand', code: 'range-engaged', nameRu: 'Вплотную', nameEn: 'Engaged',
      groupRu: 'Общая информация', sortOrder: 0, rollRange: '', symbolCost: '',
      body: 'Непосредственный контакт.', notes: '', source: 'Core', sourcePage: '105' },
    { id: '7', kind: 'rangeBand', code: 'range-move', nameRu: 'Короткая <-> Средняя', nameEn: '',
      groupRu: 'Перемещение', sortOrder: 7, rollRange: '', symbolCost: '1 манёвр(ов)',
      body: 'Переход между дистанциями.', notes: '', source: 'Core', sourcePage: '105' },
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

  it('filters spends by situation and by symbol polarity', async () => {
    render(<ReferencePage onNavigate={() => {}} />)
    await waitFor(() => expect(screen.getByRole('tab', { name: /Траты/ })).toBeTruthy())
    fireEvent.click(screen.getByRole('tab', { name: /Траты/ }))

    // Все траты боя видны изначально.
    expect(screen.getByText('Снять 1 усталость.')).toBeTruthy()
    expect(screen.getByText('Цель получает 1 усталость.')).toBeTruthy()

    // Только негативные символы — позитивная трата скрывается.
    fireEvent.click(screen.getByRole('tab', { name: /Угрозы и крахи/ }))
    expect(screen.queryByText('Снять 1 усталость.')).toBeNull()
    expect(screen.getByText('Цель получает 1 усталость.')).toBeTruthy()

    // Сменить ситуацию на социальную (символы пока «Угрозы») — строк нет.
    fireEvent.click(screen.getByRole('tab', { name: /Социальная сцена/ }))
    expect(screen.getByText(/нет строк/i)).toBeTruthy()

    // Любые символы + социальная сцена — видна светская деталь.
    fireEvent.click(screen.getByRole('tab', { name: /Любые символы/ }))
    expect(screen.getByText('Узнать деталь о собеседнике.')).toBeTruthy()
  })

  it('splits ranges into «Общая информация» (no cost) and «Перемещение» (with maneuvers)', async () => {
    render(<ReferencePage onNavigate={() => {}} />)
    await waitFor(() => expect(screen.getByRole('tab', { name: /Дистанции/ })).toBeTruthy())
    fireEvent.click(screen.getByRole('tab', { name: /Дистанции/ }))

    // По умолчанию «Общая информация»: банда видна, колонки «Манёвры» нет.
    expect(screen.getByText('Вплотную')).toBeTruthy()
    expect(screen.queryByText('Короткая <-> Средняя')).toBeNull()
    expect(screen.queryByRole('columnheader', { name: 'Манёвры' })).toBeNull()

    // «Перемещение»: переход со стоимостью, колонка «Манёвры» появляется.
    fireEvent.click(screen.getByRole('tab', { name: /^Перемещение$/ }))
    expect(screen.getByText('Короткая <-> Средняя')).toBeTruthy()
    expect(screen.getByRole('columnheader', { name: 'Манёвры' })).toBeTruthy()
    expect(screen.getByText('1 манёвр(ов)')).toBeTruthy()
    expect(screen.queryByText('Вплотную')).toBeNull()
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
