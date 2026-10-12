import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { RuleText } from './RuleText'

describe('RuleText', () => {
  it('preserves meaning and grammatical agreement in prose mentioning old result words', () => {
    const text = 'Каждая угроза даёт усталость. Одно отчаяние. Представляет угрозу; кость помехи.'
    const { container } = render(<p><RuleText text={text} /></p>)
    expect(container.textContent).toBe(text)
    expect(screen.getByLabelText('Крах').textContent).toBe('отчаяние')
  })

  it('preserves contact addresses and Markdown words rather than treating them as OCR symbols', () => {
    const { container } = render(<p><RuleText symbolsOnly text="mail@example.test и *важное* слово" /></p>)
    expect(container.querySelector('.genesys-symbol')).toBeNull()
    expect(container.textContent).toBe('mail@example.test и *важное* слово')
  })
  it('colors dice and result symbols consistently in Russian talent text', () => {
    render(<p><RuleText text="Добавьте ◻ и уберите ■; за каждый ✶ и ✸, ▲ и ▼ можно потратить ★ или ☠; ♦ — сложность." /></p>)

    expect(screen.getByLabelText('Кость бонуса').className).toContain('rule-boost')
    expect(screen.getByLabelText('Кость штрафа').className).toContain('rule-setback')
    expect(screen.getByLabelText('Успех').className).toContain('rule-success')
    expect(screen.getByLabelText('Провал').className).toContain('rule-failure')
    expect(screen.getByLabelText('Преимущество').className).toContain('rule-advantage')
    expect(screen.getByLabelText('Осложнение').className).toContain('rule-threat')
    expect(screen.getByLabelText('Триумф').className).toContain('rule-triumph')
    expect(screen.getByLabelText('Крах').className).toContain('rule-despair')
    expect(screen.getByLabelText('Кость сложности').className).toContain('rule-difficulty')
  })

  it('colors word forms and keeps ordinary words intact', () => {
    render(<p><RuleText text="Each success and advantage adds a boost die, not a successful result." /></p>)

    expect(screen.getAllByLabelText('Успех')).toHaveLength(1)
    expect(screen.getByLabelText('Преимущество').className).toContain('rule-advantage')
    expect(screen.getByLabelText('Кость бонуса').className).toContain('rule-boost')
    expect(screen.getByText(/successful/)).toBeTruthy()
  })

  it('does not turn a silhouette zero into a boost die', () => {
    render(<p><RuleText text="Фигура 0, а не ◻." /></p>)
    expect(screen.getByText(/Фигура 0/)).toBeTruthy()
    expect(screen.getByLabelText('Кость бонуса')).toBeTruthy()
  })

  it('does not turn the Russian conjunction «а» into an advantage symbol', () => {
    const { rerender } = render(<p><RuleText text="◻ к проверкам, а союзники получают ◻ ◻." /></p>)

    expect(screen.getAllByLabelText('Кость бонуса')).toHaveLength(3)
    expect(screen.queryByLabelText('Преимущество')).toBeNull()

    rerender(<p><RuleText legacyOcr text="Старый текст с А." /></p>)
    expect(screen.getByLabelText('Преимущество')).toBeTruthy()
  })

  it.each([true, false])('preserves conjunctions, initials and list markers (symbolsOnly=%s)', symbolsOnly => {
    const text = 'А потом он сбегает. Капитан А. Иванов. * стреляет дважды; @ заметка.'
    const { container } = render(<p><RuleText symbolsOnly={symbolsOnly} text={text} /></p>)
    expect(container.textContent).toBe(text)
    expect(container.querySelector('.genesys-symbol')).toBeNull()
  })

  it('requires explicit legacy OCR opt-in for ambiguous placeholders', () => {
    render(<p><RuleText symbolsOnly legacyOcr text="А * @" /></p>)
    for (const name of ['Преимущество', 'Успех', 'Триумф']) expect(screen.getByRole('img', { name })).toBeTruthy()
  })

  it('renders supplementary publisher symbols even when adjacent to text', () => {
    render(<p><RuleText symbolsOnly text={'Текст\u{F32B1}\u{F32B2}\u{F32B3}\u{F32B4}\u{F32B5}\u{F32B6}конец'} /></p>)
    for (const name of ['Осложнение', 'Крах', 'Успех', 'Провал', 'Преимущество', 'Триумф']) {
      expect(screen.getByRole('img', { name })).toBeTruthy()
    }
    expect(document.body.textContent).toContain('Текст')
    expect(document.body.textContent).toContain('конец')
  })

  it('keeps prose in the text font and converts only explicit glyphs in symbol-only mode', () => {
    const { container } = render(<p><RuleText symbolsOnly text="Успех требует 2 кости бонуса и ◻." /></p>)
    expect(container.querySelectorAll('.genesys-symbol')).toHaveLength(1)
    expect(screen.getByText(/Успех требует 2 кости бонуса/)).toBeTruthy()
    expect(screen.getByRole('img', { name: 'Кость бонуса' })).toBeTruthy()
  })

  it('localizes structured English symbol costs as icons with book labels', () => {
    render(<p><RuleText wordsAsSymbols text="Threat 2 or Despair 1" /></p>)
    expect(screen.getByRole('img', { name: 'Осложнение' })).toBeTruthy()
    expect(screen.getByRole('img', { name: 'Крах' })).toBeTruthy()
    expect(screen.queryByText('Threat')).toBeNull()
    expect(screen.getByText(/2 or/)).toBeTruthy()
  })
})
