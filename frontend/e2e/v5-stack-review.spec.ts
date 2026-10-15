import { chooseSelect } from './select'
import { expect, test } from '@playwright/test'

let token: string
let characterId: string
const characterName = 'Герой проверки стека'

test.beforeAll(async ({ request }) => {
  const account = await request.post('/api/auth/register', { data: {
    email: `v5-stack-${Date.now()}@example.test`, password: 'StackReview!2026', displayName: 'Проверка стека',
  } })
  expect(account.ok()).toBe(true)
  token = (await account.json()).token
  const headers = { Authorization: `Bearer ${token}` }
  const response = await request.get('/api/reference/realmsOfTerrinoth', { headers })
  expect(response.ok()).toBe(true)
  const reference = await response.json()
  const character = await request.post('/api/characters/', { headers, data: {
    name: characterName, system: 'realmsOfTerrinoth', archetypeId: reference.archetypes[0].id,
    careerId: reference.careers[0].id, freeCareerSkillNames: [], archetypeSkillChoices: [], careerGearChoices: [],
  } })
  expect(character.ok()).toBe(true)
  characterId = (await character.json()).id
})

test.beforeEach(async ({ page }) => {
  await page.addInitScript(value => {
    localStorage.setItem('genesysforge.token', value)
    localStorage.setItem('genesysforge.lang', 'ru')
    localStorage.setItem('genesysforge.analytics-consent', 'denied')
  }, token)
})

test('PR #283: every sheet section is reachable through the tablet select', async ({ page }) => {
  await page.goto(`/characters/${characterId}`)
  const select = page.getByLabel('Раздел листа')
  for (const width of [769, 800, 1024, 1100]) {
    await page.setViewportSize({ width, height: 900 })
    await expect(select).toBeVisible()
    await expect(page.locator('.main-tabs')).toBeHidden()
    await chooseSelect(select, 'history')
    await expect(page.locator('.history-tab')).toBeVisible()
    await chooseSelect(select, 'bio')
    await expect(page.locator('.bio-background')).toBeVisible()
  }
  await page.setViewportSize({ width: 1101, height: 900 })
  await expect(select).toBeHidden()
  const history = page.locator('.main-tabs').getByRole('button', { name: 'История', exact: true })
  await expect(history).toBeInViewport({ ratio: 1 })
  await history.click()
  await expect(page.locator('.history-tab')).toBeVisible()
})

test('PR #285: character links and management buttons have separate keyboard actions', async ({ page }) => {
  await page.goto('/characters')
  const link = page.getByRole('link', { name: characterName, exact: true })
  const card = page.locator('.char-card').filter({ has: link })
  await expect(link).toHaveAttribute('href', `/characters/${characterId}`)
  await expect(card.locator('a button, [role="link"] button')).toHaveCount(0)
  await link.focus()
  await page.keyboard.press('Tab')
  const duplicate = card.getByRole('button', { name: 'Клонировать', exact: true })
  await expect(duplicate).toBeFocused()
  await duplicate.press('Enter')
  await expect(page).toHaveURL(/\/characters\/[\w-]+$/)
  expect(page.url()).not.toContain(characterId)
  await page.goto('/characters')
  await expect(page.locator('.char-card-link')).toHaveCount(2)
  await expect(page).toHaveURL(/\/characters$/)
  await link.focus()
  await page.keyboard.press('Tab')
  await page.keyboard.press('Tab')
  const remove = card.getByRole('button', { name: 'Удалить', exact: true })
  await expect(remove).toBeFocused()
  page.once('dialog', dialog => dialog.dismiss())
  await remove.press('Enter')
  await expect(page).toHaveURL(/\/characters$/)
  await expect(page.locator('.char-card-link')).toHaveCount(2)
  await link.press('Enter')
  await expect(page).toHaveURL(new RegExp(`/characters/${characterId}$`))
  await page.goto('/characters')
  await expect(link).toBeVisible()
  const bounds = (await card.boundingBox())!
  await page.mouse.click(bounds.x + 30, bounds.y + 30)
  await expect(page).toHaveURL(new RegExp(`/characters/${characterId}$`))
})

test('PR #286: mobile career skills keep visible markers matching the legend', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 900 })
  await page.goto(`/characters/${characterId}`)
  await chooseSelect(page.getByLabel('Раздел листа'), 'sheet')
  for (const width of [320, 375, 767]) {
    await page.setViewportSize({ width, height: 900 })
    const career = page.locator('.rd-skill-grid-row .rd-career-mark.active')
    expect(await career.count()).toBeGreaterThan(0)
    for (const marker of await career.all()) await expect(marker).toBeVisible()
    await expect(page.locator('.rd-skill-legend .rd-career-mark.active')).toBeVisible()
    await expect(page.locator('.rd-skill-grid-row .rd-career-mark:not(.active)').first()).toBeVisible()
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
  }
})

test('PR #286: Ctrl+P keeps characteristic ranks and dice in separate columns', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1240, height: 900 })
  await page.goto(`/characters/${characterId}`)
  await page.locator('.main-tabs').getByRole('button', { name: 'Лист', exact: true }).click()
  for (const progression of [false, true]) {
    await page.emulateMedia({ media: 'screen' })
    const progress = page.getByRole('button', { name: 'Прокачка', exact: true })
    if ((await progress.getAttribute('aria-pressed') === 'true') !== progression) await progress.click()
    await page.emulateMedia({ media: 'print' })
    const rows = page.locator('.rd-skill-group > div.rd-skill-grid-row')
    expect(await rows.count()).toBeGreaterThan(0)
    const issues = await rows.evaluateAll(elements => elements.flatMap(row => {
      const bounds = row.getBoundingClientRect()
      const parts = ['.rd-skill-name', '.rd-skill-characteristic', '.rd-rank-diamonds', '.rd-skill-pool']
        .map(selector => row.querySelector(selector)!.getBoundingClientRect())
      return parts.flatMap((part, index) => {
        const previous = parts[index - 1]
        return part.width <= 0 || part.right > bounds.right + 1 || (previous && part.left < previous.right - 1)
          ? [row.textContent] : []
      })
    }))
    expect(issues).toEqual([])
    await expect(rows.first().locator('.rd-skill-actions')).toBeHidden()
    if (!progression) await page.screenshot({ path: testInfo.outputPath('ctrl-p.png'), fullPage: true })
  }
})

test('PR #290: archer and caster draft buttons use teal and purple', async ({ page }) => {
  await page.goto('/npcs/new?mode=draft')
  const grid = page.locator('.bt-role-grid')
  const archer = grid.getByRole('button', { name: /^Стрелок/ })
  const caster = grid.getByRole('button', { name: /^Маг/ })
  await expect(archer).toBeVisible()
  await expect(caster).toBeVisible()
  await expect(archer).toHaveCSS('--tone', '#12969a')
  await expect(caster).toHaveCSS('--tone', '#6a3fb0')
  await archer.click()
  await expect(archer).toHaveAttribute('aria-pressed', 'true')
  await expect(archer).toHaveCSS('border-top-color', 'rgb(18, 150, 154)')
  await caster.click()
  await expect(caster).toHaveAttribute('aria-pressed', 'true')
  await expect(caster).toHaveCSS('border-top-color', 'rgb(106, 63, 176)')
})
