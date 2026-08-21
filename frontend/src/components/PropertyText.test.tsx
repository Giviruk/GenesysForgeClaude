import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Quality } from '../api/types'
import { PropertyText } from './PropertyText'

const blast: Quality = {
  id: 'blast', code: 'blast', nameEn: 'Blast', nameRu: 'Взрыв', kind: 'itemQuality',
  isActive: true, hasRating: true, activationCost: '2', category: 'combat',
  description: 'Соседние цели получают урон после активации.', safeDescription: 'Урон соседним целям.',
  descriptionEn: 'Adjacent targets suffer damage after activation.', source: 'Test',
}

describe('PropertyText', () => {
  it('делает название свойства внутри описания интерактивным', () => {
    render(<p><PropertyText text={'Атака получает свойство «Взрыв».'} qualities={[blast]} /></p>)

    const property = screen.getByRole('button', { name: /Взрыв/ })
    expect(screen.queryByRole('tooltip')).toBeNull()
    fireEvent.mouseEnter(property)
    expect(screen.getByRole('tooltip').textContent).toMatch(/Соседние цели получают урон/)
  })

  it('не подменяет часть другого слова совпавшим свойством', () => {
    render(<PropertyText text="Взрывное заклинание не содержит отдельного свойства." qualities={[blast]} />)
    expect(screen.queryByRole('button')).toBeNull()
    expect(screen.getByText(/Взрывное заклинание/)).toBeTruthy()
  })
})
