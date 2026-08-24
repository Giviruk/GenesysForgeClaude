import { render, screen, waitFor, fireEvent, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BaseSheet, Reference, SheetSliceName, SheetSlices } from '../api/types'

const sheetSlices = vi.fn<(id: string, include: SheetSliceName[]) => Promise<SheetSlices>>()
const updateCharacter = vi.fn<(id: string, patch: unknown) => Promise<void>>().mockResolvedValue(undefined)
const completeCreation = vi.fn<(id: string) => Promise<void>>().mockResolvedValue(undefined)
const reference = vi.fn(() => Promise.resolve({ items: [], talents: [], qualities: [] } as unknown as Reference))
const takeFreshSlices = vi.fn<(id: string) => SheetSlices | null>(() => null)
const setActiveSlices = vi.fn()

vi.mock('../api/client', () => ({
  api: {
    sheetSlices: (id: string, include: SheetSliceName[]) => sheetSlices(id, include),
    reference: () => reference(),
    updateCharacter: (id: string, patch: unknown) => updateCharacter(id, patch),
    completeCreation: (id: string) => completeCreation(id),
    sheet: () => Promise.resolve(null),
  },
  takeFreshSlices: (id: string) => takeFreshSlices(id),
  setActiveSlices: (s: SheetSliceName[]) => setActiveSlices(s),
}))

// Вкладки подменены: проверяется, что и когда грузит страница, а не что рисуют вкладки.
vi.mock('../components/SheetTab', () => ({ SheetTab: () => <div>вкладка листа</div> }))
vi.mock('../components/TalentsTab', () => ({ TalentsTab: () => <div>вкладка талантов</div> }))
vi.mock('../components/InventoryTab', () => ({ InventoryTab: () => <div>вкладка инвентаря</div> }))
vi.mock('../components/MagicTab', () => ({ MagicTab: () => <div>вкладка магии</div> }))

const { SheetPage } = await import('./SheetPage')

const base = {
  id: 'c1', name: 'Гарет', system: 'realmsOfTerrinoth', totalXp: 100, spentXp: 0, availableXp: 100,
  isCreationPhase: true,
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

function renderPage(onOpenPrint = () => {}) {
  return render(
    <SheetPage characterId="c1" printing={false}
      onOpenPrint={onOpenPrint} onClosePrint={() => {}} onBack={() => {}} />)
}

/**
 * Лист играющего персонажа весит около 116 КБ, и две трети из них — инвентарь. Страница обязана
 * брать только то, что нужно открытой вкладке, иначе разделение на части не даёт ничего.
 */
describe('SheetPage — части листа грузятся по надобности', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    completeCreation.mockReset().mockResolvedValue(undefined)
    localStorage.removeItem('genesysforge.sheet-tab.c1')
    takeFreshSlices.mockReturnValue(null)
    sheetSlices.mockImplementation((_id, include) => Promise.resolve(serve(include)))
  })

  it('при открытии берётся только базовая часть — без инвентаря и талантов', async () => {
    renderPage()

    await screen.findByText('вкладка листа')
    expect(includesOf()).toEqual(['base'])
  })

  it('позволяет изменить имя персонажа из заголовка листа', async () => {
    renderPage()
    await screen.findByText('вкладка листа')

    fireEvent.click(screen.getByRole('button', { name: 'Гарет' }))
    const input = screen.getByRole('textbox', { name: 'Имя персонажа' })
    fireEvent.change(input, { target: { value: 'Гарет Серый' } })
    fireEvent.keyDown(input, { key: 'Enter' })

    await waitFor(() => expect(updateCharacter).toHaveBeenCalledWith('c1', { name: 'Гарет Серый' }))
    expect(await screen.findByText('Имя персонажа обновлено.')).toBeTruthy()
  })

  it('показывает понятную причину, если завершение создания отклонено', async () => {
    completeCreation.mockRejectedValueOnce({
      status: 400,
      reasonCode: 'heroic.identity.incomplete',
      message: 'Укажите личное название и происхождение героической способности до завершения создания.',
    })
    renderPage()
    await screen.findByText('вкладка листа')

    fireEvent.click(screen.getByRole('button', { name: 'Завершить создание' }))

    await waitFor(() => expect(completeCreation).toHaveBeenCalledWith('c1'))
    expect((await screen.findByRole('alert')).textContent).toMatch(/личное название и происхождение/)
  })

  it('показывает основные вкладки первыми в заданном порядке', async () => {
    renderPage()
    await screen.findByText('вкладка листа')

    const tabs = within(document.querySelector('.main-tabs') as HTMLElement)
      .getAllByRole('button').map(button => button.textContent)
    expect(tabs.slice(0, 5)).toEqual(['Лист', 'Инвентарь', 'Таланты', 'Магия', 'Заметки'])
    expect(tabs.slice(5)).toEqual(['Героика', 'Улучшения', 'Транспорт', 'Ремесло', 'Образ', 'История'])
    expect(document.querySelectorAll('.sheet-secondary-tab')).toHaveLength(6)
  })

  it('скрывает редкие действия под кнопкой с тремя точками', async () => {
    const onOpenPrint = vi.fn()
    renderPage(onOpenPrint)
    await screen.findByText('вкладка листа')

    expect(screen.queryByRole('menu')).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: 'Дополнительные действия' }))

    const menu = screen.getByRole('menu')
    expect(within(menu).getAllByRole('menuitem').map(item => item.textContent)).toEqual([
      'Печать', 'Клонировать', 'Ссылка', 'Отозвать ссылки', 'Экспорт JSON',
    ])
    fireEvent.click(within(menu).getByRole('menuitem', { name: 'Печать' }))
    expect(onOpenPrint).toHaveBeenCalledOnce()
    expect(screen.queryByRole('menu')).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Дополнительные действия' }))
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(screen.queryByRole('menu')).toBeNull()
  })

  it('ставит действия создания справа после блока опыта', async () => {
    renderPage()
    await screen.findByText('вкладка листа')

    const controls = document.querySelector('.sheet-head-controls')
    expect(controls?.children[0].classList.contains('xp-block')).toBe(true)
    expect(controls?.children[1].classList.contains('sheet-action-buttons')).toBe(true)
    expect(document.querySelector('.sheet-title-row .sheet-action-buttons')).toBeNull()
  })

  it('переход на вкладку догружает только недостающее', async () => {
    renderPage()
    await screen.findByText('вкладка листа')

    fireEvent.click(screen.getByRole('button', { name: 'Инвентарь' }))

    await screen.findByText('вкладка инвентаря')
    // Базовая часть уже есть — заново её не просят.
    expect(includesOf()).toEqual(['base', 'items'])
  })

  it('для вкладки магии догружает таланты вместе с инвентарём', async () => {
    renderPage()
    await screen.findByText('вкладка листа')

    fireEvent.click(screen.getByRole('button', { name: 'Магия' }))

    await screen.findByText('вкладка магии')
    expect(includesOf()).toEqual(['base', 'items,talents'])
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
