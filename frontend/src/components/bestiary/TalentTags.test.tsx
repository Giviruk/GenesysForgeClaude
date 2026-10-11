import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { TalentDef } from '../../api/types'
import { TalentTags } from './TalentTags'
const talent = { name: 'Guard', nameRu: 'Страж', tier: 2, activationEn: 'Passive', activation: 'Пассивный', isRanked: true, description: 'Проверочное описание из справочника', descriptionEn: 'Reference test description', safeDescription: '' } as TalentDef
function media(matches: boolean) {
  vi.stubGlobal('matchMedia', () => ({ matches, addEventListener: vi.fn(), removeEventListener: vi.fn() }))
}
afterEach(() => vi.unstubAllGlobals())
describe('TalentTags', () => {
  it('shows reference description on hover/focus, pins with keyboard, closes on Escape and outside click', () => {
    media(false)
    render(<TalentTags values={['Страж 1']} talents={[talent]} />)
    const button = screen.getByRole('button', { name: 'Страж 1' })
    fireEvent.mouseEnter(button)
    expect(screen.getByRole('tooltip').textContent).toContain(talent.description)
    expect(screen.getByRole('tooltip').textContent).toContain('пассивный')
    fireEvent.click(button)
    fireEvent.mouseLeave(button)
    expect(screen.getByRole('tooltip')).toBeTruthy()
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(screen.queryByRole('tooltip')).toBeNull()
    fireEvent.focus(button)
    expect(button.getAttribute('aria-describedby')).toBe(screen.getByRole('tooltip').id)
    fireEvent.click(button)
    fireEvent.pointerDown(document.body)
    expect(screen.queryByRole('tooltip')).toBeNull()
  })
  it('uses inline disclosure on mobile and switches between talents without inventing content', () => {
    media(true)
    const { container } = render(<TalentTags values={['Страж 1', 'Новый талант']} talents={[talent]} />)
    fireEvent.click(screen.getByRole('button', { name: 'Страж 1' }))
    expect(container.querySelector('[role=tooltip]')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Новый талант' }))
    expect(screen.getByRole('tooltip').textContent).toContain('Описание не найдено')
    fireEvent.click(screen.getByRole('button', { name: 'Закрыть описание таланта' }))
    expect(screen.queryByRole('tooltip')).toBeNull()
  })
})
