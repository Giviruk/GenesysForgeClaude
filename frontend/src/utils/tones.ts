/** Visual catalogue from redesign v5, appendix C. No game rules belong here. */
export type Tone = 'blue' | 'green' | 'orange' | 'red' | 'purple' | 'grey' | 'teal' | 'navy'

const catalogue: Record<string, Record<string, Tone>> = {
  tier: { '1': 'blue', '2': 'green', '3': 'orange', '4': 'red', '5': 'purple' },
  activation: { passive: 'grey', incidental: 'orange', action: 'blue', maneuver: 'green', manoeuvre: 'green' },
  item: { weapon: 'red', armor: 'blue', gear: 'green', consumable: 'purple' },
  shop: { all: 'orange', weaponLight: 'red', weaponHeavy: 'red', weaponRanged: 'red',
    magicImplement: 'purple', magicItem: 'purple', weaponAttachment: 'orange', weaponEnchantment: 'orange',
    armor: 'blue', armorAttachment: 'blue', transport: 'teal', gear: 'green', consumable: 'navy', service: 'grey' },
  magic: { arcana: 'purple', divine: 'orange', primal: 'green', runes: 'teal' },
  heroic: { power: 'red', duration: 'blue', frequency: 'green', story: 'orange' },
  crafting: { crafting: 'orange', alchemy: 'green', enchanting: 'purple', success: 'green', failure: 'red', cancelled: 'grey' },
  transport: { mount: 'blue', vehicle: 'orange' },
  motivation: { desire: 'green', fear: 'purple', strength: 'blue', flaw: 'red' },
  visibility: { private: 'orange', shared: 'blue', gm: 'red', all: 'blue' },
  history: { xp: 'green', skill: 'blue', talent: 'orange', item: 'teal', purchase: 'teal', wounds: 'red', creation: 'grey' },
  npc: { minion: 'green', rival: 'blue', nemesis: 'red' },
  participant: { pc: 'blue', minion: 'green', rival: 'orange', nemesis: 'red', npc: 'red' },
  scene: { combat: 'red', social: 'purple', exploration: 'teal', chase: 'orange' },
  range: { engaged: 'red', short: 'blue', medium: 'teal', long: 'orange', extreme: 'grey' },
  result: { success: 'green', failure: 'red', triumph: 'orange' },
  content: { conflict: 'red', proposal: 'purple', update: 'orange', enabled: 'green', gear: 'teal', talent: 'orange' },
  role: { brute: 'red', skirmisher: 'green', ranged: 'teal', mage: 'purple', leader: 'orange',
    social: 'navy', support: 'red', monster: 'grey', custom: 'grey' },
  system: { genesysCore: 'blue', realmsOfTerrinoth: 'purple' },
}

export function entityTone(entity: string, value: string | number): Tone {
  const entries = catalogue[entity]
  const key = String(value)
  return entries?.[key] ?? entries?.[key.toLowerCase()] ?? 'grey'
}

export function toneClass(entity: string, value: string | number): string {
  return `tone-${entityTone(entity, value)}`
}

export function rarityClass(rarity: number): string {
  return rarity <= 2 ? 'rarity-common' : rarity === 3 ? 'rarity-uncommon' : rarity <= 5 ? 'rarity-rare' : 'rarity-exotic'
}
