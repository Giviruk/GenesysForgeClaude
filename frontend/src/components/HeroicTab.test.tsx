import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/client'
import type { CharacterSheet, Reference } from '../api/types'
import { HeroicTab } from './HeroicTab'

vi.mock('../api/client', () => ({ api: {
  activateCharacterAbility: vi.fn(), setHeroicUpgrades: vi.fn(),
} }))
const sheet = {
  id: 'heroic-test', system: 'realmsOfTerrinoth', isCreationPhase: false,
  heroicAbility: { id: 'h1', code: 'test-effect', name: 'Test effect', nameRu: 'Тестовый эффект',
    description: 'Собственная тестовая способность.', source: '', upgrades: [
      { level: 1, cost: 1, description: 'Сильнее.', notes: '' },
      { level: 2, cost: 2, description: 'Ещё сильнее.', notes: '' },
    ], effects: [], activationCost: '2 Story Points', activation: 'Incidental',
    duration: '2 rounds', frequency: 'Once per session', notes: '', requirement: '' },
  heroicIdentity: { customName: 'Наследие', complete: true, originPrimary: 'destiny',
    originSecondary: null, originMode: 'standard', originRolls: [] },
  heroicConfiguration: { kind: 'none', complete: true },
  heroicIdentityIncomplete: false, heroicConfigurationIncomplete: false,
  heroicUpgrades: { powerRank: 0, durationRanks: 0, frequencyRanks: 0, story: false, secondaryEffects: [] },
  heroicUpgradeRank: 0, heroicUpgradePointsTotal: 5, heroicUpgradePointsSpent: 0,
} as unknown as CharacterSheet
const reference = { heroicSecondaryEffects: [{ id: 's1', name: 'Second effect',
  nameRu: 'Вторичный эффект', description: 'Тест.', source: '' }] } as Reference
const renderTab = (onError = vi.fn()) => render(
  <HeroicTab sheet={sheet} reference={reference} onError={onError} refresh={async () => {}} />)

describe('Heroic tab uses and purchases', () => {
  beforeEach(() => {
    localStorage.removeItem('genesysforge.heroic-uses.heroic-test')
    vi.clearAllMocks()
    vi.mocked(api.setHeroicUpgrades).mockResolvedValue(undefined)
    vi.mocked(api.activateCharacterAbility).mockResolvedValue({ sheet, abilityName: 'Наследие',
      applied: ['Состояние изменено'], manual: ['Решение ведущего'] })
  })
  it('counts successful activation, survives reopening and resets for a new session', async () => {
    const view = renderTab()
    fireEvent.click(screen.getByRole('button', { name: 'Активировать' }))
    expect(await screen.findByText('Состояние изменено')).toBeTruthy()
    expect(screen.getByText('Решение ведущего')).toBeTruthy()
    await waitFor(() => expect(screen.getByRole('button', { name: 'Активировать' }).hasAttribute('disabled')).toBe(true))
    expect(localStorage.getItem('genesysforge.heroic-uses.heroic-test')).toBe('1')
    view.unmount()
    renderTab()
    expect(screen.getByText('применения исчерпаны')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Новая сессия' }))
    expect(screen.getByText('осталось 1 из 1')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Активировать' }).hasAttribute('disabled')).toBe(false)
  })
  it('does not consume a use when the server rejects activation', async () => {
    const onError = vi.fn()
    vi.mocked(api.activateCharacterAbility).mockRejectedValue(new Error('Не удалось активировать'))
    renderTab(onError)
    fireEvent.click(screen.getByRole('button', { name: 'Активировать' }))
    await waitFor(() => expect(onError).toHaveBeenCalledWith('Не удалось активировать'))
    expect(screen.getByText('осталось 1 из 1')).toBeTruthy()
    expect(localStorage.getItem('genesysforge.heroic-uses.heroic-test')).toBeNull()
  })
  it.each([['Купить Длительность', { durationRanks: 1 }], ['Купить Частоту', { frequencyRanks: 1 }],
    ['Купить Сюжет', { story: true }]])('buys %s using the existing contract', async (name, patch) => {
    renderTab()
    fireEvent.click(screen.getByRole('button', { name }))
    await waitFor(() => expect(api.setHeroicUpgrades).toHaveBeenCalledWith('heroic-test', expect.objectContaining(patch)))
  })
  it('buys the next power level and a secondary effect', async () => {
    renderTab()
    fireEvent.click(within(document.querySelector('.heroic-power-level.next')!).getByRole('button', { name: 'Купить' }))
    await waitFor(() => expect(api.setHeroicUpgrades).toHaveBeenCalledWith('heroic-test', expect.objectContaining({ powerRank: 1 })))
    fireEvent.click(screen.getByRole('button', { name: '1 очк.' }))
    await waitFor(() => expect(api.setHeroicUpgrades).toHaveBeenCalledWith('heroic-test', expect.objectContaining({ secondaryEffectIds: ['s1'] })))
  })
  it('shows the complete origin only once', () => {
    renderTab()
    expect(screen.getAllByText(/Избранность судьбой/)).toHaveLength(1)
  })

})
