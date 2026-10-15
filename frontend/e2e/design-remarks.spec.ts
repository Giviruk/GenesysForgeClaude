import { chooseSelect } from './select'
import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'
import type { GameSession, NpcDetail, Reference } from '../src/api/types'

test.use({ serviceWorkers: 'block' })

let token: string
let characterId: string
let campaignId: string
let encounterId: string
let npcId: string
let signatureCharacterId: string
let reference: Reference
let weaponName: string
let weaponNameEn: string
let craftSpendCount: number
let mountName: string
let mountNameRu: string
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
  if (await select.isVisible()) await chooseSelect(select, key)
  else await page.locator('.main-tabs').getByRole('button', { name: label, exact: true }).click()
}
test.beforeAll(async ({ request }) => {
  const account = await request.post('/api/auth/register', { data: { email, password, displayName: 'Визуальная приёмка v5' } })
  expect(account.ok()).toBe(true)
  token = (await account.json()).token
  reference = await api<Reference>(request, '/api/reference/realmsOfTerrinoth')
  mountName = reference.mounts[0].name
  mountNameRu = reference.mounts[0].nameRu || mountName
  const weapon = reference.items.find(i => i.name === 'Halberd') ?? reference.items.find(i => i.kind === 'weapon' && i.purchasable)!
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
  const pack = await api<{ id: string }>(request, '/api/homebrew-packs', { name: 'Тени расширения — собственный тестовый набор (RealmsOfTerrinoth)', description: 'Собственный тестовый набор.', system: 'realmsOfTerrinoth' })
  await api(request, '/api/custom/skills', { name: 'Контроль пути', system: 'realmsOfTerrinoth', characteristic: 'cunning', kind: 'general', packIds: [pack.id] })
  const talent = await api<{ id: string }>(request, '/api/custom/talents', {
    name: 'Тестовый талант печати', system: 'realmsOfTerrinoth', tier: 1, isRanked: false,
    category: 'general', activation: 'Passive', description: 'Собственный тестовый талант.', packIds: [pack.id],
  })
  await api(request, `/api/characters/${characterId}/talents/buy`, { talentDefId: talent.id })
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
  await api(request, `/api/campaigns/${campaignId}/homebrew-packs/${pack.id}`, { isEnabled: true }, 'PUT')
  await api(request, `/api/campaigns/${campaignId}/characters`, { characterId })
  await api(request, `/api/campaigns/${campaignId}/chronicle/chapters`, { title: 'Дорога к перевалу', content: '# Первый день\n\nСобственный текст для проверки редактора и превью.' })
  const npc = await api<NpcDetail>(request, '/api/npcs/', {
    name: 'Страж северных ворот', system: 'realmsOfTerrinoth', kind: 'rival', role: 'brute', visibility: 'private', campaignId: null,
    description: 'Собственный тестовый NPC.', source: 'UI test', brawn: 3, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2,
    woundThreshold: 14, strainThreshold: null, soak: 3, meleeDefense: 1, rangedDefense: 0, silhouette: 1, tactics: '',
    skills: [{ name: reference.skills.find(s => s.kind === 'combat')!.name, ranks: 2 }], attacks: [{ name: 'Тестовая атака', skillName: reference.skills.find(s => s.kind === 'combat')!.name, damage: '+3', critical: '2', rangeBand: 'Вплотную', notes: '', qualities: reference.qualities.filter(q => q.hasRating).slice(0, 5).map(q => ({ qualityCode: q.code, nameRu: q.nameRu, rating: 2 })), sourceWeapon: '' }],
    talents: [reference.talents[0].nameRu || reference.talents[0].name], equipment: [], abilities: [{ name: 'Тестовая способность', description: 'Собственное описание.' }], tags: [],
  })
  npcId = npc.id
  const minion = await api<NpcDetail>(request, '/api/npcs/', { ...npc, id: undefined, name: 'Дозор у переправы', kind: 'minion', strainThreshold: null, skills: [] })
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
    let npcLists = 0
    page.on('request', request => { if (/\/api\/(?:v1\/)?npcs\/?$/.test(new URL(request.url()).pathname)) npcLists++ })
    if (!baseline) await page.route(/\/audit(?:\?|$)/, route => route.fulfill({ json: [
      { id: 'review-undo', action: 'skillRankBought', summary: 'Куплен навык', xpDelta: -5, totalXpAfter: 800, spentXpAfter: 5, canUndo: true, createdAt: '2026-10-15T00:00:00Z' },
      { id: 'review-mount', action: 'mountBought', summary: `Приобретён скакун «${mountName}»`, xpDelta: null, totalXpAfter: 800, spentXpAfter: 5, canUndo: false, createdAt: '2026-10-15T00:00:00Z' },
    ] }))
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
        await expect(page.locator('.mobile-topbar-brand')).toHaveCSS('font-size', '14px')
        await expect(page.locator('.mobile-topbar-brand')).toHaveCSS('font-weight', '300')
        expect(parseFloat(await page.locator('.mobile-topbar-brand').evaluate(el => getComputedStyle(el).letterSpacing))).toBeCloseTo(1.68, 1)
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
          await expect.poll(() => page.locator('.talent-list .compact-description').evaluateAll(nodes => nodes.filter(node => {
            const text = node.querySelector('.compact-description-text')!
            return Boolean(node.querySelector('.compact-description-toggle')) !== (text.scrollHeight > text.clientHeight + 1)
          }).length)).toBe(0)
        }
        if (!baseline && key === 'sheet') {
          const progress = page.getByRole('button', { name: language === 'ru' ? 'Прокачка' : 'Progression', exact: true })
          await expect(progress).toBeVisible()
          expect(await progress.evaluate(el => getComputedStyle(el, '::before').content)).toMatch(/^(none|normal)$/)
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
        if (!baseline && key === 'history') {
          const rows = page.locator('.audit-table tbody tr')
          if (language === 'ru') {
            await expect(rows.nth(1)).toContainText(mountNameRu)
            await expect(rows.nth(1).locator('.badge')).toHaveText('Скакун +')
          }
          if (width === 1240) {
            const undo = page.locator('.audit-undo')
            expect((await rect(undo)).height).toBe(24)
            const heights = await rows.evaluateAll(nodes => nodes.map(node => node.getBoundingClientRect().height))
            expect(Math.abs(heights[0] - heights[1])).toBeLessThanOrEqual(1)
            await expect(undo).toHaveCSS('text-transform', 'uppercase')
            await expect(undo).toHaveCSS('font-size', '13px')
          }
        }
        await page.screenshot({ path: testInfo.outputPath(`${width}-${key}.png`), fullPage: false })
        if (language === 'ru' && ((width === 1240 && ['history', 'heroic'].includes(key)) || (width === 375 && key === 'sheet'))) {
          await page.evaluate(() => window.scrollTo(0, 0))
          await page.screenshot({ path: testInfo.outputPath(`review-${key}-${width}.png`), fullPage: false,
            mask: [page.locator('.compact-description-text,.heroic-ability-card>p,.heroic-power-level p')], maskColor: '#b7bcc5' })
        }
      }
      const beforeNpcLists = npcLists
      await page.goto(`/campaigns/${campaignId}/encounters/${encounterId}`)
      await expect(page.locator('.enc-detail')).toBeVisible()
      if (!baseline) await fits(page)
      if (!baseline) {
        expect(await page.locator('.enc-participants .table-wrap').evaluate(el => el.scrollWidth - el.clientWidth)).toBeLessThanOrEqual(1)
        await expect(page.locator('.participant-table .badge')).toHaveClass(/tone-orange/)
        await expect.poll(() => npcLists - beforeNpcLists).toBe(1)
      }
      await page.screenshot({ path: testInfo.outputPath(`${width}-encounter.png`), fullPage: false })
      if (!baseline) {
        for (const view of ['', '/chronicle', '/table', '/content']) {
          await page.goto(`/campaigns/${campaignId}${view}`)
          await expect(page.locator('.campaign-tabs')).toBeVisible()
          await fits(page)
          if (view === '') for (const button of await page.locator('.campaign-story-stepper button,.campaign-story-transfers button').all()) {
            const box = await rect(button)
            expect(box.width).toBe(width < 768 ? 44 : 24)
            expect(box.height).toBe(width < 768 ? 44 : 24)
          }
          if (view === '/table' && width === 1240) {
            expect((await rect(page.locator('.left-rail'))).width).toBeGreaterThanOrEqual(210)
            expect(await page.locator('.story-title').evaluate(el => el.getBoundingClientRect().height)).toBeLessThan(24)
            for (const button of await page.locator('.story-transfer button').all()) {
              expect(await button.evaluate(el => el.scrollHeight)).toBeLessThan(48)
            }
          }
          await page.screenshot({ path: testInfo.outputPath(`${width}-campaign-${view.slice(1) || 'overview'}.png`), fullPage: false })
          if (language === 'ru' && width === 1240 && ['', '/table'].includes(view)) {
            await page.screenshot({ path: testInfo.outputPath(`review-campaign-${view.slice(1) || 'overview'}-${width}.png`),
              mask: [page.locator('.rule-text,.compact-description-text')], maskColor: '#b7bcc5' })
          }
        }
        for (const route of [`/npcs/${npcId}`, '/npcs/new?mode=draft', '/npcs/new?mode=manual']) {
          await page.goto(route)
          await expect(page.locator('.bt-card,.bt-draft-form,.bt-manual')).toBeVisible()
          await fits(page)
          await page.screenshot({ path: testInfo.outputPath(`${width}-${route.includes('new') ? route.split('=')[1] : 'npc'}.png`), fullPage: false })
        }
        await page.goto('/library')
        await expect(page.locator('.rd-entry-description').filter({ hasText: 'Контроль пути' })).toContainText('Контроль пути')
        await expect(page.locator('.library-system-switch .active')).toContainText('Realms of Terrinoth')
        expect(await page.locator('.library-system-switch .rd-filter-chip').evaluateAll(nodes => nodes.some(node => !['none', 'normal'].includes(getComputedStyle(node, '::before').content)))).toBe(false)
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

test('keeps navigation visible while a lazy page chunk is loading', async ({ page }) => {
  await page.addInitScript(token => {
    localStorage.setItem('genesysforge.token', token)
    localStorage.setItem('genesysforge.lang', 'ru')
    localStorage.setItem('genesysforge.analytics-consent', 'denied')
  }, token)
  let release!: () => void
  const chunkReady = new Promise<void>(resolve => { release = resolve })
  await page.route(/\/(?:src\/pages\/NpcsPage\.tsx|assets\/NpcsPage-[^/]+\.js)(?:\?|$)/, async route => {
    await chunkReady
    await route.continue()
  })
  await page.goto('/characters')
  await expect(page.locator('.characters-page')).toBeVisible()
  try {
    await page.locator('.app-sidebar .side-nav').getByRole('button', { name: 'Бестиарий', exact: true }).click()
    await expect(page.locator('.page-content-loading')).toBeVisible()
    await expect(page.locator('.app-sidebar')).toBeVisible()
    await expect(page.locator('.app-loading')).toHaveCount(0)
  } finally { release() }
  await expect(page.locator('.bt-items')).toBeVisible()
})

async function setupSignatureCharacter(request: APIRequestContext) {
  if (!signatureCharacterId) {
    signatureCharacterId = (await api<{ id: string }>(request, '/api/characters/', {
      name: 'Мастеровой с именным оружием', system: 'realmsOfTerrinoth', archetypeId: reference.archetypes[0].id,
      careerId: reference.careers[0].id, freeCareerSkillNames: [], archetypeSkillChoices: [], careerGearChoices: [],
    })).id
    await api(request, `/api/characters/${signatureCharacterId}`, { money: 1000 }, 'PATCH')
    await api(request, `/api/characters/${signatureCharacterId}/heroic-ability`, { heroicAbilityId: reference.heroicAbilities.find(h => h.code === 'rot.heroic.signature-weapon')!.id }, 'PUT')
    await api(request, `/api/characters/${signatureCharacterId}/heroic-identity`, { customName: 'Наручи мастера', originMode: 'standard', originPrimary: 'destiny' }, 'PUT')
    await api(request, `/api/characters/${signatureCharacterId}/heroic-configuration`, {
      weaponProfile: 'brawl', craftsmanship: 'steel', narrativeForm: 'Наручи мастера', formTraits: 'bluntOrCrushing',
      baseAttachmentDefId: reference.attachments.find(a => a.code.endsWith('.runic-thunder'))!.id,
    }, 'PUT')
  }
}

for (const language of ['ru', 'en']) test(`GEN-V5-15 second review in ${language}`, async ({ page, request }, testInfo) => {
  test.setTimeout(240_000)
  page.setDefaultTimeout(15_000)
  const ru = language === 'ru'
  const baseline = process.env.DESIGN_BASELINE === '1'
  await page.addInitScript(({ token, language }) => {
    localStorage.setItem('genesysforge.token', token)
    localStorage.setItem('genesysforge.lang', language)
    localStorage.setItem('genesysforge.analytics-consent', 'denied')
  }, { token, language })
  await setupSignatureCharacter(request)
  async function shot(name: string, width: number) {
    if (ru && width !== 1024 && name === 'statblock') {
      await page.locator('.modal-header').screenshot({ path: testInfo.outputPath(`${baseline ? 'before' : 'after'}-${name}-${width}.png`) })
      return
    }
    if (ru && width !== 1024) await page.screenshot({ path: testInfo.outputPath(`${baseline ? 'before' : 'after'}-${name}-${width}.png`),
      mask: [page.locator('p:not(.crafting-funds),.compact-description-text,.crafting-table')], maskColor: '#b7bcc5' })
  }
  for (const width of [1240, 1024, 375, 1440]) {
    await page.setViewportSize({ width, height: 900 })
    await page.goto(`/characters/${characterId}`)
    await sheetTab(page, 'inventory', ru ? 'Инвентарь' : 'Inventory')
    await expect(page.locator('.inv-title-line>strong').first()).toBeVisible()
    await page.evaluate(() => document.fonts.ready)
    if (!baseline) {
      await fits(page)
      const titles = page.locator('.inv-title-line>strong')
      expect(await titles.evaluateAll(nodes => nodes.every(el => el.getBoundingClientRect().width >= Math.min(159, el.parentElement!.clientWidth)))).toBe(true)
      await expect(titles.last()).toHaveCSS('overflow-wrap', 'normal')
    }
    await shot('inventory', width)
    await sheetTab(page, 'sheet', ru ? 'Лист' : 'Sheet')
    if (!baseline) {
      expect(await page.locator('.rd-skill-name>span').evaluateAll(nodes => nodes.every(el => el.getBoundingClientRect().height <= 44))).toBe(true)
      for (const badge of await page.locator('.sheet-page-head .badge').all()) expect(parseFloat(await badge.evaluate(el => getComputedStyle(el).fontSize))).toBeGreaterThanOrEqual(11)
      const critical = page.locator('.critical-injuries>.section-title-line')
      expect(await critical.evaluate(el => getComputedStyle(el, '::after').content)).toBe('none')
    }
    await sheetTab(page, 'talents', ru ? 'Таланты' : 'Talents')
    if (!baseline) for (const button of await page.locator('.talent-actions>button').all()) {
      expect(await button.evaluate(el => getComputedStyle(el).whiteSpace)).toBe('nowrap')
    }
    if (!baseline) {
      expect(await page.locator('.talent-actions>button').evaluateAll((nodes, maxHeight) =>
        nodes.every(el => el.getBoundingClientRect().height <= maxHeight), width < 768 ? 44.1 : 38.1)).toBe(true)
      const print = page.locator('.talent-print').first()
      const bounds = await rect(print)
      expect(bounds.width).toBeCloseTo(width < 768 ? 44 : 28, 1)
      expect(bounds.height).toBeCloseTo(width < 768 ? 44 : 28, 1)
    }
    await page.goto(`/characters/${signatureCharacterId}`)
    await sheetTab(page, 'heroic', ru ? 'Героика' : 'Heroic')
    if (!baseline) {
      await expect(page.locator('.heroic-weapon-stats')).toContainText(ru ? 'Сила +2' : 'Brawn +2')
      await expect(page.locator('.heroic-weapon-stats')).toContainText(ru ? 'Вплотную' : 'Engaged')
      expect(await page.locator('.heroic-weapon-stats small').evaluateAll(nodes => nodes.every(el => parseFloat(getComputedStyle(el).fontSize) >= 11))).toBe(true)
    }
    await page.locator('.heroic-weapon-stats').scrollIntoViewIfNeeded()
    await shot('signature', width)
    await page.goto(`/npcs/${npcId}/edit`)
    if (width < 768) await page.getByRole('tab', { name: ru ? 'Атаки' : 'Attacks', exact: true }).click()
    await expect(page.locator('.npc-attack-qualities').or(page.locator('.ability-edit .chips')).first()).toBeAttached()
    const qualities = page.locator('.npc-attack-qualities .chip')
    if (!baseline) {
      await expect(qualities).toHaveCount(5)
      for (const chip of await qualities.all()) {
        expect((await rect(chip)).height).toBeLessThanOrEqual(28.1)
        await expect(chip).toHaveCSS('white-space', 'nowrap')
        expect((await rect(chip.locator('input'))).width).toBeCloseTo(44, 1)
      }
      await expect(page.locator('.bt-field-label').first()).toHaveCSS('font-size', '12px')
    }
    await page.locator('.npc-attack-qualities,.ability-edit .chips').first().scrollIntoViewIfNeeded()
    await shot('npc-qualities', width)
    if (!baseline) {
      const picker = page.locator('.ability-edit .select-control').last()
      await expect(picker).toBeEnabled()
      await picker.evaluate(el => el.scrollIntoView({ block: 'end' }))
      const before = await rect(picker)
      await picker.press('ArrowDown')
      const popup = page.getByRole('listbox')
      const bounds = await rect(popup)
      expect(await popup.evaluate(el => el.parentElement === document.body)).toBe(true)
      expect(bounds.x).toBeGreaterThanOrEqual(15)
      expect(bounds.x + bounds.width).toBeLessThanOrEqual(width - 15)
      expect(bounds.y).toBeGreaterThanOrEqual(0)
      expect(bounds.y + bounds.height).toBeLessThanOrEqual(900)
      if (900 - before.y - before.height < bounds.height) expect(bounds.y + bounds.height).toBeLessThanOrEqual(before.y)
      await expect(popup.getByRole('option').first()).toHaveCSS('font-size', '14px')
      expect((await rect(popup.getByRole('option').first())).height).toBeGreaterThanOrEqual(width < 768 ? 43.9 : 31.9)
      await picker.press('End')
      await picker.press('Escape')
      await expect(picker).toHaveJSProperty('value', '')
      await expect(picker).toBeFocused()
    }
    await page.goto('/npcs/new?mode=draft')
    if (!baseline && width === 375) {
      await page.getByRole('button', { name: ru ? 'Далее: тип и сила' : 'Next: type and power', exact: true }).click()
      await expect(page.locator('.bt-kind-cards>button').first()).toBeVisible()
      const cards = await page.locator('.bt-kind-cards>button').evaluateAll(nodes => nodes.map(el => el.getBoundingClientRect().x))
      expect(new Set(cards).size).toBe(1)
    }
    if (!baseline) {
      await page.goto('/npcs')
      const npcTitle = page.locator('.bt-item-title strong').first()
      await expect(npcTitle).toBeVisible()
      await expect(npcTitle).toHaveCSS('-webkit-line-clamp', '2')
      expect(await npcTitle.getAttribute('title')).toBe(await npcTitle.textContent())
      await page.goto(`/campaigns/${campaignId}`)
      const footer = page.locator('.campaign-pc-foot').first()
      await expect(footer).toBeVisible()
      await expect(footer.locator(':scope>span').first()).toHaveCSS('white-space', 'nowrap')
      const actions = await footer.locator('button').evaluateAll(nodes => nodes.map(el => el.getBoundingClientRect().top))
      expect(actions).toHaveLength(2)
      expect(Math.abs(actions[0] - actions[1])).toBeLessThanOrEqual(1)
    }
    await page.goto(`/campaigns/${campaignId}/encounters/${encounterId}`)
    await expect(page.locator('.enc-detail')).toBeVisible()
    if (!baseline) {
      await expect(page.locator('.enc-head-name')).toHaveCSS('font-size', '28px')
      if (width > 767) {
        const tops = await page.locator('.enc-scene-controls>.select-control').evaluateAll(nodes => nodes.map(el => el.getBoundingClientRect().top))
        expect(Math.abs(tops[0] - tops[1])).toBeLessThanOrEqual(1)
      }
      await fits(page)
    }
    await page.goto(`/campaigns/${campaignId}/table`)
    await expect(page.locator('.range-rings')).toBeVisible()
    if (!baseline) {
      expect((await rect(page.locator('.range-rings'))).width).toBeLessThanOrEqual(width === 1024 ? 420.1 : 380.1)
      if (width === 1024) {
        const left = await rect(page.locator('.left-rail'))
        const center = await rect(page.locator('.center-stage'))
        const right = await rect(page.locator('.right-rail'))
        expect(center.x - left.x - left.width).toBeGreaterThanOrEqual(11)
        expect(Math.abs(center.y - left.y)).toBeLessThanOrEqual(1)
        expect(right.y).toBeGreaterThanOrEqual(Math.max(left.y + left.height, center.y + center.height))
      }
      if (width === 1240) {
        expect((await rect(page.locator('.left-rail'))).width).toBeCloseTo(280, 1)
        const shortName = page.locator('.init-row .select-value').filter({ hasText: /^Дозор у переправы ×3$/ })
        await expect(shortName).toHaveText('Дозор у переправы ×3')
        const nameBounds = await shortName.evaluate(el => ({ label: el.textContent!, available: el.clientWidth, content: el.scrollWidth }))
        expect(nameBounds.label).toHaveLength(20)
        expect(nameBounds.content, JSON.stringify(nameBounds)).toBeLessThanOrEqual(nameBounds.available)
        for (const button of await page.locator('.story-transfer button').all()) expect((await rect(button)).height).toBeLessThanOrEqual(38.1)
        expect((await rect(page.locator('.left-rail'))).height).toBeLessThanOrEqual((await rect(page.locator('.center-stage'))).height)
      }
      await fits(page)
    }
    await shot('table', width)
    await page.getByRole('button', { name: ru ? 'Статблок' : 'Stat block', exact: true }).first().click()
    if (!baseline) {
      await expect(page.locator('.modal-header h3')).toHaveCSS('color', 'rgb(245, 161, 28)')
      await expect(page.locator('.modal-header .muted').first()).toHaveCSS('color', 'rgb(201, 205, 224)')
    }
    await shot('statblock', width)
    await page.keyboard.press('Escape')
    await page.goto('/library')
    await expect(page.locator('.rd-pack-tag').first()).toBeVisible()
    if (!baseline) {
      await expect(page.locator('.rd-pack-tag').first()).toHaveCSS('white-space', 'nowrap')
      expect(await page.locator('.rd-pack-tag').first().getAttribute('title')).toContain('Тени расширения')
      await fits(page)
      await page.locator('.rd-pack-tag').first().click()
      await page.getByRole('button', { name: ru ? 'Добавить из библиотеки' : 'Add from library', exact: true }).click()
      const dialog = page.getByRole('dialog')
      const sort = dialog.getByRole('combobox', { name: ru ? 'Сортировка' : 'Sort' })
      await sort.click()
      const popup = page.getByRole('listbox')
      await expect(popup).toBeVisible()
      expect(await popup.evaluate(el => el.parentElement?.tagName)).toBe('DIALOG')
      expect(await popup.evaluate(el => el.matches(':popover-open'))).toBe(true)
      if (ru && width !== 1024) await popup.screenshot({ path: testInfo.outputPath(`after-select-dialog-${width}.png`) })
      await popup.locator('[data-value=name]').click()
      await expect(sort).toHaveJSProperty('value', 'name')
      await expect(sort).toBeFocused()
      await sort.press('ArrowDown')
      await expect(popup).toBeVisible()
      await sort.press('Escape')
      await expect(popup).toBeHidden()
      await expect(dialog).toBeVisible()
      await expect(sort).toBeFocused()
    }
  }
})

test('GEN-V5-15 component payment updates the wallet and cancellation refunds it in the UI', async ({ page, request }) => {
  test.skip(process.env.DESIGN_BASELINE === '1')
  await page.addInitScript(token => {
    localStorage.setItem('genesysforge.token', token)
    localStorage.setItem('genesysforge.lang', 'ru')
    localStorage.setItem('genesysforge.analytics-consent', 'denied')
  }, token)
  await setupSignatureCharacter(request)
  await page.goto(`/characters/${signatureCharacterId}`)
  await sheetTab(page, 'workshop', 'Мастерская')
  await page.locator('.workshop-tab .sheet-segment').getByRole('button', { name: 'Ремесло', exact: true }).click()
  await page.locator('.crafting-targets').getByRole('button').filter({ hasText: weaponName }).first().click()
  await page.locator('.crafting-corrections>summary').click()
  await page.getByLabel('Своя стоимость', { exact: true }).fill('20')
  await page.getByLabel('Причина', { exact: true }).first().fill('Стоимость для проверки интерфейса')
  await expect(page.getByRole('button', { name: 'Начать проект', exact: true })).toBeEnabled()
  const before = (await api<{ money: number }>(request, `/api/characters/${signatureCharacterId}`)).money
  await page.getByRole('button', { name: 'Начать проект', exact: true }).click()
  await expect(page.locator('.crafting-funds')).toContainText(String(before - 20))
  await page.getByRole('button', { name: 'Отменить проект', exact: true }).click()
  await page.getByRole('button', { name: 'Да, отменить проект', exact: true }).click()
  await expect(page.locator('.crafting-funds')).toContainText(String(before))
})
