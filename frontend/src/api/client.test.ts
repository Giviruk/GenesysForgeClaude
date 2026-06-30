import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, setUnauthorizedHandler, tokenStorage } from './client'

describe('api client — обработка 401', () => {
  afterEach(() => {
    vi.restoreAllMocks()
    setUnauthorizedHandler(null)
    tokenStorage.clear()
  })

  it('401 с токеном чистит токен и вызывает обработчик (истёкшая сессия)', async () => {
    tokenStorage.set('expired-token')
    const handler = vi.fn()
    setUnauthorizedHandler(handler)
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ message: 'нет' }), { status: 401 }))

    await expect(api.characters()).rejects.toMatchObject({ status: 401 })
    expect(handler).toHaveBeenCalledOnce()
    expect(tokenStorage.get()).toBeNull()
  })

  it('401 без токена (неверный логин) не трогает сессию', async () => {
    const handler = vi.fn()
    setUnauthorizedHandler(handler)
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ message: 'Неверный e-mail или пароль.' }), { status: 401 }))

    await expect(api.login('a@b.c', 'wrong')).rejects.toMatchObject({ status: 401 })
    expect(handler).not.toHaveBeenCalled()
  })

  it('успешный ответ возвращает данные', async () => {
    tokenStorage.set('ok-token')
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify([{ id: '1', name: 'X' }]), { status: 200 }))
    const list = await api.characters()
    expect(list).toHaveLength(1)
  })

  it('spells() обращается к /api/spells/<System> с токеном', async () => {
    tokenStorage.set('ok-token')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify([]), { status: 200 }))

    await api.spells('realmsOfTerrinoth')

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/spells/RealmsOfTerrinoth')
    expect((init?.headers as Record<string, string>).Authorization).toBe('Bearer ok-token')
  })

  it('character clone/share methods use the expected endpoints', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'copy-id' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'raw', path: '/share/raw' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'character-id' }), { status: 200 }))

    await api.duplicateCharacter('c1')
    await api.shareCharacter('c1')
    await api.revokeCharacterShares('c1')
    await api.sharedSheet('raw_token')

    expect(fetchMock.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
      ['/api/characters/c1/duplicate', 'POST'],
      ['/api/characters/c1/share', 'POST'],
      ['/api/characters/c1/share', 'DELETE'],
      ['/api/share/raw_token', 'GET'],
    ])
  })
})
