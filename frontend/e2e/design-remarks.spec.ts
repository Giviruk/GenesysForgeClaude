import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import type { GameSession, NpcDetail, Reference } from '../src/api/types'

let token: string
let characterId: string
let campaignId: string
let encounterId: string
let npcId: string
let weaponName: string
let weaponNameEn: string
let craftSpendCount: number
const email = `v5-followup-${Date.now()}@example.test`
const password = 'VisualFollowup!2026'

async function api<T>(request: APIRequestContext, path: string, data?: unknown, method = data === undefined ? 'GET' : 'POST'): Promise<T> {
  const response = await request.fetch(path, { method, headers: { Authorization: `Bearer ${token}` }, data })
  expect(response.ok(), `${path}: ${response.status()} ${await response.text()}`).toBe(true)
  return response.status() === 204 ? undefined as T : await response.json() as T
}
async function fits(page: Page) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
}
async function rect(locator: Locator) {
  await expect(locator).toBeVisible()
  await locator.evaluate(() => document.fonts.ready.then(() => undefined))
  return (await locator.boundingBox())!
}
async function sheetTab(page: Page, key: string, label: string) {
  await expect(page.locator('.sheet-title-row')).toBeVisible()
  const select = page.locator('#sheet-tab-select')
  if (await select.isVisible()) await select.selectOption(key)
  else await page.locator('.main-tabs').getByRole('button', { name: label, exact: true }).click()
}
test.beforeAll(async ({ request }) => {
  const account = await request.post('/api/auth/register', { data: { email, password, displayName: 'Визуальная приёмка v5' } })
  expect(account.ok()).toBe(true)
  token = (await account.json()).token
  const reference = await api<Reference>(request, '/api/reference/realmsOfTerrinoth')
  const weapon = reference.items.find(i => i.kind === 'weapon' && i.purchasable)!
  weaponName = weapon.nameRu || weapon.name
  weaponNameEn = weapon.name
  characterId = (await api<{ id: string }>(request, '/api/characters/', {
    name: 'Элира — хранительница перевала', system: 'realmsOfTerrinoth', archetypeId: reference.archetypes[0].id, careerId: reference.careers[0].id,
    freeCareerSkillNames: [], archetypeSkillChoices: [], careerGearChoices: [],
  })).id
  await api(request, `/api/characters/${characterId}`, { totalXp: 800, money: 10000, desire: 'Защитить путешественников', fear: 'Потерять друзей', strength: 'Терпение', flaw: 'Упрямство', background: 'Собственный тестовый персонаж для приёмки интерфейса.' }, 'PATCH')
  const cargo = reference.items.find(item => item.kind === 'gear' && item.encumbrance === 1 && item.purchasable)!
  await api(request, `/api/characters/${characterId}/items`, { itemDefId: cargo.id, quantity: 9, state: 'carried', free: true })
  await api(request, `/api/characters/${characterId}/mounts`, { mountDefId: reference.mounts.find(mount => mount.transportKind === 'mount')!.id, free: true, name: 'Северный ветер' })
  const pack = await api<{ id: string }>(request, '/api/homebrew-packs', { name: 'Тест (RealmsOfTerrinoth)', description: 'Собственный тестовый набор.', system: 'realmsOfTerrinoth' })
  await api(request, '/api/custom/skills', { name: 'Контроль пути', system: 'realmsOfTerrinoth', characteristic: 'cunning', kind: 'general', packIds: [pack.id] })
  await api(request, `/api/characters/${characterId}/items`, { itemDefId: weapon.id, quantity: 1, state: 'equipped', free: true })
  const inventory = await api<{ items: Array<{ id: string }> }>(request, `/api/characters/${characterId}`)
  await api(request, `/api/characters/${characterId}/items/${inventory.items[0].id}/damage-state`, { state: 'minor' }, 'PUT')
  await api(request, `/api/characters/${characterId}/notes`, { title: 'Заметка экспедиции', body: 'Собственный текст проверки.' })
  for (const nameRu of ['След от царапины', 'Ушиб плеча']) await api(request, `/api/characters/${characterId}/critical-injuries`, { nameRu, severity: 'Лёгкая', notes: 'Тест интерфейса.' })
  await api(request, `/api/characters/${characterId}/heroic-ability`, { heroicAbilityId: reference.heroicAbilities.find(h => h.code === 'rot.heroic.influential')!.id }, 'PUT')
  await api(request, `/api/characters/${characterId}/heroic-identity`, { customName: 'Голос перевала', originMode: 'standard', originPrimary: 'destiny' }, 'PUT')
  await api(request, `/api/characters/${characterId}/crafting`, { itemDefId: weapon.id, kind: 'item', costPercent: 100 })
  craftSpendCount = (await api<{ spends: unknown[] }>(request, `/api/characters/${characterId}/crafting/preview`, { itemDefId: weapon.id, kind: 'item' })).spends.length
  expect(craftSpendCount).toBeGreaterThan(0)
  campaignId = (await api<{ id: string }>(request, '/api/campaigns/', { name: 'Экспедиция к перевалу', description: 'Собственные тестовые данные визуальной приёмки.' })).id
  await api(request, `/api/campaigns/${campaignId}/characters`, { characterId })
  await api(request, `/api/campaigns/${campaignId}/chronicle/chapters`, { title: 'Дорога к перевалу', content: '# Первый день\n\nСобственный текст для проверки редактора и превью.' })
  const npc = await api<NpcDetail>(request, '/api/npcs/', {
    name: 'Страж северных ворот', system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute', visibility: 'private', campaignId: null,
    description: 'Собственный тестовый NPC.', source: 'UI test', brawn: 3, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2,
    woundThreshold: 14, strainThreshold: null, soak: 3, meleeDefense: 1, rangedDefense: 0, silhouette: 1, tactics: '',
    skills: [{ name: reference.skills.find(s => s.kind === 'combat')!.name, ranks: 2 }], attacks: [{ name: 'Тестовая атака', skillName: reference.skills.find(s => s.kind === 'combat')!.name, damage: '+3', critical: '2', rangeBand: 'Вплотную', notes: '', qualities: [], sourceWeapon: '' }],
    talents: [reference.talents[0].nameRu || reference.talents[0].name], equipment: [], abilities: [{ name: 'Тестовая способность', description: 'Собственное описание.' }], tags: [],
  })
  npcId = npc.id
  const minion = await api<NpcDetail>(request, '/api/npcs/', { ...npc, id: undefined, name: 'Караул на мосту', kind: 'minion', strainThreshold: null, skills: [] })
  await api(request, `/api/campaigns/${campaignId}/session/`, { name: 'Караул у старого моста', description: 'Собственная тестовая сцена.', playerStoryPoints: 3, gmStoryPoints: 2 })
  await api(request, `/api/campaigns/${campaignId}/session/participants`, { characterId })
  const session = await api<GameSession>(request, `/api/campaigns/${campaignId}/session/participants`, { npcId: minion.id, count: 3, participantType: 'minionGroup' })
  for (const p of session.participants) await api(request, `/api/campaigns/${campaignId}/session/slots`, { slotType: p.characterId ? 'player' : 'npc', assignedParticipantId: p.id })
  await api(request, `/api/campaigns/${campaignId}/rolls/`, { actorName: 'Элира', label: 'Проверка наблюдательности', poolJson: '{}', resultJson: JSON.stringify({ success: 2, advantage: 1, triumph: 0, failure: 0, threat: 0, despair: 0 }), summary: 'Успех', isSecret: false })
  encounterId = (await api<{ id: string }>(request, `/api/campaigns/${campaignId}/encounters/`, {
    name: 'Караул у старого моста', system: 'realmsOfTerrinoth', type: 'combat', threatLevel: 'hard', isVisibleToPlayers: true,
    gmDescription: 'Собственные заметки мастера.', playerDescription: 'Мост закрыт караулом.', playerGoals: 'Договориться о проходе.', npcGoals: 'Охранять мост.', location: 'Северный мост', environment: 'Горный перевал', complications: 'Тестовое осложнение.', rewards: 'Тестовая награда.', tags: ['приёмка'],
  })).id
  await api(request, `/api/encounters/${encounterId}/participants`, { npcId, participantType: 'npc', count: 1 })
})


for (const language of ['ru', 'en']) {
  test(`design remarks: responsive sections in ${language}`, async ({ page }, testInfo) => {
    test.setTimeout(360_000)
    await page.addInitScript(({ token, language }) => {
      localStorage.setItem('genesysforge.token', token)
      localStorage.setItem('genesysforge.lang', language)
      localStorage.setItem('genesysforge.analytics-consent', 'denied')
    }, { token, language })
    const baseline = process.env.DESIGN_BASELINE === '1'
    const sections = [['sheet', 'Лист', 'Sheet'], ['talents', 'Таланты', 'Talents'],
      ['inventory', 'Инвентарь', 'Inventory'], ['magic', 'Магия', 'Magic'],
      ['heroic', 'Героика', 'Heroic'], ['workshop', 'Мастерская', 'Workshop'],
      ['transport', 'Транспорт', 'Transport'], ['bio', 'Образ и заметки', 'Bio and notes'], ['history', 'История', 'History']]
    for (const width of baseline ? [1240, 375] : [1240, 768, 375]) {
      await page.setViewportSize({ width, height: 900 })
      await page.goto(`/characters/${characterId}`)
      if (!baseline && width === 375) {
        await sheetTab(page, 'sheet', language === 'ru' ? 'Лист' : 'Sheet')
        await expect(page.locator('.mobile-menu-trigger span')).toHaveCSS('position', 'static')
        await page.evaluate(() => window.scrollTo(0, 600))
        await expect.poll(async () => Math.abs((await page.locator('.mobile-topbar').boundingBox())!.y)).toBeLessThanOrEqual(1)
        await page.evaluate(() => window.scrollTo(0, 0))
      }
      for (const [key, ru, en] of sections) {
        if (baseline && !['sheet', 'talents', 'inventory'].includes(key)) continue
        await sheetTab(page, key, language === 'ru' ? ru : en)
        if (!baseline) await fits(page)
        if (!baseline && key === 'talents') {
          const parts = page.locator('.talents-v5-layout>section')
          const pyramid = await rect(parts.nth(0)), owned = await rect(parts.nth(1))
          expect(owned.y - pyramid.y - pyramid.height).toBeLessThan(40)
          const filters = page.locator('.talent-filter-chips')
          expect(await filters.evaluateAll(nodes => nodes.some(node => node.scrollWidth > node.clientWidth + 1))).toBe(false)
          if (width === 1240) {
            expect((await rect(filters.first().getByRole('button').first())).height).toBeLessThanOrEqual(28)
            expect((await rect(page.locator('.talent-list'))).height).toBeLessThan(900)
          }
        }
        if (!baseline && key === 'sheet') {
          await expect(page.locator('.rd-defense-card .rd-bar.overloaded>i')).toHaveCSS('background-color', 'rgb(226, 83, 42)')
          if (width === 375) await expect(page.locator('label[for=sheet-tab-select]')).toHaveCSS('clip-path', 'inset(50%)')
        }
        if (!baseline && key === 'inventory') {
          expect((await page.locator('.inv-card-title>.muted').first().innerText()).trim()).not.toMatch(/^·/)
          await expect(page.locator('.state-carried.active').first()).toHaveCSS('background-color', 'rgb(91, 91, 102)')
          await expect(page.getByRole('combobox', { name: language === 'ru' ? 'Состояние предмета' : 'Item condition' }).first()).toBeVisible()
        }
        if (!baseline && key === 'transport') {
          await expect(page.locator('.mount-name-input')).toHaveCSS('font-size', '21px')
          const tiles = await page.locator('.mount-stat-tiles').evaluate(el => getComputedStyle(el).gridTemplateColumns.split(' ').length)
          expect(tiles).toBe(2)
        }
        await page.screenshot({ path: testInfo.outputPath(`${width}-${key}.png`), fullPage: false })
      }
      await page.goto(`/campaigns/${campaignId}/encounters/${encounterId}`)
      await expect(page.locator('.enc-detail')).toBeVisible()
      if (!baseline) await fits(page)
      if (!baseline) {
        expect(await page.locator('.enc-participants .table-wrap').evaluate(el => el.scrollWidth - el.clientWidth)).toBeLessThanOrEqual(1)
        await expect(page.locator('.participant-table .badge')).toHaveClass(/tone-orange/)
      }
      await page.screenshot({ path: testInfo.outputPath(`${width}-encounter.png`), fullPage: false })
      if (!baseline) {
        for (const view of ['', '/chronicle', '/table', '/content']) {
          await page.goto(`/campaigns/${campaignId}${view}`)
          await expect(page.locator('.campaign-tabs')).toBeVisible()
          await fits(page)
          await page.screenshot({ path: testInfo.outputPath(`${width}-campaign-${view.slice(1) || 'overview'}.png`), fullPage: false })
        }
        for (const route of [`/npcs/${npcId}`, '/npcs/new?mode=draft', '/npcs/new?mode=manual']) {
          await page.goto(route)
          await expect(page.locator('.bt-card,.bt-draft-form,.bt-manual')).toBeVisible()
          await fits(page)
          await page.screenshot({ path: testInfo.outputPath(`${width}-${route.includes('new') ? route.split('=')[1] : 'npc'}.png`), fullPage: false })
        }
        await page.goto('/library')
        await expect(page.locator('.rd-entry-description')).toContainText('Контроль пути')
        await expect(page.locator('.library-system-switch .active')).toContainText('Realms of Terrinoth')
        await fits(page)
        await page.screenshot({ path: testInfo.outputPath(`${width}-library.png`), fullPage: false })
        await page.goto('/shop')
        await expect(page.locator('.shop-product-row').first()).toBeVisible()
        await fits(page)
        await page.locator('.shop-product-row').first().locator('.shop-product-name strong').click()
        await expect(page.locator('.shop-product-modal')).toBeVisible()
        await fits(page)
        await page.screenshot({ path: testInfo.outputPath(`${width}-shop.png`), fullPage: false })
      }
    }
    // Keep all fixture operations and browser-only helper imports exercised.
    expect(weaponName).toBeTruthy(); expect(weaponNameEn).toBeTruthy(); expect(craftSpendCount).toBeGreaterThan(0)
    if (baseline) expect((await rect(page.locator('.enc-detail'))).width).toBeGreaterThan(0)
    else {
      await page.goto(`/characters/${characterId}`)
      await sheetTab(page, 'sheet', language === 'ru' ? 'Лист' : 'Sheet')
      await page.setViewportSize({ width: 1240, height: 900 })
      await page.emulateMedia({ media: 'print' })
      await expect(page.locator('.rd-skill-actions').first()).toBeHidden()
      await expect(page.locator('.rd-skill-name').first()).toBeVisible()
    }
  })
}
