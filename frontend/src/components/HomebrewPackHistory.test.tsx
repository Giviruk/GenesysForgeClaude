import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { CampaignPackHistoryPanel, CustomEditedDate, HomebrewPackHistory, PackChangeStatus } from './HomebrewPackHistory'
import type { CampaignHomebrewPack, CustomContentChange } from '../api/types'

const changes = vi.fn()
const packs = vi.fn()
vi.mock('../api/client', () => ({ api: {
  homebrewPackChanges: (...args: unknown[]) => changes(...args),
  campaignHomebrewPacks: (...args: unknown[]) => packs(...args),
} }))
const pack: CampaignHomebrewPack = {
  updatePolicy: 'auto', status: 'active', exclusionCount: 0, id: 'pack', name: 'Набор игрока', system: 'genesysCore', entryCount: 1,
  isMine: false, isEnabled: true, ownerName: 'Игрок', ownerIsMember: true,
  connectedAt: '2026-10-01T12:00:00Z', lastChangedAt: '2026-10-07T12:00:00Z', changedAfterConnection: true }
const edit: CustomContentChange = { id: 'edit', homebrewPackId: 'pack', definitionType: 'talent', definitionId: 'talent',
  definitionName: 'Мой талант', userId: 'player', userName: 'Игрок', action: 'updated', createdAt: '2026-10-07T12:00:00Z',
  changes: [{ field: 'tier', from: '1', to: '2' }, { field: 'futureField', from: null, to: '"Новое значение"' }] }
beforeEach(() => { vi.clearAllMocks(); changes.mockResolvedValue([edit]); packs.mockResolvedValue([pack]) })

describe('история кастомного контента', () => {
  it('показывает автора, действие, дату и diff с метками и неизвестными полями', async () => {
    const close = vi.fn()
    render(<HomebrewPackHistory packId="pack" name={pack.name} campaignId="campaign" onClose={close} />)
    const dialog = screen.getByRole('dialog', { name: 'История набора' })
    expect(await within(dialog).findByText('Изменено · Мой талант')).toBeTruthy()
    expect(screen.getByText('Автор: Игрок')).toBeTruthy()
    expect(screen.getByText('Было')).toBeTruthy()
    expect(screen.getByText('Стало')).toBeTruthy()
    expect(screen.getByText('Тир').closest('tr')?.textContent).toBe('Тир12')
    expect(screen.getByText('futureField').closest('tr')?.textContent).toContain('Новое значение')
    expect(dialog.querySelector('time')?.getAttribute('dateTime')).toBe(edit.createdAt)
    expect(changes).toHaveBeenCalledWith('pack', 'campaign', 200)
    fireEvent.click(screen.getByRole('button', { name: 'Закрыть' }))
    expect(close).toHaveBeenCalledOnce()
  })

  it('сохраняет названия созданной и удалённой записи без diff', async () => {
    changes.mockResolvedValue([
      { ...edit, id: 'deleted', action: 'deleted', changes: [] },
      { ...edit, id: 'created', action: 'created', changes: [] },
    ])
    render(<HomebrewPackHistory packId="pack" name={pack.name} onClose={vi.fn()} />)
    expect(await screen.findByText('Удалено · Мой талант')).toBeTruthy()
    expect(screen.getByText('Создано · Мой талант')).toBeTruthy()
    expect(screen.queryByRole('table')).toBeNull()
  })

  it('объясняет отсутствие журнала у импорта и прежних записей', async () => {
    changes.mockResolvedValue([])
    render(<HomebrewPackHistory packId="pack" name={pack.name} onClose={vi.fn()} />)
    expect(await screen.findByText(/Старые записи и импорт не имеют истории/)).toBeTruthy()
    expect(changes).toHaveBeenCalledWith('pack', undefined, 200)
  })

  it('показывает отказ API и не выдаёт его за пустой журнал', async () => {
    changes.mockRejectedValue(new Error('История набора недоступна.'))
    render(<HomebrewPackHistory packId="pack" name={pack.name} onClose={vi.fn()} />)
    expect((await screen.findByRole('alert')).textContent).toContain('недоступна')
    expect(screen.queryByText(/Изменений пока нет/)).toBeNull()
  })

  it('открывает участнику историю отключённого набора прежнего участника', async () => {
    packs.mockResolvedValue([{ ...pack, ownerIsMember: false, isEnabled: false }])
    render(<CampaignPackHistoryPanel campaignId="campaign" />)
    expect(await screen.findByText(/игрок покинул кампанию/)).toBeTruthy()
    expect(screen.getByText('отключён')).toBeTruthy()
    expect(screen.getByText('изменён после подключения')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'История' }))
    await waitFor(() => expect(changes).toHaveBeenCalledWith('pack', 'campaign', 200))
    expect(screen.queryByRole('button', { name: 'Включить набор' })).toBeNull()
  })

  it('обновляет дату после сигнала кампании', async () => {
    packs.mockResolvedValue([{ ...pack, lastChangedAt: null, changedAfterConnection: false }])
    const view = render(<CampaignPackHistoryPanel campaignId="campaign" refreshSignal={0} />)
    await screen.findByText(pack.name)
    expect(screen.queryByText(/изменено/)).toBeNull()
    packs.mockResolvedValue([pack])
    view.rerender(<CampaignPackHistoryPanel campaignId="campaign" refreshSignal={1} />)
    expect(await screen.findByText('изменён после подключения')).toBeTruthy()
  })

  it('не придумывает дату и не помечает правки до подключения', () => {
    render(<><CustomEditedDate /><PackChangeStatus pack={{ ...pack, lastChangedAt: '2026-09-01T12:00:00Z', changedAfterConnection: false }} /></>)
    expect(screen.queryByText('Дата правки неизвестна')).toBeNull()
    expect(screen.getByText('изменено 01.09.2026')).toBeTruthy()
    expect(screen.queryByText('изменён после подключения')).toBeNull()
  })

  it.each([true, false])('скрывает метку у набора мастера по серверному флагу (isMine=%s)', (isMine) => {
    render(<PackChangeStatus pack={{ ...pack, isMine, ownerName: 'Мастер', changedAfterConnection: false }} />)
    expect(screen.getByText('изменено 07.10.2026')).toBeTruthy()
    expect(screen.queryByText('изменён после подключения')).toBeNull()
  })
})
