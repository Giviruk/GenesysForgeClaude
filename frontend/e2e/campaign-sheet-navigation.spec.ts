import { chooseSelect } from './select'
import { expect, test, type APIRequestContext } from '@playwright/test'
import type { GameSystem, Reference } from '../src/api/types'

let token: string
let campaignId: string
const characters: Array<{ id: string; system: GameSystem }> = []
const privateNote = 'Приватная заметка проверки навигации'

async function api<T>(request: APIRequestContext, path: string, data?: unknown): Promise<T> {
  const headers = { Authorization: `Bearer ${token}` }
  const response = data === undefined ? await request.get(path, { headers }) : await request.post(path, { headers, data })
  expect(response.ok(), `${path}: ${response.status()}`).toBe(true)
  return response.status() === 204 ? undefined as T : await response.json() as T
}

test.beforeAll(async ({ request }) => {
  const account = await request.post('/api/auth/register', { data: {
    email: `campaign-nav-${Date.now()}@example.test`, password: 'CampaignNavigation!2026', displayName: 'Проверка навигации',
  } })
  expect(account.ok()).toBe(true)
  token = (await account.json()).token
  campaignId = (await api<{ id: string }>(request, '/api/campaigns/', { name: 'Навигация листа кампании', description: '' })).id
  for (const system of ['genesysCore', 'realmsOfTerrinoth'] as const) {
    const reference = await api<Reference>(request, `/api/reference/${system}`)
    const character = await api<{ id: string }>(request, '/api/characters/', {
      name: `Навигация: ${system}`, system, archetypeId: reference.archetypes[0].id, careerId: reference.careers[0].id,
      freeCareerSkillNames: [], archetypeSkillChoices: [], careerGearChoices: [],
    })
    characters.push({ id: character.id, system })
    await api(request, `/api/campaigns/${campaignId}/characters`, { characterId: character.id })
    await api(request, `/api/characters/${character.id}/notes`, { title: privateNote, body: 'Только в собственном листе.' })
  }
})

for (const language of ['ru', 'en']) {
  test(`PR #283 reopened: campaign sheet sections stay reachable in ${language} across both systems`, async ({ page }) => {
    test.setTimeout(150_000)
    await page.addInitScript(({ token, language }) => {
      localStorage.setItem('genesysforge.token', token)
      localStorage.setItem('genesysforge.lang', language)
      localStorage.setItem('genesysforge.analytics-consent', 'denied')
    }, { token, language })
    for (const character of characters) {
      await page.goto(`/campaigns/${campaignId}/characters/${character.id}`)
      const select = page.getByLabel(language === 'ru' ? 'Раздел листа' : 'Sheet section')
      for (const width of [375, 768, 769, 800, 900, 1024, 1100]) {
        await page.setViewportSize({ width, height: 900 })
        await expect(select).toBeVisible()
        await expect(page.locator('.main-tabs')).toBeHidden()
        await select.click()
        await expect(page.getByRole('listbox').getByRole('option')).toHaveCount(character.system === 'realmsOfTerrinoth' ? 9 : 8)
        await select.press('Escape')
        await chooseSelect(select, 'history')
        await expect(page.locator('.history-tab')).toBeVisible()
        await chooseSelect(select, 'bio')
        await expect(page.locator('.bio-background')).toBeVisible()
        await expect(page.getByText(privateNote, { exact: true })).toHaveCount(0)
        await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
      }
      await page.reload()
      await expect(select).toHaveJSProperty('value', 'bio')
      await expect(page.locator('.bio-background')).toBeVisible()
      await page.setViewportSize({ width: 1101, height: 900 })
      await expect(select).toBeHidden()
      const history = page.locator('.main-tabs').getByRole('button', {
        name: language === 'ru' ? 'История' : 'History', exact: true,
      })
      await expect(history).toBeInViewport({ ratio: 1 })
      await history.click()
      await expect(page.locator('.history-tab')).toBeVisible()
      await page.setViewportSize({ width: 800, height: 900 })
      await page.goto(`/characters/${character.id}`)
      await expect(select).toHaveJSProperty('value', 'history')
      await chooseSelect(select, 'bio')
      await expect(page.getByText(privateNote, { exact: true })).toBeVisible()
    }
  })
}
