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
  nameRu: string
  characteristic: Characteristic
  kind: SkillKind
  safeDescription: string
  source: string
  isCustom: boolean
}

export interface TalentDef {
  id: string
  name: string
  nameRu: string
  tier: number
  isRanked: boolean
  setting: string
  activation: string
  description: string
  safeDescription: string
  source: string
  woundBonus: number
  strainBonus: number
  soakBonus: number
  meleeDefenseBonus: number
  rangedDefenseBonus: number
  isCustom: boolean
  grantsCharacteristic: boolean
}

export interface ItemDef {
  id: string
  name: string
  nameRu: string
  kind: ItemKind
  encumbrance: number
  soakBonus: number
  meleeDefense: number
  rangedDefense: number
  encumbranceThresholdBonus: number
  description: string
  safeDescription: string
  source: string
  price: number
  rarity: number
  skillName: string
  damage: string
  crit: string
  rangeBand: string
  properties: string
  isCustom: boolean
}

export interface HeroicAbilityUpgrade {
  level: number // 1 — Improved, 2 — Supreme
  cost: number
  description: string
  notes: string
}

export interface HeroicAbility {
  id: string
  name: string
  nameRu: string
  description: string
  safeDescription: string
  source: string
  isCustom: boolean
  requirement: string
  activationCost: string
  activation: string
  duration: string
  frequency: string
  notes: string
  upgrades: HeroicAbilityUpgrade[]
}

export type SpellEntryKind = 'effect' | 'additionalEffect'

export interface Spell {
  id: string
  magicSkill: string
  kind: SpellEntryKind
  parentEffect: string
  nameRu: string
  nameEn: string
  difficulty: string
  description: string
  safeDescription: string
  source: string
  isCustom: boolean
}

export interface Archetype {
  id: string
  name: string
  nameRu: string
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
  safeDescription: string
  source: string
}

export interface Career {
  id: string
  name: string
  nameRu: string
  description: string
  safeDescription: string
  source: string
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
  nameRu: string
  tier: number
  isRanked: boolean
  ranks: number
  activation: string
  description: string
  woundBonus: number
  strainBonus: number
  soakBonus: number
  meleeDefenseBonus: number
  rangedDefenseBonus: number
  grantsCharacteristic: boolean
  grantedCharacteristics: Characteristic[]
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
  price: number
  skillName: string
  damage: string
  crit: string
  rangeBand: string
  properties: string
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

export interface CampaignListItem {
  id: string
  name: string
  isGm: boolean
  characterCount: number
  createdAt: string
}

export interface CampaignMember {
  characterId: string
  characterName: string
  system: GameSystem
  archetype: string
  career: string
  isMine: boolean
}

export interface CampaignNote {
  id: string
  title: string
  body: string
  isPrivate: boolean
  createdAt: string
  updatedAt: string
}

export interface CampaignDetail {
  id: string
  name: string
  description: string
  isGm: boolean
  joinCode: string | null
  members: CampaignMember[]
  notes: CampaignNote[]
}

export interface CharacterNote {
  id: string
  title: string
  body: string
  createdAt: string
  updatedAt: string
}

export type NpcKind = 'minion' | 'rival' | 'nemesis'
export type NpcRole =
  | 'brute' | 'skirmisher' | 'archer' | 'caster' | 'leader' | 'social' | 'support' | 'monster' | 'custom'
export type NpcVisibility = 'private' | 'campaignVisible' | 'publicTemplate'
export type NpcPowerLevel = 'weak' | 'standard' | 'strong' | 'elite'
export type NpcCombatStyle = 'melee' | 'ranged' | 'magic' | 'social'

export interface NpcSkillEntry {
  name: string
  ranks: number
}

export interface NpcAbilityEntry {
  name: string
  description: string
}

export interface NpcListItem {
  id: string
  name: string
  system: GameSystem
  kind: NpcKind
  role: NpcRole
  soak: number
  woundThreshold: number
  strainThreshold: number | null
  visibility: NpcVisibility
  campaignId: string | null
  isMine: boolean
  skills: NpcSkillEntry[]
  tags: string[]
  createdAt: string
}

export interface NpcDetail {
  id: string
  name: string
  system: GameSystem
  kind: NpcKind
  role: NpcRole
  description: string
  source: string
  brawn: number
  agility: number
  intellect: number
  cunning: number
  willpower: number
  presence: number
  woundThreshold: number
  strainThreshold: number | null
  soak: number
  meleeDefense: number
  rangedDefense: number
  visibility: NpcVisibility
  campaignId: string | null
  isMine: boolean
  skills: NpcSkillEntry[]
  abilities: NpcAbilityEntry[]
  talents: string[]
  equipment: string[]
  tags: string[]
  createdAt: string
  updatedAt: string
}

export interface NpcInput {
  name: string
  system: GameSystem
  kind: NpcKind
  role: NpcRole
  description: string
  source: string
  brawn: number
  agility: number
  intellect: number
  cunning: number
  willpower: number
  presence: number
  woundThreshold: number
  strainThreshold: number | null
  soak: number
  meleeDefense: number
  rangedDefense: number
  visibility: NpcVisibility
  campaignId: string | null
  skills: NpcSkillEntry[]
  abilities: NpcAbilityEntry[]
  talents: string[]
  equipment: string[]
  tags: string[]
}

export interface QuickDraftRequest {
  system: GameSystem
  kind: NpcKind
  role: NpcRole
  powerLevel: NpcPowerLevel
  primaryCharacteristic: Characteristic | null
  combatStyle: NpcCombatStyle
  name: string | null
}

export interface NpcFilter {
  search?: string
  system?: GameSystem
  kind?: NpcKind
  role?: NpcRole
  campaignId?: string
  tag?: string
  sort?: 'name' | 'createdAt'
}

export type ParticipantType = 'playerCharacter' | 'npc' | 'minionGroup' | 'hazard'
export type InitiativeSlotType = 'player' | 'npc' | 'neutral'

export interface GameParticipant {
  id: string
  characterId: string | null
  npcId: string | null
  displayName: string
  participantType: ParticipantType
  initiativeSlotType: InitiativeSlotType
  count: number
  woundsCurrent: number
  woundsThreshold: number
  strainCurrent: number
  strainThreshold: number | null
  soak: number
  meleeDefense: number
  rangedDefense: number
  isActive: boolean
  isDefeated: boolean
  isHiddenFromPlayers: boolean
  notes: string
  order: number
}

export interface InitiativeSlot {
  id: string
  slotType: InitiativeSlotType
  order: number
  assignedParticipantId: string | null
  notes: string
}

export interface GameSession {
  id: string
  campaignId: string
  name: string
  description: string
  isActive: boolean
  isGm: boolean
  allowPlayerEdits: boolean
  playerStoryPoints: number
  gmStoryPoints: number
  currentRound: number
  currentTurnIndex: number
  publicNotes: string
  gmNotes: string | null
  participants: GameParticipant[]
  slots: InitiativeSlot[]
}

export interface AddParticipantRequest {
  characterId?: string | null
  npcId?: string | null
  displayName?: string | null
  participantType?: ParticipantType | null
  initiativeSlotType?: InitiativeSlotType | null
  count?: number | null
  woundsThreshold?: number | null
  strainThreshold?: number | null
  soak?: number | null
  meleeDefense?: number | null
  rangedDefense?: number | null
}

export interface UpdateParticipantRequest {
  displayName?: string | null
  woundsCurrent?: number | null
  woundsThreshold?: number | null
  strainCurrent?: number | null
  strainThreshold?: number | null
  soak?: number | null
  meleeDefense?: number | null
  rangedDefense?: number | null
  isActive?: boolean | null
  isDefeated?: boolean | null
  isHiddenFromPlayers?: boolean | null
  notes?: string | null
  initiativeSlotType?: InitiativeSlotType | null
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
  money: number
  derived: Derived
  skills: SheetSkill[]
  talents: SheetTalent[]
  talentTierCounts: Record<string, number>
  heroicAbility: HeroicAbility | null
  heroicUpgradeRank: number
  heroicUpgradePointsTotal: number
  heroicUpgradePointsSpent: number
  items: SheetItem[]
}
