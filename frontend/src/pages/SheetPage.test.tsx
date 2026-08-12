import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseSheet, Reference, SheetSliceName, SheetSlices } from '../api/types'

const sheetSlices = vi.fn<(id: string, include: SheetSliceName[]) => Promise<SheetSlices>>()
const updateCharacter = vi.fn(() => Promise.resolve())
const reference = vi.fn(() => Promise.resolve({ items: [], talents: [], qualities: [] } as unknown as Reference))
const takeFreshSlices = vi.fn<(id: string) => SheetSlices | null>(() => null)
const setActiveSlices = vi.fn()

vi.mock('../api/client', () => ({
  api: {
    sheetSlices: (id: string, include: SheetSliceName[]) => sheetSlices(id, include),
    reference: () => reference(),
    updateCharacter: () => updateCharacter(),
    sheet: () => Promise.resolve(null),
  },
  takeFreshSlices: (id: string) => takeFreshSlices(id),
  setActiveSlices: (s: SheetSliceName[]) => setActiveSlices(s),
}))

// Вкладки подменены: проверяется, что и когда грузит страница, а не что рисуют вкладки.
vi.mock('../components/SheetTab', () => ({ SheetTab: () => <div>вкладка листа</div> }))
vi.mock('../components/TalentsTab', () => ({ TalentsTab: () => <div>вкладка талантов</div> }))
vi.mock('../components/InventoryTab', () => ({ InventoryTab: () => <div>вкладка инвентаря</div> }))

const { SheetPage } = await import('./SheetPage')

const base = {
  id: 'c1', name: 'Гарет', system: 'realmsOfTerrinoth', totalXp: 100, spentXp: 0, availableXp: 100,
  money: 10, portraitUrl: null, archetype: { name: 'Человек' }, career: { name: 'Воин' },
  derived: {}, skills: [], characteristics: {},
} as unknown as BaseSheet

/**
 * Отдаёт ровно запрошенные части — как это делает сервер, включая `null` у незапрошенных.
 *
 * <p>Именно `null`, а не отсутствующее поле: сервер сериализует `null`-ы, и подделка, которая их
 * опускала, однажды уже скрыла настоящий баг — вкладки считали незагруженное загруженным, ничего
 * не запрашивали и рисовали пустые списки.</p>
 */
function serve(include: SheetSliceName[]): SheetSlices {
  const has = (name: SheetSliceName) => include.includes(name)
  return {
    base: has('base') ? base : null,
    items: has('items') ? [] : null,
    talents: has('talents') ? [] : null,
    talentTierCounts: has('talents') ? {} : null,
    mounts: has('mounts') ? [] : null,
    attachments: has('attachments') ? [] : null,
  }
}

const includesOf = () => sheetSlices.mock.calls.map(([, include]) => include.join(','))

function renderPage() {
  return render(
    <SheetPage characterId="c1" printing={false}
      onOpenPrint={() => {}} onClosePrint={() => {}} onBack={() => {}} />)
}

/**
 * Лист играющего персонажа весит около 116 КБ, и две трети из них — инвентарь. Страница обязана
 * брать только то, что нужно открытой вкладке, иначе разделение на части не даёт ничего.
 */
describe('SheetPage — части листа грузятся по надобности', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.removeItem('genesysforge.sheet-tab.c1')
    takeFreshSlices.mockReturnValue(null)
    sheetSlices.mockImplementation((_id, include) => Promise.resolve(serve(include)))
  })

  it('при открытии берётся только базовая часть — без инвентаря и талантов', async () => {
    renderPage()

    await screen.findByText('вкладка листа')
    expect(includesOf()).toEqual(['base'])
  })

  it('переход на вкладку догружает только недостающее', async () => {
    renderPage()
    await screen.findByText('вкладка листа')

    fireEvent.click(screen.getByRole('button', { name: 'Инвентарь' }))

    await screen.findByText('вкладка инвентаря')
    // Базовая часть уже есть — заново её не просят.
    expect(includesOf()).toEqual(['base', 'items'])
  })

  it('восстанавливает последнюю вкладку персонажа после повторного монтирования', async () => {
    const first = renderPage()
    await screen.findByText('вкладка листа')
    fireEvent.click(screen.getByRole('button', { name: 'Инвентарь' }))
    await screen.findByText('вкладка инвентаря')
    first.unmount()

    renderPage()
    await screen.findByText('вкладка инвентаря')
    expect(localStorage.getItem('genesysforge.sheet-tab.c1')).toBe('inventory')
  })

  it('возврат на уже загруженную вкладку в сеть не ходит', async () => {
    renderPage()
    await screen.findByText('вкладка листа')
    fireEvent.click(screen.getByRole('button', { name: 'Инвентарь' }))
    await screen.findByText('вкладка инвентаря')

    fireEvent.click(screen.getByRole('button', { name: 'Лист' }))
    await screen.findByText('вкладка листа')
    fireEvent.click(screen.getByRole('button', { name: 'Инвентарь' }))
    await screen.findByText('вкладка инвентаря')

    expect(includesOf()).toEqual(['base', 'items'])
  })

  it('серверу называются части открытой вкладки — их он и вернёт в ответе на правку', async () => {
    renderPage()
    await screen.findByText('вкладка листа')

    fireEvent.click(screen.getByRole('button', { name: 'Инвентарь' }))
    await screen.findByText('вкладка инвентаря')

    expect(setActiveSlices).toHaveBeenLastCalledWith(['base', 'items'])
  })

  /**
   * Правка могла задеть и то, чего нет на экране, — например, продажа предмета меняет деньги и
   * инвентарь разом. Части, которых в ответе не было, обязаны перечитаться при открытии вкладки,
   * иначе игрок увидит устаревшие данные.
   */
  it('после правки не пришедшие части перечитываются заново', async () => {
    renderPage()
    await screen.findByText('вкладка листа')
    fireEvent.click(screen.getByRole('button', { name: 'Таланты' }))
    await screen.findByText('вкладка талантов')
    expect(includesOf()).toEqual(['base', 'talents'])

    // Правка опыта на вкладке талантов вернёт только то, что там показано.
    takeFreshSlices.mockReturnValue({ base: { ...base, totalXp: 150 } })
    fireEvent.click(screen.getByRole('button', { name: '100' }))
    fireEvent.change(screen.getByDisplayValue('100'), { target: { value: '150' } })
    fireEvent.keyDown(screen.getByDisplayValue('150'), { key: 'Enter' })

    await waitFor(() => expect(updateCharacter).toHaveBeenCalled())
    // Таланты в ответе не приехали — значит, их надо взять заново.
    await waitFor(() => expect(includesOf()).toEqual(['base', 'talents', 'talents']))
  })
})
