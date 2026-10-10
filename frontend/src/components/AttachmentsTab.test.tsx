import { render, screen, waitFor, fireEvent, within } from '@testing-library/react'
import { describe, expect, it, vi, beforeEach } from 'vitest'
import type { AttachmentDef, CharacterAttachment, CharacterSheet, Reference, SheetItem } from '../api/types'
import { AttachmentsTab } from './AttachmentsTab'

const installMock = vi.fn()
const detachMock = vi.fn()
const buyMock = vi.fn()
vi.mock('../api/client', () => ({
  api: {
    installAttachment: (...a: unknown[]) => installMock(...a),
    detachAttachment: (...a: unknown[]) => detachMock(...a),
    buyAttachment: (...a: unknown[]) => buyMock(...a),
    removeAttachment: vi.fn(),
  },
}))

const razorDef = {
  id: 'def-razor', code: 'rot.attachment.razor-edge', name: 'Razor Edge', nameRu: 'Бритвенная кромка',
  hardPointCost: 1, price: 1250, rarity: 6, isEnchantment: false, hostKind: 'weapon',
  requiredTraits: 'bladed', requiredAnyTraits: 'none', forbiddenTraits: 'ranged',
  description: '', descriptionEn: '', source: '', effects: [],
} as unknown as AttachmentDef

const runeDef = {
  ...razorDef, id: 'def-rune', code: 'rot.attachment.rune-of-severing', name: 'Rune of Severing',
  nameRu: 'Руна рассечения', price: null, isEnchantment: true, hardPointCost: 1,
} as unknown as AttachmentDef

const spare = (id: string, defId: string, extra: Partial<CharacterAttachment> = {}) => ({
  id, attachmentDefId: defId, name: 'x', nameRu: 'Бритвенная кромка', hardPointCost: 1,
  isEnchantment: false, price: 1250, rarity: 6, hostCharacterItemId: null, note: '', effects: [],
  damageState: 'undamaged', isUsable: true,
  repair: {
    state: 'undamaged', canRepair: false, difficulty: null, hoursMin: 0, hoursMax: 0,
    materialPercent: 0, materialCost: 0, skillName: 'Mechanics', affordable: true,
  },
  ...extra,
} as unknown as CharacterAttachment)

const sword = {
  id: 'item-sword', itemDefId: 'def-sword', name: 'Sword', nameRu: 'Меч', kind: 'weapon',
  state: 'equipped', quantity: 1, hardPoints: 1, usedHardPoints: 0, attachments: [],
  attachmentNotes: [], overCapacity: false, formTraits: 'oneHanded, sword, bladed, hasCuttingEdge',
} as unknown as SheetItem

const mace = {
  ...sword, id: 'item-mace', name: 'Mace', nameRu: 'Булава',
  formTraits: 'oneHanded, bluntOrCrushing',
} as unknown as SheetItem

const sheet = {
  id: 'char-1', system: 'realmsOfTerrinoth', money: 5000, isCreationPhase: false,
  items: [sword, mace], skills: [],
  attachments: [spare('att-1', 'def-razor')],
} as unknown as CharacterSheet

const reference = { attachments: [razorDef] } as unknown as Reference

const renderTab = (value = sheet, definitions = reference) => render(
  <AttachmentsTab sheet={value} reference={definitions} onError={() => {}}
    refresh={() => Promise.resolve()} />)
const installedSheet = () => {
  const attachment = spare('att-1', 'def-razor', { hostCharacterItemId: 'item-sword' })
  return { ...sheet, items: [{ ...sword, usedHardPoints: 1, attachments: [attachment] }, mace],
    attachments: [attachment] } as CharacterSheet
}

describe('Улучшения предметов (SHEET-02)', () => {
  beforeEach(() => {
    installMock.mockReset().mockResolvedValue(undefined)
    detachMock.mockReset().mockResolvedValue(undefined)
    buyMock.mockReset().mockResolvedValue({ id: 'new' })
  })

  it('выбирает свободный носитель и устанавливает улучшение из запаса', async () => {
    renderTab()
    expect(screen.getByRole('button', { name: /Меч/ }).getAttribute('aria-pressed')).toBe('true')
    fireEvent.click(screen.getByRole('button', { name: 'Установить' }))
    await waitFor(() => expect(installMock).toHaveBeenCalledWith('char-1', 'att-1', 'item-sword', undefined))
  })

  it('объясняет несовместимость выбранного носителя', () => {
    renderTab()
    fireEvent.click(screen.getByRole('button', { name: /Булава/ }))
    expect(screen.getByText('Требуется подходящий профиль оружия')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Установить' })).toBeNull()
  })

  it('показывает причину нехватки слотов и уже установленного улучшения', () => {
    const installed = installedSheet()
    renderTab({ ...installed, attachments: [...installed.attachments, spare('att-2', 'def-razor'),
      spare('att-3', 'def-second')],
    }, { attachments: [razorDef, { ...razorDef, id: 'def-second' }] } as Reference)
    fireEvent.click(screen.getByRole('button', { name: /Меч/ }))
    expect(screen.getByText('Уже установлено')).toBeTruthy()
    expect(screen.getByText('Нет свободных слотов')).toBeTruthy()
  })

  it('требует причину для чар без магического ранга', async () => {
    renderTab({ ...sheet, attachments: [spare('att-2', 'def-rune', {
      nameRu: 'Руна рассечения', isEnchantment: true, price: null,
    })] }, { attachments: [runeDef] } as Reference)
    expect(screen.queryByRole('button', { name: 'Установить' })).toBeNull()
    fireEvent.change(screen.getByLabelText(/Причина установки чар/), { target: { value: 'помог чародей' } })
    fireEvent.click(screen.getByRole('button', { name: 'Установить' }))
    await waitFor(() => expect(installMock).toHaveBeenCalledWith('char-1', 'att-2', 'item-sword', 'помог чародей'))
  })

  it('снимает улучшение с выбранным исходом', async () => {
    renderTab(installedSheet())
    fireEvent.click(screen.getByRole('button', { name: 'Снять' }))
    await waitFor(() => expect(detachMock).toHaveBeenCalledWith('char-1', 'att-1', 'returned'))
    fireEvent.click(screen.getByRole('button', { name: /Испорчено при снятии/ }))
    await waitFor(() => expect(detachMock).toHaveBeenCalledWith('char-1', 'att-1', 'destroyed'))
  })

  it('сохраняет интерактивные качества установленного улучшения', () => {
    renderTab(installedSheet(), { attachments: [{ ...razorDef,
      description: 'Собственная тестовая заметка: Pierce 2.',
    }], qualities: [] } as unknown as Reference)
    fireEvent.mouseEnter(screen.getByRole('button', { name: /Проникающее/ }))
    expect(screen.getByRole('tooltip').textContent).toMatch(/игнорирует поглощение/)
  })

  it('фильтрует лавку и выдаёт бесценные руны', async () => {
    renderTab(sheet, { attachments: [razorDef, runeDef] } as Reference)
    fireEvent.click(screen.getByRole('button', { name: /Лавка/ }))
    fireEvent.click(screen.getByRole('button', { name: /Руны/ }))
    const shop = within(document.querySelector('.sheet-catalog-card')!)
    expect(shop.getByText('Руна рассечения')).toBeTruthy()
    expect(shop.getByRole('button', { name: 'Купить' }).hasAttribute('disabled')).toBe(true)
    fireEvent.click(shop.getByRole('button', { name: '+ Выдать' }))
    await waitFor(() => expect(buyMock).toHaveBeenCalledWith('char-1', 'def-rune', { free: true }))
    expect(await screen.findByRole('status')).toHaveProperty('textContent', 'Руна рассечения — в запасе')
  })

  it('не предлагает руны в Core', () => {
    renderTab({ ...sheet, system: 'genesysCore' }, { attachments: [razorDef, runeDef] } as Reference)
    fireEvent.click(screen.getByRole('button', { name: /Лавка/ }))
    expect(screen.queryByRole('button', { name: /Руны/ })).toBeNull()
    expect(screen.queryByText('Руна рассечения')).toBeNull()
  })

  it('блокирует платную покупку без денег, сохраняя выдачу', async () => {
    renderTab({ ...sheet, money: 1 })
    fireEvent.click(screen.getByRole('button', { name: /Лавка/ }))
    expect(screen.getByRole('button', { name: 'Купить' }).hasAttribute('disabled')).toBe(true)
    fireEvent.click(screen.getByRole('button', { name: '+ Без оплаты' }))
    await waitFor(() => expect(buyMock).toHaveBeenCalledWith('char-1', 'def-razor', { free: true }))
  })
})
