import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { MarkdownContent } from './MarkdownContent'
import { markdownHeadings } from '../utils/markdown'

describe('MarkdownContent', () => {
  it('renders headings, lists, formatting, tables and safe links', () => {
    render(<MarkdownContent markdown={'# Глава\n\n**Важно**\n\n- один\n- два\n\n| Кто | Где |\n|---|---|\n| Герой | Город |'} />)
    expect(screen.getByRole('heading', { name: 'Глава' })).toBeTruthy()
    expect(screen.getByText('Важно').tagName).toBe('STRONG')
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
    expect(screen.getByRole('table')).toBeTruthy()
  })

  it('does not execute raw html and routes entity links through callback', () => {
    const onEntity = vi.fn()
    render(<MarkdownContent markdown={'<script>alert(1)</script>\n\n[Бард](character:11111111-1111-1111-1111-111111111111)'}
      onEntityLink={onEntity} />)
    expect(document.querySelector('script')).toBeNull()
    fireEvent.click(screen.getByRole('link', { name: 'Бард' }))
    expect(onEntity).toHaveBeenCalledWith({ kind: 'character', id: '11111111-1111-1111-1111-111111111111' })
  })

  it('builds table of contents from markdown headings', () => {
    expect(markdownHeadings('# Пролог\n\n## Первая встреча')).toEqual([
      { level: 1, text: 'Пролог', id: 'пролог' },
      { level: 2, text: 'Первая встреча', id: 'первая-встреча' },
    ])
  })
})
