import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { RuleText } from './RuleText'

describe('RuleText', () => {
  it('colors dice and result symbols consistently in Russian talent text', () => {
    render(<p><RuleText text="Добавьте ◻ и уберите ■; за каждый ✶ и ✸, ▲ и ▼ можно потратить ★ или ☠; ♦ — сложность." /></p>)

    expect(screen.getByLabelText('Бонусная кость').className).toContain('rule-boost')
    expect(screen.getByLabelText('Кость помехи').className).toContain('rule-setback')
    expect(screen.getByLabelText('Успех').className).toContain('rule-success')
    expect(screen.getByLabelText('Провал').className).toContain('rule-failure')
    expect(screen.getByLabelText('Преимущество').className).toContain('rule-advantage')
    expect(screen.getByLabelText('Угроза').className).toContain('rule-threat')
    expect(screen.getByLabelText('Триумф').className).toContain('rule-triumph')
    expect(screen.getByLabelText('Отчаяние').className).toContain('rule-despair')
    expect(screen.getByLabelText('Кость сложности').className).toContain('rule-difficulty')
  })

  it('colors word forms and keeps ordinary words intact', () => {
    render(<p><RuleText text="Each success and advantage adds a boost die, not a successful result." /></p>)

    expect(screen.getAllByLabelText('Успех')).toHaveLength(1)
    expect(screen.getByLabelText('Преимущество').className).toContain('rule-advantage')
    expect(screen.getByLabelText('Бонусная кость').className).toContain('rule-boost')
    expect(screen.getByText(/successful/)).toBeTruthy()
  })

  it('does not turn a silhouette zero into a boost die', () => {
    render(<p><RuleText text="Фигура 0, а не ◻." /></p>)
    expect(screen.getByText(/Фигура 0/)).toBeTruthy()
    expect(screen.getByLabelText('Бонусная кость')).toBeTruthy()
  })
})
