import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import { writeFileSync } from 'node:fs'
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
  return (await locator.boundingBox())!
}
async function sheetTab(page: Page, key: string, label: string) {
  await expect(page.locator('.sheet-title-row')).toBeVisible()
  const select = page.locator('#sheet-tab-select')
  if (await select.isVisible()) await select.selectOption(key)
  else await page.locator('.main-tabs').getByRole('button', { name: label, exact: true }).click()
}
async function noOverlap(locator: Locator) {
  const errors = await locator.evaluateAll(nodes => nodes.flatMap(node => {
    const boxes = [...node.children].map(child => child.getBoundingClientRect()).filter(r => r.width && r.height)
    return boxes.flatMap((a, i) => boxes.slice(i + 1).filter(b => Math.min(a.right, b.right) - Math.max(a.left, b.left) > 1 && Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 1).map(() => node.className))
  }))
  expect(errors).toEqual([])
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
  await api(request, `/api/characters/${characterId}/items`, { itemDefId: weapon.id, quantity: 1, state: 'equipped', free: true })
  await api(request, `/api/characters/${characterId}/notes`, { title: 'Заметка экспедиции', body: 'Собственный текст проверки.' })
  for (const nameRu of ['След от царапины', 'Ушиб плеча']) await api(request, `/api/characters/${characterId}/critical-injuries`, { nameRu, severity: 'Лёгкая', notes: 'Тест интерфейса.' })
  await api(request, `/api/characters/${characterId}/heroic-ability`, { heroicAbilityId: reference.heroicAbilities.find(h => h.code === 'rot.heroic.influential')!.id }, 'PUT')
  await api(request, `/api/characters/${characterId}/heroic-identity`, { customName: 'Голос перевала', originMode: 'standard', originPrimary: 'destiny' }, 'PUT')
  await api(request, `/api/characters/${characterId}/crafting`, { itemDefId: weapon.id, kind: 'item', costPercent: 100 })
  craftSpendCount = (await api<{ spends: unknown[] }>(request, `/api/characters/${characterId}/crafting/preview`, { itemDefId: weapon.id, kind: 'item' })).spends.length
  expect(craftSpendCount).toBeGreaterThan(0)
  campaignId = (await api<{ id: string }>(request, '/api/campaigns/', { name: 'Экспедиция к перевалу', description: 'Собственные тестовые данные визуальной приёмки.' })).id
  await api(request, `/api/campaigns/${campaignId}/characters`, { characterId })
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
  writeFileSync('/tmp/genesys-v5-followup-fixture.json', JSON.stringify({ email, password, characterId, campaignId, encounterId, npcId }))
})

for (const language of ['ru', 'en']) {
  const l = (ru: string, en: string) => language === 'ru' ? ru : en
  test(`sheet, magic, heroic, crafting and bio acceptance in ${language}`, async ({ page }) => {
    test.setTimeout(180_000)
    await page.addInitScript(({ token, language }) => { localStorage.setItem('genesysforge.token', token); localStorage.setItem('genesysforge.lang', language); localStorage.setItem('genesysforge.analytics-consent', 'denied') }, { token, language })
    for (const width of [1440, 1240, 1024, 768, 375]) {
      await page.setViewportSize({ width, height: 900 })
      await page.goto(`/characters/${characterId}`)
      await sheetTab(page, 'sheet', l('Лист', 'Sheet'))
      await expect(page.locator('.heroic-summary')).toHaveCount(0)
      await expect(page.locator('.crit-ornament')).toHaveCount(1)
      await expect(page.locator('.crit-ornament>i')).toHaveCount(3)
      await sheetTab(page, 'inventory', l('Инвентарь', 'Inventory'))
      const icon = await rect(page.locator('.inv-item-icon').first())
      const title = await rect(page.locator('.inv-card-title').first())
      expect(title.x - icon.x - icon.width).toBeGreaterThanOrEqual(8)
      expect(title.x - icon.x - icon.width).toBeLessThanOrEqual(14)
      await page.locator('.shop-row').filter({ has: page.locator('strong').filter({ hasText: l(weaponName, weaponNameEn) }) }).first().getByRole('button', { name: l('Купить', 'Buy'), exact: true }).click()
      await noOverlap(page.locator('.price-control .price-row'))
      await sheetTab(page, 'magic', l('Магия', 'Magic'))
      const plate = await rect(page.locator('.magic-result-plate'))
      const eyebrow = await rect(page.locator('.magic-result-plate>h4'))
      expect(eyebrow.y).toBeGreaterThanOrEqual(plate.y)
      expect(eyebrow.y + eyebrow.height).toBeLessThanOrEqual(plate.y + plate.height)
      await expect(page.locator('.magic-result-plate>h4')).toHaveCSS('color', 'rgb(200, 180, 240)')
      await sheetTab(page, 'heroic', l('Героика', 'Heroic'))
      const upgrades = await rect(page.locator('.heroic-upgrades'))
      const head = await rect(page.locator('.heroic-upgrades>.label-line'))
      expect(Math.abs(head.y - upgrades.y)).toBeLessThanOrEqual(1)
      await expect(page.locator('.heroic-upgrades').getByRole('heading', { name: l('Сила', 'Power'), exact: true })).toHaveCount(0)
      expect((await rect(page.locator('.heroic-parameters-title'))).x - upgrades.x).toBeGreaterThanOrEqual(14)
      await sheetTab(page, 'workshop', l('Мастерская', 'Workshop'))
      await page.locator('.workshop-tab .sheet-segment').getByRole('button', { name: l('Ремесло', 'Crafting'), exact: true }).click()
      await expect(page.locator('.crafting-project')).toBeVisible()
      await expect(page.getByRole('spinbutton', { name: l('Успехи', 'Successes'), exact: true })).toBeVisible()
      await noOverlap(page.locator('.crafting-project>.sheet-card-heading'))
      await expect(page.locator('.crafting-spends')).toHaveCount(2)
      await expect(page.locator('.crafting-row')).toHaveCount(craftSpendCount)
      await sheetTab(page, 'bio', l('Образ и заметки', 'Bio and notes'))
      if (width > 900) {
        const identity = await rect(page.locator('.bio-identity'))
        const notes = await rect(page.locator('.bio-layout .rd-section-card'))
        expect(Math.abs(identity.y - notes.y)).toBeLessThanOrEqual(1)
      }
      await page.getByLabel(l('Стремление', 'Desire'), { exact: true }).click()
      await expect(page.getByLabel(l('Стремление', 'Desire'), { exact: true })).toHaveCSS('outline-style', 'none')
      await noOverlap(page.locator('.bio-save'))
      await fits(page)
    }
    await page.goto('/magic')
    await expect(page.locator('.magic-result-plate>h4')).toHaveCSS('color', 'rgb(200, 180, 240)')
    await fits(page)
  })

  test(`campaign overview, encounters and game table acceptance in ${language}`, async ({ page }) => {
    test.setTimeout(150_000)
    await page.addInitScript(({ token, language }) => { localStorage.setItem('genesysforge.token', token); localStorage.setItem('genesysforge.lang', language); localStorage.setItem('genesysforge.analytics-consent', 'denied') }, { token, language })
    for (const width of [1440, 1240, 1024, 768, 375]) {
      await page.setViewportSize({ width, height: 900 })
      await page.goto(`/campaigns/${campaignId}`)
      await page.locator('.campaign-tabs').getByRole('button', { name: l('Обзор', 'Overview'), exact: true }).click()
      await expect(page.locator('.campaign-npc-row>.badge')).toContainText(l('Группа приспешников', 'Minion group'))
      expect(await page.locator('.campaign-npc-row>.badge').evaluate(e => e.scrollHeight - e.clientHeight)).toBeLessThanOrEqual(1)
      await fits(page)
      await page.goto(`/campaigns/${campaignId}/encounters/${encounterId}`)
      await expect(page.locator('.enc-scene-content')).toBeVisible()
      await noOverlap(page.locator('.enc-scene-title'))
      await expect(page.locator('.enc-queue>h4')).toHaveCSS('background-color', 'rgb(232, 234, 243)')
      await fits(page)
      await page.locator('.campaign-tabs').getByRole('button', { name: l('Игровой стол', 'Game table'), exact: true }).click()
      await expect(page.locator('.range-rings')).toBeVisible()
      await expect(page.locator('.ring-focus-switch')).toHaveCount(0)
      await expect(page.locator('.range-rings')).toHaveCSS('border-top-width', '0px')
      const board = await rect(page.locator('.range-rings'))
      expect(Math.abs(board.width - board.height)).toBeLessThanOrEqual(1)
      if (width === 1240) expect(board.height).toBeGreaterThan(440)
      await noOverlap(page.locator('.quick-panel .form-row, .quick-panel .quick-main, .participant-control-card .pc-name, .init-row'))
      await expect(page.locator('.roll-entry')).toContainText('Элира')
      await expect(page.locator('.roll-entry-result')).toContainText(l('Успех', 'Success'))
      await expect(page.locator('.roll-entry time')).toBeVisible()
      await fits(page)
    }
  })

  test(`NPC, shop, reference and new character acceptance in ${language}`, async ({ page }) => {
    test.setTimeout(120_000)
    await page.addInitScript(({ token, language }) => { localStorage.setItem('genesysforge.token', token); localStorage.setItem('genesysforge.lang', language); localStorage.setItem('genesysforge.analytics-consent', 'denied') }, { token, language })
    for (const width of [1240, 768, 375]) {
      await page.setViewportSize({ width, height: 900 })
      await page.goto(`/npcs/${npcId}/edit`)
      if (width < 760) await page.getByRole('tab', { name: l('Характеристики', 'Characteristics'), exact: true }).click()
      await expect(page.getByLabel(l('Усталость', 'Strain'), { exact: true })).toBeDisabled()
      await expect(page.getByLabel(l('Усталость', 'Strain'), { exact: true })).toHaveAttribute('placeholder', '—')
      if (width >= 760) {
        await expect(page.locator('.bt-section-nav')).toHaveCSS('padding-left', '0px')
      }
      await fits(page)
      await page.goto('/npcs/new?mode=manual')
      await expect(page.locator('input[name=npc-name]')).toBeVisible()
      if (width >= 760) {
        await noOverlap(page.locator('.bt-mode'))
        await expect(page.locator('.bt-mode')).toHaveCSS('padding', '0px')
      }
      await fits(page)
      await page.goto('/npcs/new?mode=draft')
      await expect(page.locator('.bt-role-icon')).toHaveCount(9)
      const boxes = await page.locator('.bt-role-icon').evaluateAll(nodes => nodes.map(n => ({ width: n.getBoundingClientRect().width, height: n.getBoundingClientRect().height })))
      expect(boxes.every(b => b.width === 38 && b.height === 38)).toBe(true)
      await fits(page)
    }
    await page.setViewportSize({ width: 1240, height: 900 })
    await page.goto('/shop')
    await page.locator('.shop-toolbar select').selectOption(characterId)
    await page.locator('.shop-product-row').filter({ hasText: l(weaponName, weaponNameEn) }).first().click()
    await expect(page.locator('.shop-product-modal')).toBeVisible()
    const material = await rect(page.locator('.shop-modal-options select'))
    const quantity = await rect(page.locator('.shop-modal-options input[type=number]'))
    expect(Math.abs(material.y - quantity.y)).toBeLessThanOrEqual(1)
    expect((await rect(page.locator('.shop-funds'))).height).toBeLessThan(90)
    await page.goto('/reference')
    await expect(page.locator('.reference-page .section-switch>.tab').first()).toBeVisible()
    expect(await page.locator('.reference-page .section-switch>.tab').nth(1).evaluate(e => getComputedStyle(e, '::before').width)).toBe('1px')
    await page.goto('/characters')
    await page.getByRole('button', { name: l('Новый персонаж', 'New character'), exact: true }).click()
    expect((await rect(page.locator('.character-create-modal .system-switch'))).height).toBeLessThanOrEqual(40)
    await fits(page)
  })
}
