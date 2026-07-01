export type GameSystem = 'genesysCore' | 'realmsOfTerrinoth'
export type Characteristic = 'brawn' | 'agility' | 'intellect' | 'cunning' | 'willpower' | 'presence'
export type SkillKind = 'general' | 'combat' | 'social' | 'knowledge' | 'magic'
export type TalentCategory = 'general' | 'social' | 'combat' | 'magic'
export type ItemKind = 'weapon' | 'armor' | 'gear'
export type ItemState = 'equipped' | 'carried' | 'backpack'

export interface AuthResponse {
  token: string
  userId: string
  email: string
  displayName: string
}

export interface AuthProviders {
  /** client id Google OAuth, либо null/пусто, если вход через Google не настроен */
  googleClientId: string | null
}

/** Профиль текущего пользователя (U-21). */
export interface Account {
  id: string
  email: string
  displayName: string
  avatarUrl: string | null
  createdAt: string
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
  category: TalentCategory
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
  /** Структурные качества (U-10): свойство+рейтинг, бэкфилнутые из строки properties у встроенных предметов. */
  qualities: ItemQualityRef[]
}

/** Структурное качество предмета: ссылка на справочник по коду + рейтинг. */
export interface ItemQualityRef {
  code: string
  nameRu: string
  nameEn: string
  rating: number | null
  hasRating: boolean
  isActive: boolean
  activationCost: string
}

/** Справочное качество предмета/заклинания (U-10). */
export interface Quality {
  id: string
  code: string
  nameEn: string
  nameRu: string
  kind: 'itemQuality' | 'spellAdditionalEffect'
  isActive: boolean
  hasRating: boolean
  activationCost: string
  category: string
  description: string
  safeDescription: string
  source: string
}

export interface HeroicAbilityUpgrade {
  level: number // 1 — улучшенная, 2 — высшая
  cost: number
  description: string
  notes: string
}

export type RuleEffectKind =
  | 'manual' | 'healWounds' | 'healStrain' | 'adjustSoak' | 'adjustMeleeDefense' | 'adjustRangedDefense'
  | 'adjustWoundThreshold' | 'adjustStrainThreshold' | 'addBoostNextCheck' | 'addSetbackNextCheck' | 'spendStoryPoint'

export interface RuleEffect {
  kind: RuleEffectKind
  amount: number
  duration: string
  description: string
}

export interface HeroicAbility {
  id: string
  code: string
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
  effects: RuleEffect[]
}

export interface ActivateAbilityResult {
  session: GameSession
  abilityName: string
  applied: string[]
  manual: string[]
}

export interface ActivateCharacterAbilityResult {
  sheet: CharacterSheet
  abilityName: string
  applied: string[]
  manual: string[]
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

export type ArchetypeAbilityAutomationKind = 'passive' | 'activationCost' | 'timedEffect' | 'manual' | 'requiresGmDecision'

export interface ArchetypeAbility {
  code: string
  nameRu: string
  nameEn: string
  safeDescription: string
  automationKind: ArchetypeAbilityAutomationKind
}

export interface ArchetypeStartingSkill {
  skillName: string
  nameRu: string
  freeRanks: number
  isChoice: boolean
  choiceGroup: string
  choiceCount: number
}

export interface ArchetypeSkillChoice {
  choiceGroup: string
  skillNames: string[]
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
  isCustom: boolean
  abilities: ArchetypeAbility[]
  startingSkills: ArchetypeStartingSkill[]
}

export type CareerRuleKind = 'advisory' | 'skillSubstitution'

export interface CareerStartingGear {
  itemCode: string
  itemNameRu: string
  quantity: number
  isChoice: boolean
  choiceGroup: string
  choiceOption: number
}

export interface CareerRule {
  code: string
  kind: CareerRuleKind
  description: string
}

export interface CareerGearChoice {
  choiceGroup: string
  optionIndex: number
}

export interface Career {
  id: string
  name: string
  nameRu: string
  description: string
  safeDescription: string
  source: string
  isCustom: boolean
  careerSkillNames: string[]
  startingMoneyFixed: number
  startingMoneyDice: string
  startingGear: CareerStartingGear[]
  rules: CareerRule[]
}

export interface Reference {
  archetypes: Archetype[]
  careers: Career[]
  skills: SkillDef[]
  talents: TalentDef[]
  items: ItemDef[]
  heroicAbilities: HeroicAbility[]
  qualities: Quality[]
}

export interface CustomArchetypeInput {
  system: GameSystem
  name: string
  nameRu?: string | null
  brawn: number
  agility: number
  intellect: number
  cunning: number
  willpower: number
  presence: number
  woundBase: number
  strainBase: number
  startingXp: number
  description?: string | null
  abilityNameRu?: string | null
  abilityDescription?: string | null
}

export interface CustomCareerInput {
  system: GameSystem
  name: string
  nameRu?: string | null
  description?: string | null
  careerSkillNames: string[]
  startingMoneyFixed: number
  startingMoneyDice?: string | null
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

export interface CharacterShareResponse {
  token: string
  path: string
}

export interface DicePool {
  ability: number
  proficiency: number
}

export interface SheetSkill {
  skillDefId: string
  name: string
  nameRu: string
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
  nameRu: string
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
export type CreatureTemplate = 'none' | 'undead' | 'beast' | 'dragon' | 'demon' | 'construct'

export interface NpcSkillEntry {
  name: string
  ranks: number
}

export interface NpcAbilityEntry {
  name: string
  description: string
}

export interface NpcAttackQualityEntry {
  qualityCode: string
  nameRu: string
  rating: number | null
}

export interface NpcAttackEntry {
  name: string
  skillName: string
  damage: string
  critical: string
  rangeBand: string
  notes: string
  qualities: NpcAttackQualityEntry[]
  /** Подпись оружия из снаряжения, из которого атака автосоздана. Пусто — кастомная (ручная) атака. */
  sourceWeapon: string
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
  isBuiltIn: boolean
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
  silhouette: number
  tactics: string
  visibility: NpcVisibility
  campaignId: string | null
  isMine: boolean
  isBuiltIn: boolean
  skills: NpcSkillEntry[]
  abilities: NpcAbilityEntry[]
  attacks: NpcAttackEntry[]
  talents: string[]
  equipment: string[]
  tags: string[]
  warnings: string[]
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
  silhouette: number
  tactics: string
  visibility: NpcVisibility
  campaignId: string | null
  skills: NpcSkillEntry[]
  abilities: NpcAbilityEntry[]
  attacks: NpcAttackEntry[]
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
  template?: CreatureTemplate
  magicSkill?: string | null
  environment?: string | null
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
  criticalInjuries: number
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

// Лог бросков стола (U-08). Pool/Result хранятся как JSON-снимки (см. utils/diceRoller.ts).
export interface RollLogEntry {
  id: string
  campaignId: string
  sessionId: string | null
  actorName: string
  label: string
  poolJson: string
  resultJson: string
  summary: string
  isSecret: boolean
  createdAt: string
}

export interface CreateRollRequest {
  actorName?: string | null
  label?: string | null
  poolJson: string
  resultJson: string
  summary?: string | null
  isSecret: boolean
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
  criticalInjuries?: number | null
  isActive?: boolean | null
  isDefeated?: boolean | null
  isHiddenFromPlayers?: boolean | null
  notes?: string | null
  initiativeSlotType?: InitiativeSlotType | null
}

export type EncounterType =
  | 'combat' | 'social' | 'exploration' | 'chase' | 'investigation' | 'travel' | 'hazard' | 'mixed' | 'custom'
export type ThreatLevel = 'trivial' | 'easy' | 'standard' | 'hard' | 'deadly'
export type SendToTableMode = 'replace' | 'append'

export interface EncounterParticipant {
  id: string
  characterId: string | null
  npcId: string | null
  displayName: string
  participantType: ParticipantType
  initiativeSide: InitiativeSlotType
  quantity: number
  notes: string
  startsHidden: boolean
  startsDefeated: boolean
  startingWoundsOverride: number | null
  startingStrainOverride: number | null
  order: number
}

export interface EncounterListItem {
  id: string
  name: string
  system: GameSystem
  type: EncounterType
  threatLevel: ThreatLevel
  isVisibleToPlayers: boolean
  participantCount: number
  tags: string[]
  createdAt: string
  updatedAt: string
}

export interface EncounterDetail {
  id: string
  campaignId: string
  name: string
  system: GameSystem
  type: EncounterType
  threatLevel: ThreatLevel
  isGm: boolean
  isVisibleToPlayers: boolean
  gmDescription: string | null
  playerDescription: string
  playerGoals: string
  npcGoals: string | null
  location: string
  environment: string
  complications: string | null
  rewards: string
  tags: string[]
  participants: EncounterParticipant[]
  createdAt: string
  updatedAt: string
}

export interface EncounterInput {
  name: string
  system: GameSystem
  type: EncounterType
  threatLevel: ThreatLevel
  gmDescription: string
  playerDescription: string
  playerGoals: string
  npcGoals: string
  location: string
  environment: string
  complications: string
  rewards: string
  isVisibleToPlayers: boolean
  tags: string[]
}

export interface AddEncounterParticipantRequest {
  characterId?: string | null
  npcId?: string | null
  displayName?: string | null
  participantType?: ParticipantType | null
  initiativeSide?: InitiativeSlotType | null
  quantity?: number | null
  notes?: string | null
  startsHidden?: boolean | null
  startsDefeated?: boolean | null
  startingWoundsOverride?: number | null
  startingStrainOverride?: number | null
}

export interface UpdateEncounterParticipantRequest {
  displayName?: string | null
  initiativeSide?: InitiativeSlotType | null
  quantity?: number | null
  notes?: string | null
  startsHidden?: boolean | null
  startsDefeated?: boolean | null
  startingWoundsOverride?: number | null
  startingStrainOverride?: number | null
}

export interface EncounterFilter {
  search?: string
  type?: EncounterType
  tag?: string
}

export type ContentEntryType =
  | 'archetype' | 'career' | 'skill' | 'talent' | 'item' | 'heroicAbility'
  | 'spell' | 'magicAction' | 'alchemyRecipe' | 'rune' | 'houseRule' | 'customNote'
export type AllowedState = 'allowed' | 'disallowed' | 'askGm'
export type HouseRuleCategory =
  | 'none' | 'characterCreation' | 'combat' | 'magic' | 'equipment' | 'xp' | 'campaignTone' | 'custom'

export interface ContentPackEntry {
  id: string
  contentType: ContentEntryType
  contentId: string | null
  title: string
  allowedState: AllowedState
  category: HouseRuleCategory
  safeSummary: string
  source: string
  pageRef: string
  gmNotes: string | null
  playerNotes: string
  tags: string[]
  sortOrder: number
}

export interface ContentPackListItem {
  id: string
  name: string
  system: GameSystem
  isPublicToCampaign: boolean
  entryCount: number
  updatedAt: string
}

export interface ContentPackDetail {
  id: string
  campaignId: string
  name: string
  description: string
  system: GameSystem
  isGm: boolean
  isPublicToCampaign: boolean
  entries: ContentPackEntry[]
  createdAt: string
  updatedAt: string
}

export interface ContentPackEntryInput {
  contentType: ContentEntryType
  contentId?: string | null
  title: string
  allowedState: AllowedState
  category?: HouseRuleCategory | null
  safeSummary?: string
  source?: string
  pageRef?: string
  gmNotes?: string
  playerNotes?: string
  tags?: string[]
}

export interface HomebrewPackListItem {
  id: string
  name: string
  description: string
  system: GameSystem
  isShared: boolean
  isEnabledByDefault: boolean
  entryCount: number
  updatedAt: string
}

export interface HomebrewPackShare {
  token: string
  path: string
}

export interface HomebrewPackImportResult {
  id: string
  name: string
  entryCount: number
}

export interface HomebrewPackDocument {
  format: 'genesysforge.homebrew-pack.v1'
  name: string
  description?: string | null
  system: GameSystem
  skills?: unknown[] | null
  talents?: unknown[] | null
  items?: unknown[] | null
  heroicAbilities?: unknown[] | null
  archetypes?: unknown[] | null
  careers?: unknown[] | null
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
  // Мотивации и предыстория (U-22)
  desire: string | null
  fear: string | null
  strength: string | null
  flaw: string | null
  background: string | null
  // Критические ранения (U-23)
  criticalInjuries: CriticalInjury[]
}

/** Критическое ранение персонажа (U-23). */
export interface CriticalInjury {
  id: string
  ruleCode: string | null
  nameRu: string
  severity: string | null
  rollResult: number | null
  notes: string | null
}

/** Опциональные текстовые поля мотиваций/предыстории (U-22) для create/update. */
export interface CharacterBio {
  desire?: string
  fear?: string
  strength?: string
  flaw?: string
  background?: string
}

// ── История персонажа / audit log (U-09) ──

export type CharacterAuditAction =
  | 'xpAwarded' | 'characteristicBought' | 'characteristicRefunded'
  | 'skillRankBought' | 'skillRankRefunded' | 'talentBought' | 'talentRefunded'
  | 'itemBought' | 'itemSold' | 'itemRemoved'
  | 'heroicAbilityChanged' | 'creationCompleted' | 'manualEdit'

export interface CharacterAuditEntry {
  id: string
  createdAt: string
  action: CharacterAuditAction
  summary: string
  xpDelta: number | null
  totalXpAfter: number
  spentXpAfter: number
}

// ── Экспорт / импорт персонажа (формат genesysforge.character.v1) ──

/** Переносимый JSON персонажа. Структура совпадает с серверным CharacterExportDto. */
export interface CharacterExport {
  format: string
  exportedAt: string
  character: unknown
}

export interface ImportPreview {
  name: string
  system: GameSystem
  archetypeName: string
  careerName: string
  totalXp: number
  spentXp: number
  skillCount: number
  talentCount: number
  itemCount: number
  noteCount: number
  warnings: string[]
}

export interface ImportResult {
  characterId: string
  name: string
  warnings: string[]
}

// Справочные таблицы правил (U-11).
export type RuleTableKind = 'difficulty' | 'symbolSpend' | 'rangeBand' | 'criticalInjury'

export interface RuleTableEntry {
  id: string
  kind: RuleTableKind
  code: string
  nameRu: string
  nameEn: string
  groupRu: string
  sortOrder: number
  rollRange: string
  symbolCost: string
  body: string
  notes: string
  source: string
  sourcePage: string
}

export interface RulesResponse {
  entries: RuleTableEntry[]
}

// Глобальный поиск (U-11).
export interface SearchHit {
  type: string
  group: string
  title: string
  subtitle: string
  snippet: string
  route: string
}

export interface SearchResponse {
  hits: SearchHit[]
}
