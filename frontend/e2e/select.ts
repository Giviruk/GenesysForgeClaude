import { expect, type Locator } from '@playwright/test'

export async function chooseSelect(field: Locator, value: string) {
  // The archived before-screenshots still use the previous native control.
  if (process.env.DESIGN_BASELINE === '1' && await field.evaluate(el => el.tagName === 'SELECT')) {
    await field.selectOption(value)
    return
  }
  await field.click()
  const id = await field.getAttribute('aria-controls')
  const list = field.page().locator(`[id=${JSON.stringify(id)}]`)
  await expect(list).toBeVisible()
  await list.locator(`[role=option][data-value=${JSON.stringify(value)}]`).click()
  // Action pickers (add talent/quality) intentionally reset to their placeholder.
  // Each caller checks its resulting state; the common interaction waits for dismissal.
  await expect(list).toBeHidden()
}
