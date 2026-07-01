import { expect, test, type APIRequestContext, type Page } from '@playwright/test'

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

  test('campaign, player join, NPC duplicate, encounter and Game Table smoke', async ({ page, request }) => {
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
    await expect(page.getByRole('button', { name: 'Game Table' })).toBeVisible()
    await expect(page.getByText(encounterName)).toBeVisible()
    await expect(page.getByText(characterName).first()).toBeVisible()
  })

  test('magic builder plus character export/import smoke', async ({ page, request }) => {
    const user = await register(request, 'magic-import')
    const characterName = unique('E2E Exported')
    const { id } = await createCharacter(request, user.token, characterName, 'realmsOfTerrinoth')

    const exported = await apiGet<CharacterExport>(request, user.token, `/api/characters/${id}/export`)
    expect(exported.format).toBe('genesysforge.character.v1')
    const imported = await apiPost<ImportResult>(request, user.token, '/api/characters/import', exported)
    await openAs(page, user.token, `/characters/${imported.characterId}`)
    await expect(page.getByText(characterName).first()).toBeVisible()

    await page.goto('/magic')
    await expect(page.locator('.magic-builder')).toBeVisible()
    await expect(page.locator('.difficulty-badge.big')).toBeVisible()
    const optionalEffect = page.locator('.effect-row input').first()
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
    await page.getByRole('button', { name: /Отправить/i }).click()
    await expect(page.locator('.notice')).toBeVisible()
  })
})
