import { render, screen, waitFor, fireEvent } from '@testing-library/react'
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
  id: 'char-1', money: 5000, startingPurchaseBudget: 0, isCreationPhase: false,
  items: [sword, mace], skills: [],
  attachments: [spare('att-1', 'def-razor')],
} as unknown as CharacterSheet

const reference = { attachments: [razorDef] } as unknown as Reference

describe('Улучшения предметов (ROT-EQP-ATT-01)', () => {
  beforeEach(() => {
    installMock.mockReset(); installMock.mockResolvedValue(undefined)
    detachMock.mockReset(); detachMock.mockResolvedValue(undefined)
    buyMock.mockReset(); buyMock.mockResolvedValue({ id: 'new' })
  })

  it('ставит улучшение на выбранный предмет по кнопке «Применить»', async () => {
    render(<AttachmentsTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    // Броска нет: правило книги показано подсказкой.
    expect(screen.getByText(/проверки Механики средней сложности/)).toBeTruthy()

    fireEvent.change(screen.getByLabelText('Предмет'), { target: { value: 'item-sword' } })
    fireEvent.change(screen.getByLabelText('Улучшение'), { target: { value: 'att-1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Применить' }))

    await waitFor(() => expect(installMock).toHaveBeenCalledWith(
      'char-1', 'att-1', 'item-sword', undefined))
  })

  it('не предлагает несовместимый предмет', () => {
    render(<AttachmentsTab sheet={sheet} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    // Бритвенная кромка — для клинкового оружия; у булавы такого признака нет.
    fireEvent.change(screen.getByLabelText('Предмет'), { target: { value: 'item-mace' } })
    const options = [...screen.getByLabelText('Улучшение').querySelectorAll('option')]
    expect(options).toHaveLength(1)
    expect((screen.getByRole('button', { name: 'Применить' }) as HTMLButtonElement).disabled).toBe(true)
  })

  it('требует причину для чар без магического навыка', async () => {
    const withRune = {
      ...sheet,
      attachments: [spare('att-2', 'def-rune', { nameRu: 'Руна рассечения', isEnchantment: true, price: null })],
    } as unknown as CharacterSheet
    render(<AttachmentsTab sheet={withRune} reference={{ attachments: [runeDef] } as unknown as Reference}
      onError={() => {}} refresh={() => Promise.resolve()} />)

    fireEvent.change(screen.getByLabelText('Предмет'), { target: { value: 'item-sword' } })
    fireEvent.change(screen.getByLabelText('Улучшение'), { target: { value: 'att-2' } })

    const apply = () => screen.getByRole('button', { name: 'Применить' }) as HTMLButtonElement
    expect(apply().disabled).toBe(true)

    fireEvent.change(screen.getByLabelText(/Причина установки чар/), {
      target: { value: 'помог городской чародей' },
    })
    fireEvent.click(apply())
    await waitFor(() => expect(installMock).toHaveBeenCalledWith(
      'char-1', 'att-2', 'item-sword', 'помог городской чародей'))
  })

  it('фильтрует списки по виду носителя', () => {
    const armorDef = {
      ...razorDef, id: 'def-plating', code: 'rot.attachment.deflective-plating',
      name: 'Deflective Plating', nameRu: 'Отклоняющие пластины', hostKind: 'armor',
      requiredTraits: 'none', forbiddenTraits: 'none',
    } as unknown as AttachmentDef
    const both = {
      ...sheet,
      attachments: [
        spare('att-1', 'def-razor'),
        spare('att-2', 'def-plating', { nameRu: 'Отклоняющие пластины' }),
      ],
    } as unknown as CharacterSheet
    render(<AttachmentsTab sheet={both} reference={{ attachments: [razorDef, armorDef] } as unknown as Reference}
      onError={() => {}} refresh={() => Promise.resolve()} />)

    const reserve = () => document.querySelector('.attach-list')!.textContent ?? ''
    expect(reserve()).toContain('Бритвенная кромка')
    expect(reserve()).toContain('Отклоняющие пластины')

    fireEvent.click(screen.getByRole('button', { name: 'Броня' }))
    expect(reserve()).not.toContain('Бритвенная кромка')
    expect(reserve()).toContain('Отклоняющие пластины')
  })

  it('снимает установленное улучшение', async () => {
    const installed = {
      ...sheet,
      items: [{ ...sword, usedHardPoints: 1, attachments: [spare('att-1', 'def-razor', { hostCharacterItemId: 'item-sword' })] }, mace],
      attachments: [spare('att-1', 'def-razor', { hostCharacterItemId: 'item-sword' })],
    } as unknown as CharacterSheet
    render(<AttachmentsTab sheet={installed} reference={reference} onError={() => {}}
      refresh={() => Promise.resolve()} />)

    fireEvent.click(screen.getByRole('button', { name: 'Снять' }))
    await waitFor(() => expect(detachMock).toHaveBeenCalledWith('char-1', 'att-1', 'returned'))
  })
})
