import { expect, test } from '@playwright/test'
import type { NpcDetail, Reference, RuleTableEntry } from '../src/api/types'
test.use({ serviceWorkers: 'block' })

const reference: Reference = {
  archetypes: [], careers: [], talents: [], heroicAbilities: [], heroicSecondaryEffects: [],
  items: [], qualities: [], attachments: [], mounts: [],
  skills: [{ id: 'brawl', name: 'Brawl', nameRu: 'Грубая сила', characteristic: 'brawn',
    kind: 'combat', safeDescription: '', source: '', isCustom: false },
  { id: 'ranged', name: 'Ranged (Light)', nameRu: 'Дальний бой (лёгкое оружие)', characteristic: 'agility',
    kind: 'combat', safeDescription: '', source: '', isCustom: false },
  { id: 'melee', name: 'Melee', nameRu: 'Ближний бой', characteristic: 'brawn',
    kind: 'combat', safeDescription: '', source: '', isCustom: false }],
}
const npc: NpcDetail = {
  id: 'symbol-preview', name: 'Genesys — символы', system: 'genesysCore', kind: 'rival', role: 'brute',
  description: 'А потом он сбегает. Капитан А. Иванов. * пометка @. ✶ ✸ ▲ ▼ ★ ☠ · ◻ ■ ♦ ⬣', source: '',
  brawn: 3, agility: 2, intellect: 2, cunning: 2, willpower: 2, presence: 2,
  woundThreshold: 12, strainThreshold: null, soak: 3, meleeDefense: 0, rangedDefense: 0,
  silhouette: 1, tactics: '', visibility: 'publicTemplate', campaignId: null,
  isMine: true, isBuiltIn: false, skills: [{ name: 'Рукопашный бой', ranks: 2 }, { name: 'Дальний бой (лёгкий)', ranks: 2 }],
  attacks: [{ name: 'Кулаки', skillName: 'Brawl', damage: '+0', critical: '5',
    rangeBand: 'Engaged', notes: '', qualities: [], sourceWeapon: '' },
  { name: 'Клинок', skillName: 'Melee (Light)', damage: '+1', critical: '3',
    rangeBand: 'Engaged', notes: '', qualities: [], sourceWeapon: '' }],
  abilities: [], talents: [], equipment: [], tags: [], warnings: [], createdAt: '', updatedAt: '',
}
const cost: RuleTableEntry = {
  id: 'cost', kind: 'symbolSpend', code: 'symbol-preview', nameRu: 'Пример траты', nameEn: 'Example spend',
  groupRu: 'Бой', groupEn: 'Combat', sortOrder: 0, rollRange: '', symbolCost: '2 Threat или 1 Despair',
  body: 'Символы проверки: ✶ ✸ ▲ ▼ ★ ☠.', bodyEn: 'Check symbols: ✶ ✸ ▲ ▼ ★ ☠.',
  notes: '', source: '', sourcePage: '',
}

// Isolated UI fixtures: no account, campaign or database changes are required.
for (const lang of ['ru', 'en'] as const) {
  for (const width of [390, 768, 1280]) {
    test(`Genesys glyphs, book labels and print (${lang}, ${width}px)`, async ({ page }, testInfo) => {
      await page.setViewportSize({ width, height: 1000 })
      await page.addInitScript(lang => {
        localStorage.setItem('genesysforge.lang', lang)
        localStorage.setItem('genesysforge.token', 'isolated-symbol-ui-test')
      }, lang)
      await page.route('**/api/**', route => {
        const path = new URL(route.request().url()).pathname
        if (!path.startsWith('/api/')) return route.continue()
        const json = path.endsWith('/rules') ? { entries: [cost] }
          : path.includes('/reference/') ? reference
          : path.endsWith('/npcs/symbol-preview') ? npc
          : path.endsWith('/account') ? { id: 'test', displayName: 'Preview', email: 'preview@example.test' }
          : []
        return route.fulfill({ json })
      })
      await page.goto('/npcs/symbol-preview')
      await expect(page.getByRole('heading', { name: npc.name, exact: true })).toBeVisible()
      await expect(page.locator('.npc-card')).toBeVisible()
      await expect(page.getByText(lang === 'ru' ? 'Сила' : 'Brawn', { exact: true })).toBeVisible()
      await expect(page.locator('.npc-skill-name').filter({ hasText: lang === 'ru' ? 'Грубая сила' : 'Brawl' })).toBeVisible()
      await expect(page.locator('.npc-skill-name').filter({ hasText: lang === 'ru' ? 'Дальний бой (лёгкое оружие)' : 'Ranged (Light)' })).toBeVisible()

      const loaded = await page.evaluate(async () => {
        await document.fonts.load('24px "Genesys Symbols"')
        return document.fonts.check('24px "Genesys Symbols"')
      })
      expect(loaded).toBe(true)
      if (width < 760) await page.getByRole('tab', { name: lang === 'ru' ? 'Способности и снаряжение' : 'Abilities and gear', exact: true }).click()
      const description = page.locator('.npc-desc:visible').first()
      await expect(description.locator('.genesys-symbol')).toHaveCount(10)
      await expect(description).toContainText('А потом он сбегает. Капитан А. Иванов. * пометка @.')
      for (const label of lang === 'ru'
        ? ['Успех', 'Провал', 'Преимущество', 'Осложнение', 'Триумф', 'Крах']
        : ['Success', 'Failure', 'Advantage', 'Threat', 'Triumph', 'Despair']) {
        await expect(description.getByRole('img', { name: label, exact: true })).toBeVisible()
      }
      expect(await description.locator('.genesys-symbol').evaluateAll(nodes => nodes.every(node =>
        getComputedStyle(node).fontFamily.includes('Genesys Symbols')
        && getComputedStyle(node).fontWeight === '400'
        && node.getBoundingClientRect().width > 0))).toBe(true)
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBeLessThanOrEqual(1)
      await page.screenshot({ path: testInfo.outputPath(`glyphs-${lang}-${width}.png`), fullPage: true })

      if (width < 760) await page.getByRole('tab', { name: /Атаки|Attacks/ }).click()
      await page.getByRole('button', { name: lang === 'ru' ? 'Атаковать' : 'Attack', exact: true }).first().click()
      const drawer = page.getByRole('complementary', { name: lang === 'ru' ? 'Дайсроллер' : 'Dice roller' })
      await expect(drawer.locator('.dr-die-glyph')).toHaveCount(6)
      await expect(drawer).toContainText(lang === 'ru' ? 'Вплотную' : 'Engaged')
      await expect(drawer.getByRole('img', { name: lang === 'ru' ? 'Кость бонуса' : 'Boost die', exact: true })).toBeVisible()
      await expect(drawer.getByRole('img', { name: lang === 'ru' ? 'Кость штрафа' : 'Setback die', exact: true })).toBeVisible()
      await page.screenshot({ path: testInfo.outputPath(`roller-${lang}-${width}.png`), fullPage: true })
      await drawer.getByRole('button', { name: lang === 'ru' ? 'Закрыть' : 'Close', exact: true }).click()

      await page.getByRole('button', { name: lang === 'ru' ? 'Печать' : 'Print', exact: true }).filter({ visible: true }).click()
      const card = page.locator('.print-card')
      await expect(card).toContainText(lang === 'ru' ? 'Грубая сила 2' : 'Brawl 2')
      await expect(card).toContainText(lang === 'ru' ? 'Вплотную' : 'Engaged')
      await expect(card).toContainText(lang === 'ru' ? 'навык Ближний бой' : 'skill Melee')
      await expect(card).not.toContainText('Melee (Light)')
      await expect(card.locator('.genesys-symbol')).toHaveCount(10)
      await page.emulateMedia({ media: 'print' })
      expect(await card.locator('.genesys-symbol').evaluateAll(nodes => nodes.every(node =>
        getComputedStyle(node).fontFamily.includes('Genesys Symbols')
        && node.getBoundingClientRect().width > 0))).toBe(true)
      await page.screenshot({ path: testInfo.outputPath(`print-${lang}-${width}.png`), fullPage: true })
      await page.emulateMedia({ media: 'screen' })
      await page.getByRole('button', { name: lang === 'ru' ? '← Назад' : '← Back', exact: true }).click()

      await page.goto('/reference')
      await page.getByRole('tab', { name: lang === 'ru' ? /^Траты \(/ : /^Spends \(/ }).click()
      const row = page.locator('.ref-cost').first()
      await expect(row.getByRole('img', { name: lang === 'ru' ? 'Осложнение' : 'Threat', exact: true })).toBeVisible()
      await expect(row.getByRole('img', { name: lang === 'ru' ? 'Крах' : 'Despair', exact: true })).toBeVisible()
      await expect(row).toContainText('2')
      await expect(row).toContainText('1')
      await expect(row).toContainText(lang === 'ru' ? 'или' : 'or')
      if (lang === 'en') await expect(row).not.toContainText('или')
    })
  }
}
