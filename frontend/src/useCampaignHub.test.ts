import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useCampaignHub } from './useCampaignHub'

const hub = vi.hoisted(() => ({
  start: vi.fn(), invoke: vi.fn(), stop: vi.fn(), on: vi.fn(),
  onreconnecting: vi.fn(), onreconnected: vi.fn(), onclose: vi.fn(),
}))
vi.mock('./api/client', () => ({ tokenStorage: { get: () => 'test-token' } }))
vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: class {
    withUrl() { return this }
    withAutomaticReconnect() { return this }
    build() { return hub }
  },
}))

beforeEach(() => {
  vi.clearAllMocks()
  hub.start.mockResolvedValue(undefined); hub.invoke.mockResolvedValue(undefined); hub.stop.mockResolvedValue(undefined)
})

describe('campaign subscription snapshots', () => {
  it('refreshes all snapshots after subscription and reconnect, using current handlers', async () => {
    const initial = { onCampaignChanged: vi.fn(), onGameTableChanged: vi.fn(), onRollAdded: vi.fn(), onStatus: vi.fn() }
    const current = { onCampaignChanged: vi.fn(), onGameTableChanged: vi.fn(), onRollAdded: vi.fn(), onStatus: vi.fn() }
    const hook = renderHook(({ handlers }) => useCampaignHub('campaign', handlers), { initialProps: { handlers: initial } })
    await waitFor(() => expect(initial.onStatus).toHaveBeenCalledWith('connected'))
    expect(initial.onCampaignChanged).toHaveBeenCalledOnce()
    hook.rerender({ handlers: current })
    let resolve!: () => void
    hub.invoke.mockImplementationOnce(() => new Promise<void>(r => { resolve = r }))
    await act(async () => { hub.onreconnected.mock.calls[0][0]() })
    expect(current.onCampaignChanged).not.toHaveBeenCalled()
    await act(async () => { resolve() })
    expect(hub.invoke).toHaveBeenLastCalledWith('SubscribeCampaign', 'campaign')
    expect(current.onCampaignChanged).toHaveBeenCalledOnce()
    expect(current.onGameTableChanged).toHaveBeenCalledOnce()
    expect(current.onRollAdded).toHaveBeenCalledOnce()
    hook.unmount()
  })

  it('reports a failed resubscription and does not apply a false fresh snapshot', async () => {
    const handlers = { onCampaignChanged: vi.fn(), onStatus: vi.fn() }
    renderHook(() => useCampaignHub('campaign', handlers))
    await waitFor(() => expect(handlers.onCampaignChanged).toHaveBeenCalledOnce())
    hub.invoke.mockRejectedValueOnce(new Error('subscription rejected'))
    await act(async () => { hub.onreconnected.mock.calls[0][0]() })
    expect(handlers.onStatus).toHaveBeenLastCalledWith('disconnected')
    expect(handlers.onCampaignChanged).toHaveBeenCalledOnce()
  })
})
