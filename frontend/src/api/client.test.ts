import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, invalidateReference, setUnauthorizedHandler, takeFreshSheet, tokenStorage } from './client'

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

/**
 * Справочник — каталог игры на 560 КБ, который лист раньше перезапрашивал после каждого клика.
 * Кэш обязан жить ровно до момента, когда каталог реально мог измениться, — не дольше.
 */
describe('api client — кэш справочника', () => {
  const refBody = () => new Response(JSON.stringify({ items: [] }), { status: 200 })

  afterEach(() => {
    vi.restoreAllMocks()
    invalidateReference()
    tokenStorage.clear()
  })

  it('второй запрос того же справочника идёт из кэша, а не в сеть', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(refBody()))

    await api.reference('realmsOfTerrinoth')
    await api.reference('realmsOfTerrinoth')

    expect(fetchMock).toHaveBeenCalledOnce()
  })

  it('разные системы и контексты кэшируются по отдельности', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(refBody()))

    await api.reference('realmsOfTerrinoth')
    await api.reference('genesysCore')
    await api.reference('realmsOfTerrinoth', { characterId: 'c1' })

    expect(fetchMock).toHaveBeenCalledTimes(3)
  })

  it('одновременные запросы склеиваются в один', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(refBody()))

    await Promise.all([api.reference('realmsOfTerrinoth'), api.reference('realmsOfTerrinoth')])

    expect(fetchMock).toHaveBeenCalledOnce()
  })

  it('правка персонажа кэш не сбрасывает: каталог от неё не меняется', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(refBody()))

    await api.reference('realmsOfTerrinoth')
    await api.moveCargo('c1', 'i1', { mountId: 'm1' })
    await api.reference('realmsOfTerrinoth')

    const referenceCalls = fetchMock.mock.calls.filter(([url]) => String(url).includes('/api/reference/'))
    expect(referenceCalls).toHaveLength(1)
  })

  it('правка кастомного предмета сбрасывает кэш', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(refBody()))

    await api.reference('realmsOfTerrinoth')
    await api.deleteCustomItem('i9')
    await api.reference('realmsOfTerrinoth')

    const referenceCalls = fetchMock.mock.calls.filter(([url]) => String(url).includes('/api/reference/'))
    expect(referenceCalls).toHaveLength(2)
  })

  /** Маршрут висит на персонаже, но меняет состав справочника — проверка по префиксу его пропустит. */
  it('подключение homebrew-пака к персонажу сбрасывает кэш', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(refBody()))

    await api.reference('realmsOfTerrinoth')
    await api.setCharacterHomebrewPack('c1', 'p1', true)
    await api.reference('realmsOfTerrinoth')

    const referenceCalls = fetchMock.mock.calls.filter(([url]) => String(url).includes('/api/reference/'))
    expect(referenceCalls).toHaveLength(2)
  })

  it('неудачная правка кэш не сбрасывает', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation((url) =>
      Promise.resolve(String(url).includes('/api/custom/')
        ? new Response(JSON.stringify({ message: 'нет' }), { status: 400 })
        : refBody()))

    await api.reference('realmsOfTerrinoth')
    await expect(api.deleteCustomItem('x')).rejects.toMatchObject({ status: 400 })
    await api.reference('realmsOfTerrinoth')

    const referenceCalls = fetchMock.mock.calls.filter(([url]) => String(url).includes('/api/reference/'))
    expect(referenceCalls).toHaveLength(1)
  })

  it('провалившийся запрос справочника не залипает в кэше', async () => {
    tokenStorage.set('t')
    let first = true
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => {
      if (first) { first = false; return Promise.resolve(new Response('{}', { status: 500 })) }
      return Promise.resolve(refBody())
    })

    await expect(api.reference('realmsOfTerrinoth')).rejects.toMatchObject({ status: 500 })
    await api.reference('realmsOfTerrinoth')

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('конец сессии чистит кэш: у следующего пользователя свой кастомный контент', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(refBody()))

    await api.reference('realmsOfTerrinoth')
    tokenStorage.clear()
    await api.reference('realmsOfTerrinoth')

    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})

/**
 * Лист, приехавший вместе с ответом на правку. Убирает второй запрос: интерфейс после каждой правки
 * всё равно перечитывает лист, а на проде это отдельные 250–500 мс.
 */
describe('api client — лист в ответе на правку', () => {
  const sheetBody = (id = 'c1') => new Response(
    JSON.stringify({ id, derived: {}, characteristics: {}, money: 5 }), { status: 200 })

  afterEach(() => {
    vi.restoreAllMocks()
    takeFreshSheet('c1')
    invalidateReference()
    tokenStorage.clear()
  })

  it('правка персонажа просит лист заголовком, GET — нет', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(sheetBody()))

    await api.sheet('c1')
    await api.updateMount('c1', 'm1', { isActive: true })

    const headerOf = (i: number) =>
      (fetchMock.mock.calls[i][1]?.headers as Record<string, string>)['X-Return-Sheet']
    expect(headerOf(0)).toBeUndefined()
    expect(headerOf(1)).toBe('1')
  })

  it('вернувшийся лист забирается один раз', async () => {
    tokenStorage.set('t')
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(sheetBody()))

    await api.updateMount('c1', 'm1', { isActive: true })

    expect(takeFreshSheet('c1')).toMatchObject({ id: 'c1' })
    // Второй раз — уже ничего: устаревшие данные не должны осесть в интерфейсе.
    expect(takeFreshSheet('c1')).toBeNull()
  })

  it('лист другого персонажа не отдаётся', async () => {
    tokenStorage.set('t')
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(sheetBody('c1')))

    await api.updateMount('c1', 'm1', { isActive: true })

    expect(takeFreshSheet('c2')).toBeNull()
  })

  it('после двух правок подряд остаётся результат последней', async () => {
    tokenStorage.set('t')
    let n = 0
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(new Response(
      JSON.stringify({ id: 'c1', derived: {}, characteristics: {}, money: ++n }), { status: 200 })))

    await api.updateMount('c1', 'm1', { isActive: true })
    await api.updateMount('c1', 'm1', { isActive: false })

    expect(takeFreshSheet('c1')).toMatchObject({ money: 2 })
  })

  it('ответ со своим телом листом не считается', async () => {
    tokenStorage.set('t')
    // Покупка отвечает { id }, а не листом — подменять её нельзя.
    vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
      Promise.resolve(new Response(JSON.stringify({ id: 'mount-1' }), { status: 201 })))

    await api.buyMount('c1', 'def-1', { free: true })

    expect(takeFreshSheet('c1')).toBeNull()
  })

  it('ответ 204 ничего не оставляет', async () => {
    tokenStorage.set('t')
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(new Response(null, { status: 204 })))

    await api.updateMount('c1', 'm1', { isActive: true })

    expect(takeFreshSheet('c1')).toBeNull()
  })
})
