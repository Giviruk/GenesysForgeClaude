import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { PropertyTags } from './PropertyTags'

describe('PropertyTags', () => {
  it('локализует английское имя встроенного качества и сохраняет рейтинг', () => {
    render(<PropertyTags properties="Vicious 3" />)

    const property = screen.getByRole('button', { name: 'Высококритичное 3' })
    expect(property).toBeTruthy()
    expect(screen.queryByText('Vicious 3')).toBeNull()

    fireEvent.mouseEnter(property)
    expect(screen.getByRole('tooltip').textContent).toMatch(/критической травмы/)
  })

  it('использует серверное описание для пользовательского качества приватной кампании', () => {
    render(<PropertyTags
      properties="Special 1"
      qualityDefinitions={[{
        code: 'special', nameRu: 'Особое', nameEn: 'Special', hasRating: true,
        description: 'Собственное описание качества.', safeDescription: '', descriptionEn: '',
      }]} />)

    const property = screen.getByRole('button', { name: 'Особое 1' })
    fireEvent.mouseEnter(property)
    expect(screen.getByRole('tooltip').textContent).toMatch(/Собственное описание качества/)
  })
})
