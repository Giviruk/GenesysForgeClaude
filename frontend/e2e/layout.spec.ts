import { expect, test, type APIRequestContext, type Page } from '@playwright/test'

interface Reference {
  archetypes: Array<{ id: string }>
  careers: Array<{ id: string }>
  items: Array<{ id: string; kind: string }>
  mounts: Array<{ id: string }>
  heroicAbilities: Array<{ id: string; code: string }>
  heroicSecondaryEffects: Array<{ id: string }>
  attachments: Array<{ id: string; hostKind: string }>
}

let token: string
let characterId: string
let signatureCharacterId: string
let emptyHeroicCharacterId: string
let spacingCharacterId: string
let spacingCampaignId: string
let secondaryEffectCount: number
let campaignId: string
const longHeroicName = 'Наследие'.repeat(15)

async function api<T>(request: APIRequestContext, path: string, data?: unknown): Promise<T> {
  const response = data === undefined
    ? await request.get(path, { headers: { Authorization: `Bearer ${token}` } })
    : await request.post(path, { headers: { Authorization: `Bearer ${token}` }, data })
  expect(response.ok(), `${path}: HTTP ${response.status()}`).toBe(true)
  return response.status() === 204 ? undefined as T : await response.json() as T
}

async function fitsViewport(page: Page) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
}

async function spacedElements(page: Page, selector: string, count: number) {
  const elements = page.locator(selector)
  await expect(elements).toHaveCount(count)
  await expect(elements.first()).toBeVisible()
  const gaps = await elements.evaluateAll(nodes => nodes.slice(1).map((node, i) => {
    const a = nodes[i].getBoundingClientRect()
    const b = node.getBoundingClientRect()
    const sameRow = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 1
    return sameRow ? b.left - a.right : b.top - a.bottom
  }))
  expect(gaps.length, selector).toBeGreaterThan(0)
  for (const gap of gaps) expect(gap, selector).toBeGreaterThanOrEqual(6)
}

async function iconHasSpace(page: Page, selector: string, count: number) {
  const elements = page.locator(selector)
  await expect(elements).toHaveCount(count)
  await expect(elements.first()).toBeVisible()
  const gaps = await elements.evaluateAll(elements => elements.map(element => {
    const label = `${element.tagName}.${element.className}`
    const icon = element.querySelector(':scope > .button-icon')
    if (!icon) throw new Error(`Missing direct .button-icon in ${label}`)
    const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT)
    let text: Node | null
    while ((text = walker.nextNode())) {
      if (text.textContent?.trim() && !icon.contains(text)) break
    }
    if (!text) throw new Error(`Missing readable icon caption in ${label}`)
    const start = text.textContent!.search(/\S/)
    const range = document.createRange()
    range.setStart(text, start)
    range.setEnd(text, start + 1)
    return range.getBoundingClientRect().left - icon.getBoundingClientRect().right
  }))
  for (const gap of gaps) expect(gap, selector).toBeGreaterThanOrEqual(6)
}

// Exercise deep links through the app's History API without loading the document again.
async function navigateWithinApp(page: Page, path: string) {
  await page.evaluate(path => {
    history.pushState(null, '', path)
    dispatchEvent(new PopStateEvent('popstate'))
  }, path)
  await expect(page).toHaveURL(path)
}

async function tab(page: Page, key: string, name: string) {
  await expect(page.locator('.sheet-title-row')).toBeVisible()
  if (await page.locator('#sheet-tab-select').isVisible()) await page.locator('#sheet-tab-select').selectOption(key)
  else await page.locator('.main-tabs').getByRole('button', { name, exact: true }).click()
}

async function arrowHasSpace(page: Page, selector: string) {
  const controls = page.locator(selector)
  await expect(controls.first()).toBeVisible()
  const styles = await controls.evaluateAll(elements => elements.map(element => {
    const style = getComputedStyle(element)
    return { image: style.backgroundImage, padding: parseFloat(style.paddingRight) }
  }))
  for (const style of styles) {
    expect(style.image).not.toBe('none')
    // Chevron width + right inset + a gap between the selected text and the icon.
    expect(style.padding).toBeGreaterThanOrEqual(36)
  }
}

test.beforeAll(async ({ request }) => {
  const response = await request.post('/api/auth/register', { data: {
    email: `layout-${Date.now().toString(36)}@example.test`, password: 'Passw0rd!',
    displayName: 'Мастер тестового приключения с длинным именем',
  } })
  expect(response.ok()).toBe(true)
  token = (await response.json() as { token: string }).token
  const reference = await api<Reference>(request, '/api/reference/RealmsOfTerrinoth')
  const character = await api<{ id: string }>(request, '/api/characters/', {
    name: 'Персонаж с длинным именем для проверки интерфейса', system: 'realmsOfTerrinoth',
    archetypeId: reference.archetypes[0].id, careerId: reference.careers[0].id,
    freeCareerSkillNames: [], archetypeSkillChoices: [], careerGearChoices: [],
  })
  characterId = character.id
  const headers = { Authorization: `Bearer ${token}` }
  await api(request, `/api/characters/${characterId}/items`, {
    itemDefId: reference.items.find(item => item.kind === 'weapon')!.id, quantity: 1, state: 'equipped', free: true,
  })
  await api(request, `/api/characters/${characterId}/mounts`, { mountDefId: reference.mounts[0].id, free: true })
  expect((await request.put(`/api/characters/${characterId}/heroic-ability`, { headers,
    data: { heroicAbilityId: reference.heroicAbilities[0].id },
  })).ok()).toBe(true)
  await api(request, `/api/characters/${characterId}/notes`, { title: 'Приватная заметка', body: 'Тест приватности' })
  campaignId = (await api<{ id: string }>(request, '/api/campaigns/', { name: 'Проверка интерфейса', description: '' })).id
  await api(request, `/api/campaigns/${campaignId}/characters`, { characterId })
  await api(request, `/api/campaigns/${campaignId}/chronicle/chapters`, { title: 'Глава с длинным заголовком', content: 'Тестовая глава.' })

  const createCharacter = async (name: string) => (await api<{ id: string }>(request, '/api/characters/', {
    name, system: 'realmsOfTerrinoth', archetypeId: reference.archetypes[0].id, careerId: reference.careers[0].id,
    freeCareerSkillNames: [], archetypeSkillChoices: [], careerGearChoices: [],
  })).id
  spacingCharacterId = await createCharacter('Проверка отступов и длинного названия способности')
  expect((await request.patch(`/api/characters/${spacingCharacterId}`, { headers, data: { totalXp: 800 } })).ok()).toBe(true)
  await api(request, `/api/characters/${spacingCharacterId}/items`, {
    itemDefId: reference.items.find(item => item.kind === 'weapon')!.id, quantity: 4, state: 'equipped', free: true,
  })
  const mount = await api<{ id: string }>(request, `/api/characters/${spacingCharacterId}/mounts`, { mountDefId: reference.mounts[0].id, free: true })
  expect((await request.patch(`/api/characters/${spacingCharacterId}/mounts/${mount.id}`, { headers, data: { woundsCurrent: 999 } })).ok()).toBe(true)
  const attachment = await api<{ id: string }>(request, `/api/characters/${spacingCharacterId}/attachments`, {
    attachmentDefId: reference.attachments.find(attachment => attachment.hostKind === 'armor')!.id, free: true,
  })
  expect((await request.put(`/api/characters/${spacingCharacterId}/attachments/${attachment.id}/damage-state`, {
    headers, data: { state: 'minor' },
  })).ok()).toBe(true)
  expect((await request.put(`/api/characters/${spacingCharacterId}/heroic-ability`, { headers, data: {
    heroicAbilityId: reference.heroicAbilities.find(ability => ability.code === 'rot.heroic.influential')!.id,
  } })).ok()).toBe(true)
  expect((await request.put(`/api/characters/${spacingCharacterId}/heroic-identity`, { headers, data: {
    customName: longHeroicName, originMode: 'standard', originPrimary: 'destiny',
  } })).ok()).toBe(true)
  secondaryEffectCount = reference.heroicSecondaryEffects.length
  expect((await request.put(`/api/characters/${spacingCharacterId}/heroic-upgrades`, { headers, data: {
    powerRank: 2, durationRanks: 1, frequencyRanks: 1, story: true,
    secondaryEffectIds: reference.heroicSecondaryEffects.slice(0, 2).map(effect => effect.id),
  } })).ok()).toBe(true)
  signatureCharacterId = await createCharacter('Проверка формы именного оружия')
  emptyHeroicCharacterId = await createCharacter('Проверка выбора героической способности')
  expect((await request.put(`/api/characters/${signatureCharacterId}/heroic-ability`, { headers, data: {
    heroicAbilityId: reference.heroicAbilities.find(ability => ability.code === 'rot.heroic.signature-weapon')!.id,
  } })).ok()).toBe(true)
  const campaign = await api<{ id: string; joinCode: string }>(request, '/api/campaigns/', { name: 'Проверка отступов кампании', description: '' })
  spacingCampaignId = campaign.id
  await api(request, `/api/campaigns/${spacingCampaignId}/characters`, { characterId: spacingCharacterId })
  for (const player of [1, 2]) {
    const account = await request.post('/api/auth/register', { data: {
      email: `layout-player-${player}-${Date.now().toString(36)}@example.test`, password: 'Passw0rd!',
      displayName: `Игрок ${player} с длинным именем для проверки переноса элементов`,
    } })
    expect(account.ok()).toBe(true)
    const playerToken = (await account.json() as { token: string }).token
    expect((await request.post('/api/campaigns/join', { headers: { Authorization: `Bearer ${playerToken}` }, data: { joinCode: campaign.joinCode } })).ok()).toBe(true)
  }
})

for (const width of [320, 390, 768, 1280, 1440]) {
  test(`GEN-UI-SHEET-SPACING: heroic and campaign groups have gaps (RU/EN, ${width}px)`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 })
    let documentLoads = 0
    page.on('request', request => {
      if (request.isNavigationRequest() && request.frame() === page.mainFrame()) documentLoads++
    })
    await page.addInitScript(token => {
      localStorage.setItem('genesysforge.token', token)
      if (!localStorage.getItem('genesysforge.lang')) localStorage.setItem('genesysforge.lang', 'ru')
      localStorage.setItem('genesysforge.analytics-consent', 'denied')
    }, token)
    await page.goto(`/characters/${spacingCharacterId}`)
    for (const language of ['ru', 'en']) {
      const label = (ru: string, en: string) => language === 'ru' ? ru : en
      if (language === 'en') {
        // Module-level dictionaries require one reload when switching language.
        await page.locator('.rd-dialog-inner > footer > button').first().click()
        if (await page.locator('.mobile-menu-trigger').isVisible()) await page.locator('.mobile-menu-trigger').click()
        await page.locator('.side-nav-footer').getByRole('button', { name: 'English', exact: true }).click()
        await expect(page.locator('html')).toHaveAttribute('lang', 'en')
        await expect(page.getByRole('button', { name: 'Create content', exact: true })).toBeVisible()
        await navigateWithinApp(page, `/characters/${spacingCharacterId}`)
      }
      await tab(page, 'sheet', label('Лист', 'Sheet'))
      await expect(page.locator('.heroic-summary > strong')).toHaveText(longHeroicName)
      await spacedElements(page, '.heroic-summary > .heroic-upgrade', 4)
      await expect(page.locator('.heroic-summary > .hint').first()).toHaveCSS('margin-top', '0px')
      await fitsViewport(page)
      await page.locator('.sheet-actions-trigger').click()
      await iconHasSpace(page, '.sheet-actions-popover button:has(> .button-icon)', 4)
      await expect(page.locator('.sheet-actions-popover button').first()).toHaveCSS('column-gap', '8px')
      await page.locator('.sheet-actions-trigger').click()
      await tab(page, 'inventory', label('Инвентарь', 'Inventory'))
      await spacedElements(page, '.money-derived > *', 4)
      await expect(page.locator('.money-derived')).toContainText(language === 'ru' ? /\d+\/\d+ перегружен! Поглощение \d+ Защита \d+\/\d+/ : /\d+\/\d+ encumbered! Soak \d+ Defense \d+\/\d+/)
      await fitsViewport(page)
      await tab(page, 'transport', label('Транспорт', 'Transport'))
      await iconHasSpace(page, '.mounts-intro > p', 1)
      // Every wrapped line of the intro starts to the right of its icon.
      const introIndent = await page.locator('.mounts-intro > p').evaluate(element => {
        const icon = element.querySelector(':scope > .button-icon')!
        const range = document.createRange()
        range.selectNodeContents(element)
        range.setStartAfter(icon)
        return Math.min(...Array.from(range.getClientRects(), rect => rect.left)) - icon.getBoundingClientRect().right
      })
      expect(introIndent).toBeGreaterThanOrEqual(6)
      await iconHasSpace(page, '.mount-flags > span', 1)
      await fitsViewport(page)
      await tab(page, 'bio', label('Образ и заметки', 'Bio and notes'))
      await iconHasSpace(page, '.bio-save > [role="status"]', 1)
      await iconHasSpace(page, '.bio-layout > aside p:has(> .button-icon)', 1)
      await fitsViewport(page)
      await tab(page, 'workshop', label('Мастерская', 'Workshop'))
      await page.getByRole('button', { name: label('Улучшения', 'Upgrades'), exact: true }).click()
      await iconHasSpace(page, '.attachment-blocked', 1)
      await iconHasSpace(page, '.attach-row p.warn-text', 1)
      await page.getByRole('button', { name: label('Ремесло', 'Crafting'), exact: true }).click()
      await iconHasSpace(page, '.crafting-calculation > p:has(> .button-icon)', 1)
      await fitsViewport(page)
      await tab(page, 'magic', label('Магия', 'Magic'))
      // Additional effects vary by action; inspect the first explicit unavailable reason.
      await iconHasSpace(page, '.magic-effect-row .warn-text:has(> .button-icon) >> nth=0', 1)
      await fitsViewport(page)
      await tab(page, 'heroic', label('Героика', 'Heroic'))
      await spacedElements(page, '.heroic-identity-form > *', 3)
      await spacedElements(page, '.heroic-identity-save, .heroic-ability-reset button', 2)
      await expect(page.locator('.heroic-ability-reset button')).toHaveCSS('flex-grow', '0')
      await expect(page.locator('.heroic-identity > .hint')).toHaveCSS('margin-top', '0px')
      await spacedElements(page, '.heroic-secondary-effects > *', secondaryEffectCount + 1)
      await expect(page.locator('.heroic-secondary-effects')).toHaveCSS('opacity', '1')
      await expect(page.locator('.heroic-secondary-effects > .bought').first()).toHaveCSS('opacity', '1')
      await expect(page.locator('.heroic-secondary-effects > .heroic-upgrade:not(.bought)').first()).toHaveCSS('opacity', '0.75')
      await spacedElements(page, '.heroic-upgrade-head:has(.heroic-plus[aria-label*="Duration"], .heroic-plus[aria-label*="Длительность"]) > button', 2)
      await iconHasSpace(page, '.heroic-activate-button', 1)
      await iconHasSpace(page, '.heroic-ability-card > h4', 1)
      await fitsViewport(page)
      if (language === 'ru' && width === 1280) {
        await page.locator('.heroic-ability-reset').scrollIntoViewIfNeeded()
        await page.screenshot({ path: test.info().outputPath('heroic-spacing.jpg'), type: 'jpeg' })
      }
      await navigateWithinApp(page, `/characters/${emptyHeroicCharacterId}`)
      await tab(page, 'heroic', label('Героика', 'Heroic'))
      await iconHasSpace(page, '.heroic-ability-card > h3', 1)
      await fitsViewport(page)
      await navigateWithinApp(page, `/characters/${signatureCharacterId}`)
      await tab(page, 'heroic', label('Героика', 'Heroic'))
      await spacedElements(page, '.heroic-weapon-form > *', 7)
      await spacedElements(page, '.heroic-weapon-form > .inline-form:first-child > select', 2)
      await iconHasSpace(page, '.heroic-parameter > h3', 1)
      await fitsViewport(page)
      await navigateWithinApp(page, `/campaigns/${spacingCampaignId}/settings`)
      await spacedElements(page, '.campaign-players-list > .page-head', 2)
      await spacedElements(page, '.campaign-players-panel > .form-actions > button', 2)
      await page.getByRole('button', { name: label('Добавить существующего', 'Add existing character'), exact: true }).click()
      await spacedElements(page, '.campaign-players-panel > .form-actions, .campaign-players-panel > .campaign-character-form', 2)
      await spacedElements(page, '.campaign-character-form .form-actions > button', 2)
      await fitsViewport(page)
      const panelHeight = await page.locator('.campaign-players-panel').evaluate(element => element.getBoundingClientRect().height)
      await page.getByRole('button', { name: label('Создать персонажа', 'Create character'), exact: true }).click()
      await expect(page.locator('.modal')).toBeVisible()
      // The existing-character form stays in place; the fixed modal contributes no empty wrapper.
      await expect(page.locator('.campaign-players-panel > .campaign-character-form')).toHaveCount(1)
      expect(await page.locator('.campaign-players-panel').evaluate(element => element.getBoundingClientRect().height)).toBe(panelHeight)
      await spacedElements(page, '.modal-actions > button', 2)
      await spacedElements(page, '.modal > label:nth-of-type(1), .modal > label:nth-of-type(2), .modal > label:nth-of-type(3)', 3)
      await fitsViewport(page)
      await navigateWithinApp(page, `/campaigns/${spacingCampaignId}/characters/${spacingCharacterId}`)
      await tab(page, 'sheet', label('Лист', 'Sheet'))
      await spacedElements(page, '.heroic-summary > .heroic-upgrade', 4)
      await fitsViewport(page)
      await navigateWithinApp(page, '/library')
      await page.getByRole('button', { name: label('Создать контент', 'Create content'), exact: true }).click()
      await spacedElements(page, '.rd-dialog-body > .rd-system-badge, .rd-dialog-body > .rd-system-badge + .rd-chips', 2)
      await spacedElements(page, '.rd-dialog-inner > footer > button', 2)
      await fitsViewport(page)
    }
    expect(documentLoads, 'Initial document and the RU/EN language switch').toBe(2)
  })
}

for (const width of [320, 390, 768, 1280, 1440]) {
  test(`GEN-UI-01: controls and tables fit ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 })
    await page.addInitScript(value => {
      localStorage.setItem('genesysforge.token', value)
      localStorage.setItem('genesysforge.lang', 'ru')
      localStorage.setItem('genesysforge.analytics-consent', 'denied')
    }, token)

    await page.goto('/account')
    await expect(page.getByRole('button', { name: 'Сохранить', exact: true })).toBeVisible()
    // Только форма профиля: другие формы страницы со своими .form-actions не должны ломать счёт.
    await spacedElements(page, 'form:has([data-testid="avatar-file"]) .form-actions > button', 2)
    await fitsViewport(page)

    await page.goto('/reference')
    await expect(page.locator('.ref-table tbody tr').first()).toBeVisible()
    await fitsViewport(page)
    if (width <= 390) {
      expect(await page.locator('.rule-table .table-wrap').first().evaluate(element => element.scrollWidth > element.clientWidth)).toBe(true)
    }

    await page.goto(`/characters/${characterId}`)
    await expect(page.locator('.rd-skills-grid')).toBeVisible()
    if (width === 1280) {
      const rows = page.locator('.rd-skill-group > div.rd-skill-grid-row')
      const index = await rows.evaluateAll(elements => elements.findIndex(element => element.querySelector('.rd-buy:not(:disabled)')))
      expect(index).toBeGreaterThanOrEqual(0)
      const row = rows.nth(index)
      const geometry = () => row.evaluate(element => ({
        height: element.getBoundingClientRect().height,
        columns: [...element.children].map(cell => cell.getBoundingClientRect().width),
      }))
      const before = await geometry()
      const purchased = page.waitForResponse(response => response.request().method() === 'POST' && response.url().endsWith('/buy-rank'))
      await row.locator('.rd-buy').click()
      expect((await purchased).ok()).toBe(true)
      await expect(row.locator('.rd-refund')).toBeVisible()
      await expect.poll(geometry).toEqual(before)
    }
    if (width <= 768) await arrowHasSpace(page, '#sheet-tab-select')
    else {
      const overflow = await page.locator('.rd-skill-group > div.rd-skill-grid-row').evaluateAll(rows => rows.some(row => {
        const bounds = row.getBoundingClientRect()
        return [...row.children].some(cell => {
          const rect = cell.getBoundingClientRect()
          return rect.left < bounds.left - 1 || rect.right > bounds.right + 1
        })
      }))
      expect(overflow).toBe(false)
    }
    await tab(page, 'inventory', 'Инвентарь')
    await arrowHasSpace(page, '.shop-craftsmanship-select')
    await fitsViewport(page)
    await tab(page, 'history', 'История')
    await expect(page.locator('.audit-table tbody tr').first()).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'workshop', 'Мастерская')
    await expect(page.locator('.attachments-layout')).toBeVisible()
    const installationRules = page.getByRole('button', { name: 'Правила установки', exact: true })
    await installationRules.focus()
    await installationRules.press('Enter')
    await expect(page.getByRole('tooltip')).toContainText('Приложение бросок не делает')
    await fitsViewport(page)
    await page.keyboard.press('Escape')
    await installationRules.evaluate(element => (element as HTMLElement).blur())
    await expect(page.getByRole('tooltip')).toHaveCount(0)
    await page.getByRole('button', { name: 'Ремесло', exact: true }).click()
    await expect(page.locator('.crafting-tab')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'magic', 'Магия')
    await expect(page.locator('.magic-builder')).toBeVisible()
    await fitsViewport(page)
    await page.locator('.magic-builder').getByRole('button', { name: 'Справочник', exact: true }).click()
    await expect(page.locator('.magic-matrix')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'heroic', 'Героика')
    await expect(page.locator('.heroic-ability-card')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'bio', 'Образ и заметки')
    await expect(page.locator('.bio-background')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'transport', 'Транспорт')
    await expect(page.locator('.mount-card')).toBeVisible()
    await fitsViewport(page)

    await page.goto(`/campaigns/${campaignId}/chronicle`)
    await expect(page.getByRole('textbox', { name: 'Название главы' })).toBeVisible()
    await fitsViewport(page)
    await page.goto(`/campaigns/${campaignId}`)
    await expect(page.getByRole('heading', { name: 'Игроки', exact: true })).toHaveCount(0)
    await expect(page.getByRole('heading', { name: 'Наборы кампании', exact: true })).toHaveCount(0)
    await expect(page.getByRole('button', { name: 'Создать персонажа', exact: true })).toHaveCount(0)
    await page.getByRole('button', { name: 'Настройки', exact: true }).click()
    await expect(page).toHaveURL(new RegExp(`/campaigns/${campaignId}/settings$`))
    await page.reload()
    await expect(page.getByRole('heading', { name: 'Игроки', exact: true })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Создать персонажа', exact: true })).toBeVisible()
    await spacedElements(page, '.panel > .form-actions > button', 2)
    await page.getByRole('button', { name: 'Добавить существующего', exact: true }).click()
    await spacedElements(page, 'form .form-actions > button', 2)
    await fitsViewport(page)
    await page.goto(`/campaigns/${campaignId}/characters/${characterId}`)
    await tab(page, 'inventory', 'Инвентарь')
    await expect(page.getByRole('heading', { name: 'Инвентарь', exact: true })).toBeVisible()
    await fitsViewport(page)
    for (const path of ['/campaigns', '/npcs']) {
      await page.goto(path)
      await expect(page.getByRole('heading', { name: path === '/npcs' ? 'Бестиарий' : 'Кампании', exact: true })).toBeVisible()
      await fitsViewport(page)
    }
  })
}

test('GEN-UI-01: public links have gaps and consistent footer colours', async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('genesysforge.lang', 'ru')
    localStorage.setItem('genesysforge.analytics-consent', 'denied')
  })
  for (const width of [320, 1440]) {
    await page.setViewportSize({ width, height: 900 })
    await page.goto('/login')
    await page.getByRole('button', { name: 'Войти по e-mail' }).click()
    await spacedElements(page, '.auth-links > button', 2)
    const colours = await page.locator('.footer-links a, .footer-links button').evaluateAll(elements =>
      elements.map(element => getComputedStyle(element).color))
    expect(new Set(colours).size).toBe(1)
    await fitsViewport(page)
  }
})

test('GEN-UI-01: property and repair tooltips remain inside the screen after resize', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 900 })
  await page.addInitScript(value => {
    localStorage.setItem('genesysforge.token', value)
    localStorage.setItem('genesysforge.lang', 'ru')
    localStorage.setItem('genesysforge.analytics-consent', 'denied')
  }, token)
  await page.goto('/shop')
  await page.locator('.shop-search').fill('Башенный щит')
  const property = page.locator('.shop-product-row .prop-tag[role="button"]').last()
  await property.click()
  const tooltip = page.getByRole('tooltip')
  await expect(tooltip).toBeVisible()
  for (const width of [320, 390, 768]) {
    await page.setViewportSize({ width, height: 900 })
    await expect.poll(() => tooltip.evaluate(element => {
      const rect = element.getBoundingClientRect()
      return rect.left >= 11 && rect.right <= innerWidth - 11
    })).toBe(true)
    await fitsViewport(page)
  }
  await page.goto(`/characters/${characterId}`)
  await tab(page, 'inventory', 'Инвентарь')
  await page.locator('.info-tip').first().click()
  await expect(page.getByRole('tooltip')).toBeVisible()
  await fitsViewport(page)
})

for (const width of [320, 390, 768, 1280, 1440]) {
  test(`SHEET-01–07: English sheet fits ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 })
    await page.addInitScript(value => {
      localStorage.setItem('genesysforge.token', value)
      localStorage.setItem('genesysforge.lang', 'en')
      localStorage.setItem('genesysforge.analytics-consent', 'denied')
    }, token)
    await page.goto(`/characters/${characterId}`)
    await tab(page, 'workshop', 'Workshop')
    await expect(page.locator('.attachments-layout')).toBeVisible()
    await fitsViewport(page)
    await page.getByRole('button', { name: 'Crafting', exact: true }).click()
    await expect(page.locator('.crafting-tab')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'magic', 'Magic')
    await expect(page.locator('.magic-builder')).toBeVisible()
    await fitsViewport(page)
    await page.locator('.magic-builder').getByRole('button', { name: 'Reference', exact: true }).click()
    await expect(page.locator('.magic-matrix')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'heroic', 'Heroic')
    await expect(page.locator('.heroic-ability-card')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'transport', 'Transport')
    await expect(page.locator('.mount-card')).toBeVisible()
    await fitsViewport(page)
    await tab(page, 'bio', 'Bio and notes')
    await expect(page.locator('.bio-background')).toBeVisible()
    await expect(page.getByText('Приватная заметка', { exact: true })).toBeVisible()
    await fitsViewport(page)
    await page.goto(`/campaigns/${campaignId}/characters/${characterId}`)
    await tab(page, 'bio', 'Bio')
    await expect(page.locator('.bio-background')).toBeVisible()
    await expect(page.getByText('Приватная заметка', { exact: true })).toHaveCount(0)
    await fitsViewport(page)
    await page.goto('/magic')
    await expect(page.locator('.magic-builder')).toBeVisible()
    await fitsViewport(page)
  })
}
