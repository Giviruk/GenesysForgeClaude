import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import type { CampaignHomebrewPack, CustomContentChange } from '../src/api/types'

type GameSystem = 'genesysCore' | 'realmsOfTerrinoth'

interface AuthResponse {
  token: string
  email: string
}

interface Reference {
  archetypes: Array<{ id: string }>
  careers: Array<{ id: string }>
  skills: Array<{ id: string; name: string }>
  talents: Array<{ id: string; tier: number; grantsCharacteristic: boolean }>
  items: Array<{ id: string; name: string; nameRu: string; kind: string; soakBonus: number }>
}

interface CharacterSheet {
  id: string
  name: string
  derived: { soak: number }
  skills: Array<{ skillDefId: string; ranks: number; isCareer: boolean }>
  talents: Array<{ talentDefId: string }>
  items: Array<{ id: string; name: string; itemDefId: string }>
}

interface CampaignDetail {
  id: string
  name: string
  joinCode: string | null
}

interface NpcDetail {
  id: string
  name: string
}

interface EncounterDetail {
  id: string
  name: string
}

interface CharacterExport {
  format: string
  exportedAt: string
  character: unknown
}

interface ImportResult {
  characterId: string
}

const tokenKey = 'genesysforge.token'

test('GEN-CONTENT-01: original pack live edit, scoped history and campaign UI', async ({ page, request }, testInfo) => {
  const gm = await register(request, 'content-history-gm')
  const author = await register(request, 'content-history-author')
  const member = await register(request, 'content-history-member')
  const campaign = await apiPost<CampaignDetail>(request, gm.token, '/api/campaigns/', { name: unique('Live content'), description: '' })
  const talentRequest = { system: 'genesysCore', name: unique('Player talent'), tier: 1, isRanked: false,
    activation: 'Passive', description: 'User-authored effect.', woundBonus: 0, strainBonus: 0, soakBonus: 0,
    meleeDefenseBonus: 0, rangedDefenseBonus: 0, category: 'general' }
  const pack = await apiPost<{ id: string; name: string }>(request, author.token, '/api/homebrew-packs/',
    { system: 'genesysCore', name: unique('Author pack') })
  const talent = await apiPost<{ id: string }>(request, author.token, '/api/custom/talents', { ...talentRequest, packIds: [pack.id] })
  const share = await apiPost<{ token: string }>(request, author.token, `/api/homebrew-packs/${pack.id}/share`)
  const connectUrl = `/api/campaigns/${campaign.id}/homebrew-packs/shared/${share.token}/import`
  const nonMember = await request.post(connectUrl, { headers: authHeaders(gm.token) })
  expect(nonMember.status()).toBe(400)
  expect(await nonMember.json()).toMatchObject({ reasonCode: 'homebrew.owner_not_member' })
  for (const user of [author, member])
    await apiPost(request, user.token, '/api/campaigns/join', { joinCode: campaign.joinCode })
  expect(await apiPost(request, gm.token, connectUrl)).toMatchObject({ id: pack.id })
  const ownPack = await apiPost<{ id: string }>(request, gm.token, '/api/homebrew-packs/',
    { system: 'genesysCore', name: unique('GM pack') })
  await readJson(await request.put(`/api/campaigns/${campaign.id}/homebrew-packs/${ownPack.id}`,
    { headers: authHeaders(gm.token), data: { isEnabled: true } }), 'GM pack connection')
  const gmTalent = await apiPost<{ id: string }>(request, gm.token, `/api/campaigns/${campaign.id}/custom/talents`,
    { ...talentRequest, name: unique('Gm talent'), packIds: [ownPack.id] })
  const gmPack = (await apiGet<CampaignHomebrewPack[]>(request, gm.token, `/api/campaigns/${campaign.id}/homebrew-packs/`))
    .find(x => x.isMine)!
  // Creation and connection can fall within the same JavaScript millisecond.
  expect(new Date(gmPack.lastChangedAt!).getTime()).toBeGreaterThanOrEqual(new Date(gmPack.connectedAt).getTime())
  expect(gmPack.changedAfterConnection).toBe(false)
  await readJson(await request.put(`/api/campaigns/${campaign.id}/custom/talents/${gmTalent.id}`,
    { headers: authHeaders(gm.token), data: { ...talentRequest, name: unique('Edited Gm talent'), tier: 2 } }), 'GM live edit')
  const changed = { ...talentRequest, tier: 2 }
  await readJson(await request.put(`/api/custom/talents/${talent.id}`, { headers: authHeaders(author.token), data: changed }), 'author live edit')
  const historyUrl = `/api/homebrew-packs/${pack.id}/changes?campaignId=${campaign.id}`
  for (const user of [gm, member]) {
    const history = await apiGet<CustomContentChange[]>(request, user.token, historyUrl)
    expect(history).toHaveLength(2)
    expect(history[0]).toMatchObject({ definitionId: talent.id, action: 'updated', changes: [{ field: 'tier', from: '1', to: '2' }] })
    const reference = await apiGet<{ customLastEditedAt: Record<string, string>; talents: Array<{ id: string; tier: number }> }>(
      request, user.token, `/api/reference/GenesysCore?campaignId=${campaign.id}`)
    expect(reference.customLastEditedAt[talent.id]).toBe(history[0].createdAt)
    expect(reference.talents.find(x => x.id === talent.id)?.tier).toBe(2)
    const campaignPacks = await apiGet<CampaignHomebrewPack[]>(request, user.token, `/api/campaigns/${campaign.id}/homebrew-packs/`)
    const metadata = campaignPacks.find(x => x.id === pack.id)!
    expect(metadata.ownerIsMember).toBe(true)
    expect(new Date(metadata.lastChangedAt!).getTime()).toBeGreaterThan(new Date(metadata.connectedAt).getTime())
    expect(metadata.changedAfterConnection).toBe(true)
    const editedGmPack = campaignPacks.find(x => x.id === gmPack.id)!
    expect(new Date(editedGmPack.lastChangedAt!).getTime()).toBeGreaterThan(new Date(editedGmPack.connectedAt).getTime())
    expect(editedGmPack.changedAfterConnection).toBe(false)
  }
  const otherCampaign = await apiPost<CampaignDetail>(request, gm.token, '/api/campaigns/', { name: unique('Unconnected content'), description: '' })
  expect((await request.get(`/api/homebrew-packs/${pack.id}/changes?campaignId=${otherCampaign.id}`,
    { headers: authHeaders(gm.token) })).status()).toBe(400)
  expect((await request.get(historyUrl)).status()).toBe(401)
  await readJson(await request.put(`/api/custom/talents/${talent.id}`, { headers: authHeaders(author.token), data: changed }), 'unchanged save')
  expect(await apiGet(request, member.token, historyUrl)).toHaveLength(2)

  await openAs(page, member.token, `/campaigns/${campaign.id}`)
  await expect(page.getByRole('button', { name: 'Настройки', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Наборы кампании', exact: true })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Контент', exact: true })).toHaveCount(0)

  await openAs(page, gm.token, `/campaigns/${campaign.id}/content?section=packs`)
  await expect(page.getByText('изменён после подключения')).toHaveCount(1)
  const playerPackRow = page.locator('.rd-pack-grid .rd-section-card').filter({ hasText: 'E2E content-history-author' })
  await playerPackRow.getByRole('button', { name: 'История', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: 'История набора' })
  await expect(dialog.getByText(`Изменено · ${talentRequest.name}`)).toBeVisible()
  await expect(dialog.getByRole('row', { name: 'Тир 1 2' })).toBeVisible()
  await testInfo.attach('custom-content-history', {
    body: await page.screenshot({ path: testInfo.outputPath('custom-content-history.png'), fullPage: true }), contentType: 'image/png',
  })
})

test('PublicSafe infrastructure: release identity, OpenAPI JSON and manifest MIME', async ({ request }) => {
  test.skip(process.env.E2E_CONTENT_MODE !== 'PublicSafe', 'Only applies to the public artifact')
  const health = await request.get('/api/v1/health')
  expect(health.ok()).toBe(true)
  expect(await health.json()).toMatchObject({ contentMode: 'PublicSafe', status: 'ok' })
  expect((await health.json()).version).toContain('+')
  const schema = await request.get('/openapi/v1.json')
  expect(schema.headers()['content-type']).toContain('application/json')
  expect((await schema.json()).openapi).toMatch(/^3\./)
  const manifest = await request.get('/manifest.webmanifest')
  expect(manifest.headers()['content-type']).toContain('application/manifest+json')
  expect((await manifest.json()).start_url).toBe('/')
})

test('PublicSafe PWA: clears legacy personal reference cache and refuses offline API data', async ({ page, request }) => {
  test.skip(process.env.E2E_CONTENT_MODE !== 'PublicSafe', 'Only applies to the public artifact')
  const user = await register(request, 'pwa-privacy')
  await openAs(page, user.token, '/')
  await page.evaluate(async () => { await navigator.serviceWorker.ready })
  await expect.poll(() => page.evaluate(() => navigator.serviceWorker.controller !== null)).toBe(true)
  await page.evaluate(async () => {
    const cache = await caches.open('genesysforge-reference-v1')
    await cache.put('/api/reference/RealmsOfTerrinoth', new Response(JSON.stringify({ privateMarker: 'old-user-secret' })))
  })
  // A fresh authenticated API call must clean caches even during a worker upgrade.
  await page.reload()
  await expect.poll(() => page.evaluate(async () => (await caches.keys())
    .filter(key => key.startsWith('genesysforge-reference-')))).toEqual([])
  await page.evaluate(async token => {
    const response = await fetch('/api/v1/reference/RealmsOfTerrinoth', { headers: { Authorization: `Bearer ${token}` } })
    if (!response.ok) throw new Error(`reference: ${response.status}`)
  }, user.token)
  await page.context().setOffline(true)
  const offlineData = await page.evaluate(async token => {
    try {
      await fetch('/api/v1/reference/RealmsOfTerrinoth', { headers: { Authorization: `Bearer ${token}` } })
      return true
    } catch { return false }
  }, user.token)
  expect(offlineData).toBe(false)
  await page.context().setOffline(false)
})

test('PublicSafe: book references, no prose, purchased sheet and publisher links', async ({ page, request }) => {
  test.skip(process.env.E2E_CONTENT_MODE !== 'PublicSafe', 'Only applies to the public artifact')
  const user = await register(request, 'public-safe')
  const response = await request.get('/api/v1/reference/RealmsOfTerrinoth', { headers: authHeaders(user.token) })
  expect(response.headers()['cache-control']).toContain('no-store')
  const reference = await readJson<{
    talents: Array<{ id: string; name: string; tier: number; description: string; descriptionEn: string; safeDescription: string; source: string }>
    heroicAbilities: Array<{ id: string; description: string; descriptionEn: string; safeDescription: string; source: string;
      upgrades: Array<{ description: string; descriptionEn: string; source: string }> }>
  }>(response, 'public reference')
  for (const talent of reference.talents) {
    expect([talent.description, talent.descriptionEn, talent.safeDescription]).toEqual(['', '', ''])
    expect(talent.source).toMatch(/, с\. \d+/)
  }
  for (const heroic of reference.heroicAbilities) {
    expect([heroic.description, heroic.descriptionEn, heroic.safeDescription]).toEqual(['', '', ''])
    for (const upgrade of heroic.upgrades) {
      expect([upgrade.description, upgrade.descriptionEn]).toEqual(['', ''])
      expect(upgrade.source).toBe(heroic.source)
    }
  }
  const { id } = await createCharacter(request, user.token, unique('Public hero'), 'realmsOfTerrinoth')
  const talent = reference.talents.find(t => t.name === 'Toughened')!
  await apiPost(request, user.token, `/api/characters/${id}/talents/buy`, { talentDefId: talent.id })
  const sheet = await apiGet<{ talents: Array<{ talentDefId: string; description: string; descriptionEn: string; source: string }> }>(
    request, user.token, `/api/characters/${id}`)
  expect(sheet.talents.find(t => t.talentDefId === talent.id)).toMatchObject({ description: '', descriptionEn: '', source: talent.source })
  await openAs(page, user.token, `/characters/${id}`)
  await page.getByRole('button', { name: 'Таланты', exact: true }).click()
  // Core cites the Russian edition's pages, so the link shows that edition's title and publisher page.
  const label = talent.source.replace('Genesys Core Rulebook (RU translation),', 'Genesys. Основная книга правил,')
  await expect(page.getByRole('link', { name: label }).first()).toBeVisible()
  expect(await page.getByRole('link', { name: label }).first().getAttribute('href'))
    .toBe('https://hobbyworld.ru/genesys-osnovnaja-kniga-pravil')
})

function unique(prefix: string): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

function authHeaders(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` }
}

async function readJson<T>(response: Awaited<ReturnType<APIRequestContext['get']>>, label: string): Promise<T> {
  const body = await response.text()
  expect(response.ok(), `${label} failed: ${response.status()} ${body}`).toBeTruthy()
  return body ? JSON.parse(body) as T : undefined as T
}

async function apiGet<T>(request: APIRequestContext, token: string, url: string): Promise<T> {
  return readJson<T>(await request.get(url, { headers: authHeaders(token) }), `GET ${url}`)
}

async function apiPost<T>(request: APIRequestContext, token: string, url: string, data?: unknown): Promise<T> {
  return readJson<T>(await request.post(url, { headers: authHeaders(token), data }), `POST ${url}`)
}

async function register(request: APIRequestContext, label: string): Promise<AuthResponse> {
  const email = `${unique(label)}@example.test`
  return readJson<AuthResponse>(await request.post('/api/auth/register', {
    data: {
      email,
      password: 'Passw0rd!',
      displayName: `E2E ${label}`,
    },
  }), 'POST /api/auth/register')
}

async function createCharacter(
  request: APIRequestContext,
  token: string,
  name = unique('E2E Hero'),
  system: GameSystem = 'genesysCore',
): Promise<{ id: string; reference: Reference }> {
  const reference = await apiGet<Reference>(
    request,
    token,
    `/api/reference/${system === 'genesysCore' ? 'GenesysCore' : 'RealmsOfTerrinoth'}`,
  )
  const created = await apiPost<{ id: string }>(request, token, '/api/characters/', {
    name,
    system,
    archetypeId: reference.archetypes[0].id,
    careerId: reference.careers[0].id,
    freeCareerSkillNames: [],
    archetypeSkillChoices: [],
    careerGearChoices: [],
  })
  return { id: created.id, reference }
}

async function openAs(page: Page, token: string, path: string): Promise<void> {
  await page.addInitScript(([key, value]) => {
    window.localStorage.setItem(key, value)
  }, [tokenKey, token])
  await page.goto(path)
}

test.describe('U-29 smoke E2E', () => {
  test('register data path: create character, buy/refund skill and talent, equip item', async ({ page, request }) => {
    const user = await register(request, 'character')
    const characterName = unique('E2E Character')
    const { id, reference } = await createCharacter(request, user.token, characterName)

    await openAs(page, user.token, `/characters/${id}`)
    await expect(page.getByText(characterName)).toBeVisible()

    const before = await apiGet<CharacterSheet>(request, user.token, `/api/characters/${id}`)
    const skill = before.skills.find(s => !s.isCareer && s.ranks === 0) ?? before.skills.find(s => s.ranks === 0)
    expect(skill, 'Expected a skill that can be bought').toBeTruthy()
    await apiPost<void>(request, user.token, `/api/characters/${id}/skills/${skill!.skillDefId}/buy-rank`)
    let sheet = await apiGet<CharacterSheet>(request, user.token, `/api/characters/${id}`)
    expect(sheet.skills.find(s => s.skillDefId === skill!.skillDefId)?.ranks).toBe(1)
    await apiPost<void>(request, user.token, `/api/characters/${id}/skills/${skill!.skillDefId}/refund-rank`)
    sheet = await apiGet<CharacterSheet>(request, user.token, `/api/characters/${id}`)
    expect(sheet.skills.find(s => s.skillDefId === skill!.skillDefId)?.ranks).toBe(0)

    const talent = reference.talents.find(t => t.tier === 1 && !t.grantsCharacteristic)
    expect(talent, 'Expected a tier-1 talent without characteristic choice').toBeTruthy()
    await apiPost<void>(request, user.token, `/api/characters/${id}/talents/buy`, { talentDefId: talent!.id })
    sheet = await apiGet<CharacterSheet>(request, user.token, `/api/characters/${id}`)
    expect(sheet.talents.some(t => t.talentDefId === talent!.id)).toBeTruthy()
    await apiPost<void>(request, user.token, `/api/characters/${id}/talents/refund`, { talentDefId: talent!.id })
    sheet = await apiGet<CharacterSheet>(request, user.token, `/api/characters/${id}`)
    expect(sheet.talents.some(t => t.talentDefId === talent!.id)).toBeFalsy()

    const armor = reference.items.find(i => i.kind === 'armor' && i.soakBonus > 0)
    expect(armor, 'Expected armor with soak bonus').toBeTruthy()
    const soakBefore = sheet.derived.soak
    await apiPost<{ id: string }>(request, user.token, `/api/characters/${id}/items`, {
      itemDefId: armor!.id,
      quantity: 1,
      state: 'equipped',
      cost: 0,
    })
    sheet = await apiGet<CharacterSheet>(request, user.token, `/api/characters/${id}`)
    expect(sheet.derived.soak).toBeGreaterThan(soakBefore)

    await page.reload()
    await page.getByRole('button', { name: 'Инвентарь' }).click()
    await expect(page.getByText(armor!.nameRu || armor!.name).first()).toBeVisible()
  })

  test('campaign, player join, NPC duplicate, encounter and game table smoke', async ({ page, request }) => {
    const gm = await register(request, 'gm')
    const player = await register(request, 'player')
    const characterName = unique('E2E Player')
    const { id: characterId } = await createCharacter(request, player.token, characterName)

    const campaignName = unique('E2E Campaign')
    const campaign = await apiPost<CampaignDetail>(request, gm.token, '/api/campaigns/', {
      name: campaignName,
      description: 'E2E campaign',
    })
    expect(campaign.joinCode).toBeTruthy()
    await apiPost<CampaignDetail>(request, player.token, '/api/campaigns/join', {
      joinCode: campaign.joinCode,
      characterId,
    })

    const npcName = unique('E2E Goblin')
    const npc = await apiPost<NpcDetail>(request, gm.token, '/api/npcs/', {
      name: npcName,
      system: 'genesysCore',
      kind: 'minion',
      role: 'skirmisher',
      description: 'E2E generated NPC',
      source: 'E2E',
      brawn: 2,
      agility: 3,
      intellect: 2,
      cunning: 2,
      willpower: 2,
      presence: 2,
      woundThreshold: 5,
      strainThreshold: null,
      soak: 2,
      meleeDefense: 0,
      rangedDefense: 0,
      silhouette: 1,
      tactics: '',
      visibility: 'private',
      campaignId: null,
      skills: [{ name: 'Ranged', ranks: 1 }],
      abilities: [],
      attacks: [],
      talents: [],
      equipment: [],
      tags: ['e2e'],
    })
    const duplicated = await apiPost<NpcDetail>(request, gm.token, `/api/npcs/${npc.id}/duplicate`)

    const encounterName = unique('E2E Encounter')
    const encounter = await apiPost<EncounterDetail>(request, gm.token, `/api/campaigns/${campaign.id}/encounters/`, {
      name: encounterName,
      system: 'genesysCore',
      type: 'combat',
      threatLevel: 'easy',
      gmDescription: 'GM notes',
      playerDescription: 'Player description',
      playerGoals: 'Survive',
      npcGoals: 'Ambush',
      location: 'Road',
      environment: 'Forest',
      complications: '',
      rewards: '',
      isVisibleToPlayers: true,
      tags: ['e2e'],
    })
    await apiPost<EncounterDetail>(request, gm.token, `/api/encounters/${encounter.id}/participants`, {
      npcId: duplicated.id,
      participantType: 'minionGroup',
      initiativeSide: 'npc',
      quantity: 2,
      notes: '',
    })
    await apiPost<void>(request, gm.token, `/api/encounters/${encounter.id}/participants/characters`, {
      characterIds: [characterId],
    })
    await apiPost<void>(request, gm.token, `/api/encounters/${encounter.id}/send-to-table`, { mode: 'replace' })

    await openAs(page, gm.token, `/campaigns/${campaign.id}/table`)
    await expect(page.getByRole('button', { name: 'Игровой стол' })).toBeVisible()
    await expect(page.getByText(encounterName)).toBeVisible()
    await expect(page.getByText(characterName).first()).toBeVisible()
  })

  test('magic builder plus character export/import smoke', async ({ page, request }) => {
    const user = await register(request, 'magic-import')
    const characterName = unique('E2E Exported')
    const { id } = await createCharacter(request, user.token, characterName, 'realmsOfTerrinoth')

    const exported = await apiGet<CharacterExport>(request, user.token, `/api/characters/${id}/export`)
    // v8 больше не содержит устаревшее поле стартового бюджета.
    expect(exported.format).toBe('genesysforge.character.v8')
    const imported = await apiPost<ImportResult>(request, user.token, '/api/characters/import', exported)
    await openAs(page, user.token, `/characters/${imported.characterId}`)
    await expect(page.getByText(characterName).first()).toBeVisible()

    await page.goto('/magic')
    await expect(page.locator('.magic-builder')).toBeVisible()
    await expect(page.locator('.difficulty-badge.big')).toBeVisible()
    const optionalEffect = page.locator('.magic-effect-row input:not(:disabled)').first()
    if (await optionalEffect.count()) {
      await optionalEffect.check()
    }
    await page.getByRole('button', { name: /Печать|Print/i }).first().click()
    await expect(page.locator('.print-overlay')).toBeVisible()
  })

  test('password reset request screen smoke', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: /e-mail/i }).click()
    await page.getByRole('button', { name: /Забыли/i }).click()
    await page.locator('input[type="email"]').fill(`${unique('reset')}@example.test`)
    // Регистрации и сбросы делят лимит AuthSensitive с одного IP; для E2E его поднимает
    // AUTH_SENSITIVE_PERMIT_LIMIT (ci.yml), так что 429 здесь — настоящая ошибка окружения.
    const pending = page.waitForResponse(response =>
      response.url().endsWith('/auth/password-reset/request') && response.request().method() === 'POST')
    await page.getByRole('button', { name: /Отправить/i }).click()
    expect((await pending).status()).toBe(204)
    await expect(page.locator('.notice')).toBeVisible()
  })
})

// Английская локаль: браузер en-US → интерфейс на английском; переключатель в футере
// возвращает русский и переживает перезагрузку (выбор хранится в localStorage).
test.describe('i18n smoke E2E', () => {
  test.use({ locale: 'en-US' })

  test('english UI by browser locale and switch back to Russian', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('button', { name: 'Sign in with e-mail' })).toBeVisible()
    await expect(page.getByText('Character sheets for Genesys Core and Realms of Terrinoth')).toBeVisible()

    await page.getByRole('button', { name: 'Русский' }).click()
    await expect(page.getByRole('button', { name: 'Войти по e-mail' })).toBeVisible()

    await page.reload()
    await expect(page.getByRole('button', { name: 'Войти по e-mail' })).toBeVisible()
  })
})
