import { expect, test } from '@playwright/test'

test('документы и уведомление регистрации доступны без аккаунта', async ({ page }) => {
  await page.route(/\/api\/(auth|v1)\//, route => route.fulfill({ status: 401, contentType: 'application/json', body: '{}' }))
  await page.goto('/privacy')
  await expect(page.getByRole('heading', { name: 'Политика обработки персональных данных GenesysForge' })).toBeVisible()
  if (await page.getByRole('button', { name: 'Отказаться' }).isVisible()) {
    await page.getByRole('button', { name: 'Отказаться' }).click()
  }
  await page.getByRole('link', { name: 'Соглашение', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Пользовательское соглашение GenesysForge' })).toBeVisible()
  await page.goto('/register')
  await expect(page.getByText('Регистрируясь, вы принимаете', { exact: false })).toBeVisible()
})

// Запуск с VITE_ARIADNE_ENDPOINT=http://analytics.test и VITE_ARIADNE_PROJECT_KEY=pub_test.
// Настоящая аналитика в этом тесте не получает событий.
test('публичные документы, равный выбор аналитики и отзыв после reload', async ({ page }) => {
  test.skip(process.env.E2E_ANALYTICS_CONSENT !== '1', 'Нужен frontend с тестовой конфигурацией аналитики')
  let trackers = 0
  const fontRequests: string[] = []
  page.on('request', request => {
    if (/fonts\.(googleapis|gstatic)\.com/.test(request.url())) fontRequests.push(request.url())
  })
  await page.route(/\/api\/(auth|v1)\//, route => route.fulfill({ status: 401, contentType: 'application/json', body: '{}' }))
  await page.route('http://analytics.test/tracker.js', route => {
    trackers++
    return route.fulfill({ contentType: 'text/javascript', body: `
      localStorage.setItem('ariadne.anonymous', '11111111-1111-1111-1111-111111111111');
      sessionStorage.setItem('ariadne.session', 'session');
      sessionStorage.setItem('ariadne.activity', 'time');
      window.ariadne = { track() {}, getAnonymousId() { return localStorage.getItem('ariadne.anonymous') },
        optOut() {}, reset() {} };
    ` })
  })
  await page.goto('/privacy')
  await expect(page.getByRole('heading', { name: 'Политика обработки персональных данных GenesysForge' })).toBeVisible()
  await expect(page.getByRole('region', { name: 'Согласие на аналитику' })).toBeVisible()
  expect(trackers).toBe(0)
  const banner = page.getByRole('region')
  expect(await banner.getByRole('button', { name: 'Принять' }).evaluate(el => getComputedStyle(el).backgroundColor))
    .toBe(await banner.getByRole('button', { name: 'Отказаться' }).evaluate(el => getComputedStyle(el).backgroundColor))
  await page.getByRole('button', { name: 'Отказаться' }).click()
  await page.reload()
  await expect(page.getByRole('region')).toHaveCount(0)
  expect(trackers).toBe(0)
  await page.getByRole('link', { name: 'Соглашение', exact: true }).click()
  await expect(page.getByRole('heading', { name: 'Пользовательское соглашение GenesysForge' })).toBeVisible()
  await page.getByRole('button', { name: 'Аналитика', exact: true }).click()
  await page.getByRole('button', { name: 'Принять' }).click()
  await expect.poll(() => trackers).toBe(1)
  await expect.poll(() => page.evaluate(() => localStorage.getItem('ariadne.anonymous'))).not.toBeNull()
  await page.getByRole('button', { name: 'Аналитика', exact: true }).click()
  await Promise.all([page.waitForEvent('load'), page.getByRole('button', { name: 'Отказаться' }).click()])
  await expect(page.getByRole('heading', { name: 'Пользовательское соглашение GenesysForge' })).toBeVisible()
  expect(await page.evaluate(() => [localStorage.getItem('genesysforge.analytics-consent'), localStorage.getItem('ariadne.anonymous'), sessionStorage.getItem('ariadne.session'), sessionStorage.getItem('ariadne.activity')]))
    .toEqual(['denied', null, null, null])
  expect(trackers).toBe(1)
  expect(fontRequests).toEqual([])
  await page.goto('/register')
  await expect(page.getByText('Регистрируясь, вы принимаете', { exact: false })).toBeVisible()
})
