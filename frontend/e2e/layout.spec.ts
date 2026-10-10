import { expect, test, type APIRequestContext, type Page } from '@playwright/test'

interface Reference {
  archetypes: Array<{ id: string }>
  careers: Array<{ id: string }>
  items: Array<{ id: string; kind: string }>
  mounts: Array<{ id: string }>
  heroicAbilities: Array<{ id: string }>
}

let token: string
let characterId: string
let campaignId: string

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

async function separatedButtons(page: Page, selector: string) {
  await expect(page.locator(selector)).toHaveCount(2)
  const gaps = await page.locator(selector).evaluateAll(buttons => buttons.slice(1).map((button, i) => {
    const a = buttons[i].getBoundingClientRect()
    const b = button.getBoundingClientRect()
    const sameRow = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top) > 1
    return sameRow ? b.left - a.right : b.top - a.bottom
  }))
  expect(gaps.length).toBeGreaterThan(0)
  for (const gap of gaps) expect(gap).toBeGreaterThanOrEqual(6)
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
  await api(request, `/api/characters/${characterId}/items`, {
    itemDefId: reference.items.find(item => item.kind === 'weapon')!.id, quantity: 1, state: 'equipped', free: true,
  })
  await api(request, `/api/characters/${characterId}/mounts`, { mountDefId: reference.mounts[0].id, free: true })
  const heroicResponse = await request.put(`/api/characters/${characterId}/heroic-ability`, { headers: { Authorization: `Bearer ${token}` }, data: { heroicAbilityId: reference.heroicAbilities[0].id } })
  expect(heroicResponse.ok()).toBe(true)
  await api(request, `/api/characters/${characterId}/notes`, { title: 'Приватная заметка', body: 'Тест приватности' })
  const campaign = await api<{ id: string }>(request, '/api/campaigns/', { name: 'Проверка интерфейса', description: '' })
  campaignId = campaign.id
  await api(request, `/api/campaigns/${campaignId}/characters`, { characterId })
  await api(request, `/api/campaigns/${campaignId}/chronicle/chapters`, { title: 'Глава с длинным заголовком', content: 'Тестовая глава.' })
})

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
    await separatedButtons(page, 'form:has([data-testid="avatar-file"]) .form-actions > button')
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
    await separatedButtons(page, '.panel > .form-actions > button')
    await page.getByRole('button', { name: 'Добавить существующего', exact: true }).click()
    await separatedButtons(page, 'form .form-actions > button')
    await fitsViewport(page)
    await page.goto(`/campaigns/${campaignId}/characters/${characterId}`)
    await page.locator('.main-tabs').getByRole('button', { name: 'Инвентарь', exact: true }).click()
    await expect(page.getByRole('heading', { name: 'Инвентарь', exact: true })).toBeVisible()
    await fitsViewport(page)
    for (const path of ['/campaigns', '/npcs']) {
      await page.goto(path)
      await expect(page.locator('.page h2')).toBeVisible()
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
    await separatedButtons(page, '.auth-links > button')
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
    await page.locator('.main-tabs').getByRole('button', { name: 'Bio', exact: true }).click()
    await expect(page.locator('.bio-background')).toBeVisible()
    await expect(page.getByText('Приватная заметка', { exact: true })).toHaveCount(0)
    await fitsViewport(page)
    await page.goto('/magic')
    await expect(page.locator('.magic-builder')).toBeVisible()
    await fitsViewport(page)
  })
}
