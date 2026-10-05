import { cleanup, render, screen } from '@testing-library/react'
import '@testing-library/jest-dom/vitest'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { BookReference } from './BookReference'

afterEach(() => { cleanup(); vi.doUnmock('../i18n'); vi.resetModules() })

describe('book references', () => {
  it.each([
    ['Genesys Core Rulebook, с. 73', 'https://www.edge-studio.net/games/genesys-core-rulebook/'],
    ['Genesys Core Rulebook (RU translation), с. 73', 'https://www.edge-studio.net/games/genesys-core-rulebook/'],
    ['Realms of Terrinoth, с. 77-78', 'https://www.edge-studio.net/games/realms-of-terrinoth/'],
  ])('links %s to the publisher, keeping the printed page', (source, url) => {
    render(<BookReference source={source} />)
    expect(screen.getByRole('link', { name: source })).toHaveAttribute('href', url)
    expect(screen.getByRole('link')).toHaveAttribute('rel', 'noopener noreferrer')
  })

  it('does not convert user-authored source text into a URL or HTML', () => {
    render(<BookReference source={'javascript:alert(1)<img src=x>'} />)
    expect(screen.queryByRole('link')).not.toBeInTheDocument()
    expect(screen.getByText('javascript:alert(1)<img src=x>')).toBeInTheDocument()
  })

  it('marks an unconfirmed legacy page explicitly', () => {
    render(<BookReference source="Genesys (legacy catalog), страница не подтверждена" />)
    expect(screen.getByText(/страница не подтверждена/)).toBeInTheDocument()
    expect(screen.queryByRole('link')).not.toBeInTheDocument()
  })

  it('localizes page notation in English', async () => {
    vi.doMock('../i18n', () => ({ t: (_ru: unknown, en: unknown) => en }))
    const { BookReference: EnglishReference } = await import('./BookReference')
    render(<EnglishReference source="Realms of Terrinoth, с. 79" />)
    expect(screen.getByRole('link', { name: 'Realms of Terrinoth, p. 79' })).toBeInTheDocument()
  })
})
