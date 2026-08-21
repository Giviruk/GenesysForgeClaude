import { afterEach, describe, expect, it, vi } from 'vitest'
import { API_TIMING_EVENT, api, invalidateReference, setActiveSlices, setUnauthorizedHandler, takeFreshSlices, tokenStorage } from './client'

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

  it('сохраняет машинный код причины из ответа API', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ message: 'Выберите параметр.', reasonCode: 'heroic.parameter.incomplete' }), { status: 400 }))

    await expect(api.characters()).rejects.toMatchObject({
      status: 400, reasonCode: 'heroic.parameter.incomplete', message: 'Выберите параметр.',
    })
  })

  it('публикует длительность полного API-действия', async () => {
    const timings: unknown[] = []
    const listener = (event: Event) => timings.push((event as CustomEvent).detail)
    window.addEventListener(API_TIMING_EVENT, listener)
    const response = new Response(JSON.stringify([{ id: '1', name: 'X' }]), { status: 200 })
    vi.spyOn(response.headers, 'get').mockImplementation(name => {
      if (name.toLowerCase() === 'content-length') return '42'
      if (name.toLowerCase() === 'server-timing') return 'app;dur=3.1'
      return null
    })
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(response)

    await api.characters()

    window.removeEventListener(API_TIMING_EVENT, listener)
    expect(timings).toHaveLength(1)
    expect(timings[0]).toMatchObject({
      method: 'GET', url: '/api/characters/', ok: true, status: 200,
      responseBytes: 42, serverTiming: 'app;dur=3.1',
    })
    expect(timings[0]).toEqual(expect.objectContaining({ durationMs: expect.any(Number) }))
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

  it('chronicle methods use campaign-scoped versioned endpoints', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'ch1' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ imageUrl: 'https://storage.test/image.png' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'ch1' }), { status: 200 }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(JSON.stringify([]), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ id: 'ch1' }), { status: 200 }))

    await api.campaignChronicle('c1')
    await api.createChronicleChapter('c1', { title: 'Пролог', content: '# Пролог' })
    const image = new Blob(['png'])
    await api.uploadChronicleImage('c1', image)
    await api.updateChronicleChapter('c1', 'ch1', { title: 'Пролог', content: 'Текст', expectedVersion: 1 })
    await api.deleteChronicleChapter('c1', 'ch1')
    await api.chronicleHistory('c1', 'ch1')
    await api.restoreChronicleRevision('c1', 'ch1', 'r1')

    expect(fetchMock.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
      ['/api/campaigns/c1/chronicle', 'GET'],
      ['/api/campaigns/c1/chronicle/chapters', 'POST'],
      ['/api/campaigns/c1/chronicle/images', 'POST'],
      ['/api/campaigns/c1/chronicle/chapters/ch1', 'PUT'],
      ['/api/campaigns/c1/chronicle/chapters/ch1', 'DELETE'],
      ['/api/campaigns/c1/chronicle/chapters/ch1/history', 'GET'],
      ['/api/campaigns/c1/chronicle/chapters/ch1/restore/r1', 'POST'],
    ])
    expect(fetchMock.mock.calls[2][1]?.body).toBe(image)
    expect(fetchMock.mock.calls[3][1]?.body).toBe(JSON.stringify({ title: 'Пролог', content: 'Текст', expectedVersion: 1 }))
  })

  it('loads campaign member audit through the campaign-scoped endpoint', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify([]), { status: 200 }))

    await api.campaignMemberAudit('campaign-1', 'character-1', 25)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/campaigns/campaign-1/characters/character-1/audit?take=25',
      expect.objectContaining({ method: 'GET' }),
    )
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

    await api.createCustomArchetype('campaign-1', archetypePayload)
    await api.createCustomCareer('campaign-1', careerPayload)
    await api.updateCustomArchetype('campaign-1', 'a1', archetypePayload)
    await api.updateCustomCareer('campaign-1', 'c1', careerPayload)
    await api.deleteCustomArchetype('campaign-1', 'a1')
    await api.deleteCustomCareer('campaign-1', 'c1')

    expect(fetchMock.mock.calls.map(([url, init]) => [url, init?.method])).toEqual([
      ['/api/campaigns/campaign-1/custom/archetypes', 'POST'],
      ['/api/campaigns/campaign-1/custom/careers', 'POST'],
      ['/api/campaigns/campaign-1/custom/archetypes/a1', 'PUT'],
      ['/api/campaigns/campaign-1/custom/careers/c1', 'PUT'],
      ['/api/campaigns/campaign-1/custom/archetypes/a1', 'DELETE'],
      ['/api/campaigns/campaign-1/custom/careers/c1', 'DELETE'],
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
    await api.deleteCustomItem('campaign-1', 'i9')
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
      Promise.resolve(String(url).includes('/custom/')
        ? new Response(JSON.stringify({ message: 'нет' }), { status: 400 })
        : refBody()))

    await api.reference('realmsOfTerrinoth')
    await expect(api.deleteCustomItem('campaign-1', 'x')).rejects.toMatchObject({ status: 400 })
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
 * Части листа, приехавшие вместе с ответом на правку. Убирают второй запрос: интерфейс после каждой
 * правки всё равно перечитывает лист, а на проде это отдельные 250–500 мс. Просим при этом ровно
 * то, что сейчас на экране: инвентарь — две трети веса листа, и на вкладке заметок он не нужен.
 */
describe('api client — части листа в ответе на правку', () => {
  const slicesBody = (money = 5) => new Response(
    JSON.stringify({ base: { id: 'c1', derived: {}, characteristics: {}, money } }), { status: 200 })

  afterEach(() => {
    vi.restoreAllMocks()
    takeFreshSlices('c1')
    setActiveSlices(['base'])
    invalidateReference()
    tokenStorage.clear()
  })

  it('правка персонажа просит части заголовком, GET — нет', async () => {
    tokenStorage.set('t')
    setActiveSlices(['base', 'items'])
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(slicesBody()))

    await api.sheet('c1')
    await api.updateMount('c1', 'm1', { isActive: true })

    const headerOf = (i: number) =>
      (fetchMock.mock.calls[i][1]?.headers as Record<string, string>)['X-Return-Slices']
    expect(headerOf(0)).toBeUndefined()
    expect(headerOf(1)).toBe('base,items')
  })

  /** Правка денег и опыта уходит в сам `/api/characters/{id}` — без хвоста, но части ей тоже нужны. */
  it('правка без хвоста в адресе тоже просит части', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(slicesBody()))

    await api.updateCharacter('c1', { money: 10 })

    const headers = fetchMock.mock.calls[0][1]?.headers as Record<string, string>
    expect(headers['X-Return-Slices']).toBe('base')
    expect(takeFreshSlices('c1')).toMatchObject({ base: { money: 5 } })
  })

  it('вернувшиеся части забираются один раз', async () => {
    tokenStorage.set('t')
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(slicesBody()))

    await api.updateMount('c1', 'm1', { isActive: true })

    expect(takeFreshSlices('c1')).toMatchObject({ base: { id: 'c1' } })
    // Второй раз — уже ничего: устаревшие данные не должны осесть в интерфейсе.
    expect(takeFreshSlices('c1')).toBeNull()
  })

  it('части другого персонажа не отдаются', async () => {
    tokenStorage.set('t')
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(slicesBody()))

    await api.updateMount('c1', 'm1', { isActive: true })

    expect(takeFreshSlices('c2')).toBeNull()
  })

  it('после двух правок подряд остаётся результат последней', async () => {
    tokenStorage.set('t')
    let n = 0
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(slicesBody(++n)))

    await api.updateMount('c1', 'm1', { isActive: true })
    await api.updateMount('c1', 'm1', { isActive: false })

    expect(takeFreshSlices('c1')).toMatchObject({ base: { money: 2 } })
  })

  /** `duplicate` создаёт другого персонажа и отвечает своим `{ id }` — частями это не считается. */
  it('ответ со своим телом частями не считается', async () => {
    tokenStorage.set('t')
    vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
      Promise.resolve(new Response(JSON.stringify({ id: 'copy-1' }), { status: 201 })))

    await api.duplicateCharacter('c1')

    expect(takeFreshSlices('c1')).toBeNull()
  })

  /**
   * Покупка создаёт запись внутри персонажа, поэтому части приезжают и с ней — раньше за ними шёл
   * второй запрос. Идентификатор созданного при этом не теряется.
   */
  it('покупка возвращает части вместе с идентификатором созданного', async () => {
    tokenStorage.set('t')
    setActiveSlices(['base', 'items'])
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(new Response(
      JSON.stringify({ base: { id: 'c1' }, items: [{ id: 'item-1' }], createdId: 'item-1' }),
      { status: 200 })))

    const created = await api.addItem('c1', 'def-1', 1, 'carried', { free: true })

    expect(created.createdId).toBe('item-1')
    expect(takeFreshSlices('c1')).toMatchObject({ createdId: 'item-1' })
  })

  it('ответ 204 ничего не оставляет', async () => {
    tokenStorage.set('t')
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(new Response(null, { status: 204 })))

    await api.updateMount('c1', 'm1', { isActive: true })

    expect(takeFreshSlices('c1')).toBeNull()
  })

  it('чтение частей уходит на свой маршрут со списком', async () => {
    tokenStorage.set('t')
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockImplementation(() => Promise.resolve(slicesBody()))

    await api.sheetSlices('c1', ['base', 'items'])

    expect(fetchMock.mock.calls[0][0]).toBe('/api/characters/c1/slices?include=base,items')
  })
})
