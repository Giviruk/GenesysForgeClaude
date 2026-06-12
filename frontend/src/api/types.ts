export type GameSystem = 'genesysCore' | 'realmsOfTerrinoth'
export type Characteristic = 'brawn' | 'agility' | 'intellect' | 'cunning' | 'willpower' | 'presence'
export type SkillKind = 'general' | 'combat' | 'social' | 'knowledge' | 'magic'
export type ItemKind = 'weapon' | 'armor' | 'gear'
export type ItemState = 'equipped' | 'carried' | 'backpack'

export interface AuthResponse {
  token: string
  userId: string
  email: string
  displayName: string
}

export interface SkillDef {
  id: string
  name: string
  characteristic: Characteristic
  kind: SkillKind
  isCustom: boolean
}

export interface TalentDef {
  id: string
  name: string
  tier: number
  isRanked: boolean
  activation: string
  description: string
  woundBonus: number
  strainBonus: number
  soakBonus: number
  meleeDefenseBonus: number
  rangedDefenseBonus: number
  isCustom: boolean
}

export interface ItemDef {
  id: string
  name: string
  kind: ItemKind
  encumbrance: number
  soakBonus: number
  meleeDefense: number
  rangedDefense: number
  encumbranceThresholdBonus: number
  description: string
  price: number
  rarity: number
  isCustom: boolean
}

export interface HeroicAbility {
  id: string
  name: string
  description: string
  isCustom: boolean
}

export interface Archetype {
  id: string
  name: string
  brawn: number
  agility: number
  intellect: number
  cunning: number
  willpower: number
  presence: number
  woundBase: number
  strainBase: number
  startingXp: number
  description: string
}

export interface Career {
  id: string
  name: string
  description: string
  careerSkillNames: string[]
}

export interface Reference {
  archetypes: Archetype[]
  careers: Career[]
  skills: SkillDef[]
  talents: TalentDef[]
  items: ItemDef[]
  heroicAbilities: HeroicAbility[]
}

export interface CharacterListItem {
  id: string
  name: string
  system: GameSystem
  archetype: string
  career: string
  isCreationPhase: boolean
  createdAt: string
}

export interface DicePool {
  ability: number
  proficiency: number
}

export interface SheetSkill {
  skillDefId: string
  name: string
  kind: SkillKind
  characteristic: Characteristic
  ranks: number
  isCareer: boolean
  pool: DicePool
  nextRankCost: number
  freeRanks: number
}

export interface SheetTalent {
  talentDefId: string
  name: string
  tier: number
  isRanked: boolean
  ranks: number
  activation: string
  description: string
}

export interface SheetItem {
  id: string
  itemDefId: string
  name: string
  kind: ItemKind
  state: ItemState
  quantity: number
  encumbrance: number
  soakBonus: number
  meleeDefense: number
  rangedDefense: number
  encumbranceThresholdBonus: number
  load: number
  description: string
}

export interface Derived {
  woundThreshold: number
  strainThreshold: number
  soak: number
  meleeDefense: number
  rangedDefense: number
  encumbranceThreshold: number
  encumbranceLoad: number
  encumbered: boolean
}

export interface CharacterSheet {
  id: string
  name: string
  system: GameSystem
  archetype: Archetype
  career: Career
  characteristics: Record<Characteristic, number>
  totalXp: number
  spentXp: number
  availableXp: number
  isCreationPhase: boolean
  woundsCurrent: number
  strainCurrent: number
  derived: Derived
  skills: SheetSkill[]
  talents: SheetTalent[]
  talentTierCounts: Record<string, number>
  heroicAbility: HeroicAbility | null
  items: SheetItem[]
}
