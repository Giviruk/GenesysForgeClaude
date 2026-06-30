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

  it('custom archetype/career methods use the expected endpoints', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'archetype-id' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'career-id' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'archetype-id' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'career-id' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))

    const archetypePayload = {
      system: 'genesysCore' as const,
      name: 'Custom Species',
      nameRu: '',
      brawn: 2,
      agility: 2,
      intellect: 2,
      cunning: 2,
      willpower: 2,
      presence: 2,
      woundBase: 10,
      strainBase: 10,
      startingXp: 100,
      description: '',
      abilityNameRu: '',
      abilityDescription: '',
    }
    const careerPayload = {
      system: 'genesysCore' as const,
      name: 'Custom Career',
      nameRu: '',
      description: '',
      careerSkillNames: ['Athletics'],
      startingMoneyFixed: 0,
      startingMoneyDice: '',
    }

    await api.createCustomArchetype(archetypePayload)
    await api.createCustomCareer(careerPayload)
    await api.updateCustomArchetype('a1', archetypePayload)
    await api.updateCustomCareer('c1', careerPayload)
    await api.deleteCustomArchetype('a1')
    await api.deleteCustomCareer('c1')

    expect(fetchMock.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
      ['/api/custom/archetypes', 'POST'],
      ['/api/custom/careers', 'POST'],
      ['/api/custom/archetypes/a1', 'PUT'],
      ['/api/custom/careers/c1', 'PUT'],
      ['/api/custom/archetypes/a1', 'DELETE'],
      ['/api/custom/careers/c1', 'DELETE'],
    ])
  })

  it('homebrew pack methods use the expected endpoints', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ format: 'genesysforge.homebrew-pack.v1' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'p1' }), { status: 201 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'raw', path: '/homebrew/import/raw' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'p2' }), { status: 201 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))

    await api.homebrewPacks()
    await api.exportHomebrewPack('p1')
    await api.importHomebrewPack({ format: 'genesysforge.homebrew-pack.v1', name: 'Pack', system: 'genesysCore' })
    await api.shareHomebrewPack('p1')
    await api.importSharedHomebrewPack('raw token')
    await api.setHomebrewPackDefault('p1', false)
    await api.setCharacterHomebrewPack('c1', 'p1', true)
    await api.setCampaignHomebrewPack('g1', 'p1', true)

    expect(fetchMock.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
      ['/api/homebrew-packs/', 'GET'],
      ['/api/homebrew-packs/p1/export', 'GET'],
      ['/api/homebrew-packs/import', 'POST'],
      ['/api/homebrew-packs/p1/share', 'POST'],
      ['/api/homebrew-packs/shared/raw%20token/import', 'POST'],
      ['/api/homebrew-packs/p1/default', 'PUT'],
      ['/api/characters/c1/homebrew-packs/p1', 'PUT'],
      ['/api/campaigns/g1/homebrew-packs/p1', 'PUT'],
    ])
  })
})
