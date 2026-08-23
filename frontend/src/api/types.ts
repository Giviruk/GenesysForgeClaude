export type GameSystem = 'genesysCore' | 'realmsOfTerrinoth'
export type Characteristic = 'brawn' | 'agility' | 'intellect' | 'cunning' | 'willpower' | 'presence'
export type SkillKind = 'general' | 'combat' | 'social' | 'knowledge' | 'magic'
export type TalentCategory = 'general' | 'social' | 'combat' | 'magic'
export type ItemKind = 'weapon' | 'armor' | 'gear'
export type ItemState = 'equipped' | 'carried' | 'backpack'
export type ShopItemCategory =
  | 'weaponLight' | 'weaponHeavy' | 'weaponRanged' | 'magicImplement' | 'magicItem'
  | 'armor' | 'transport' | 'gear' | 'consumable' | 'service'

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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
  source: string
  woundBonus: number
  strainBonus: number
  soakBonus: number
  meleeDefenseBonus: number
  rangedDefenseBonus: number
  isCustom: boolean
  grantsCharacteristic: boolean
  /** Английская подпись тайминга активации — стабильнее локализованной строки. */
  activationEn: string
  /** Талант применим вне своего хода (Out-of-turn Incidental, ROT-TAL-01). */
  canUseOutOfTurn: boolean
  /** Навыки, которые талант делает карьерными, пока принадлежит персонажу (ROT-TAL-04). */
  careerSkillNames: string[]
  /** Bare-slug код таланта — ключ связей prerequisite/exclusion, общий для обеих систем. */
  linkCode: string
  /** Обязательный талант-предусловие, bare-slug код; пусто — предусловий нет (ROT-TAL-02). */
  requiresTalentCode: string
  /** Коды несовместимых талантов; отношение симметрично. */
  excludesTalentCodes: string[]
  /** Лимит применений и область его сброса; стоимость активации (ROT-TAL-05). */
  usesPerScope: number
  useScope: AbilityUseScope
  storyPointCost: number
  strainCost: number
  trigger: string
  /** Схема обязательного выбора при покупке ранга (ROT-TAL-03). */
  choiceKind: TalentChoiceKind
  choiceCountFirstRank: number
  choiceCountNextRank: number
}

/** Что выбирает игрок при покупке ранга таланта (ROT-TAL-03). */
export type TalentChoiceKind =
  | 'none' | 'characteristic' | 'skill' | 'spellConfiguration' | 'animalCompanion'

/** Один сохранённый выбор ранга таланта. */
export interface CharacterTalentChoice {
  rankIndex: number
  kind: TalentChoiceKind
  value: string
  displayName: string
}

export interface ItemDef {
  id: string
  /** Стабильный код встроенной записи; пусто у старого custom content. */
  code: string
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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
  source: string
  price: number | null
  rarity: number | null
  skillName: string
  damage: string
  crit: string
  rangeBand: string
  properties: string
  isCustom: boolean
  /** Структурные качества (U-10): свойство+рейтинг, бэкфилнутые из строки properties у встроенных предметов. */
  qualities: ItemQualityRef[]
  /** Слоты улучшений по таблице книги; null — книжного значения у записи нет (ROT-WPN-01/ROT-ARM-01). */
  hardPoints: number | null
  /** Влияние предмета на проверки навыков (ROT-ARM-01). */
  checkModifiers: ItemCheckModifier[]
  /** Типизированные профили атаки (ROT-WPN-01); пусто у не-оружия. */
  attackProfiles: WeaponAttackProfile[]
  /** Магический инструмент (ROT-MAG-IMP-01); null — запись инструментом не является. */
  implement: ImplementSpec | null
  /** Runebound shard (ROT-MAG-11); отдельный implement для навыка Runes. */
  shard: RuneboundShardSpec | null
  /** Есть обычная книжная цена и предмет можно купить через магазин. */
  purchasable: boolean
  /** Предмет можно продать по обычной экономике. */
  sellable: boolean
  /** Серверная категория общей витрины. */
  shopCategory: ShopItemCategory
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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
  source: string
}

export interface HeroicAbilityUpgrade {
  level: number // 1 — улучшенная, 2 — высшая
  cost: number
  description: string
  /** Английское описание; пусто, если не переведено. */
  descriptionEn?: string
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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
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

/** Параметр, который primary effect требует выбрать вместе с собой (ROT-HA-02). */
export type HeroicParameterKind = 'none' | 'paragonSkill' | 'sixthSenseSubject' | 'signatureWeapon'

export type SignatureWeaponProfile = 'brawl' | 'oneHanded' | 'twoHanded' | 'ranged'

/**
 * Качество изготовления экземпляра (ROT-WPN-02). Порядок значений — исторический: гномья работа
 * была первой ещё в именном оружии (ROT-HA-02).
 */
export type WeaponCraftsmanship = 'dwarven' | 'elven' | 'steel' | 'iron' | 'ancient'

/** Один признак формы оружия. На проводе флаги едут строкой «oneHanded, sword». */
export type WeaponFormTrait =
  | 'brawl' | 'oneHanded' | 'twoHanded' | 'ranged'
  | 'sword' | 'bowOrCrossbow' | 'bladed' | 'bluntOrCrushing'
  | 'hasCuttingEdge' | 'woodenWorkingEdge'
  // Признаки брони — совместимость улучшений считается тем же набором (ROT-EQP-ATT-01).
  | 'plateArmor' | 'metalArmor' | 'hardenedPlate'

/** Именное оружие: числа приходят с сервера из выбранного профиля. */
export interface SignatureWeapon {
  profile: SignatureWeaponProfile
  craftsmanship: WeaponCraftsmanship
  narrativeForm: string
  formTraits: string
  isLost: boolean
  skillName: string
  damage: string
  crit: number
  rangeBand: string
  encumbrance: number
  hardPoints: number
  /** Качества профиля вместе с теми, что даёт базовое улучшение. */
  qualities: ItemQualityRef[]
  /**
   * Базовое улучшение оружия (ROT-HA-02): временное, действует только вместе со способностью,
   * не покупается и не занимает слотов. `null` — старый персонаж, который его ещё не выбрал.
   */
  baseAttachment: SignatureBaseAttachment | null
  /** Выбор Improved: Укреплённое либо древняя работа (ROT-HA-05). */
  improvement: SignatureWeaponImprovement
  /** Бесплатное улучшение Supreme: установлено постоянно и занимает слоты. */
  supremeAttachment: SignatureBaseAttachment | null
  /** Работа выбрана вне нынешнего списка способности — у персонажа, созданного до правила. */
  craftsmanshipOutOfRules: boolean
}

/** Что даёт Improved именного оружия: ровно одно из двух, навсегда (ROT-HA-05). */
export type SignatureWeaponImprovement = 'none' | 'reinforced' | 'ancient' 

/** Базовое улучшение именного оружия. Цены и слотов у героической копии нет — их и не приходит. */
export interface SignatureBaseAttachment {
  defId: string
  code: string
  name: string
  nameRu: string
  description: string
  effects: AttachmentEffect[]
}

/** Параметр primary effect на листе персонажа. */
export interface HeroicConfiguration {
  kind: HeroicParameterKind
  paragonSkillDefId: string | null
  paragonSkillName: string | null
  /** Навык Paragon больше не виден персонажу: снимок имени сохранён, замена не подставляется. */
  paragonSkillMissing: boolean
  sixthSenseSubject: string | null
  signatureWeapon: SignatureWeapon | null
  complete: boolean
}

/** Как задано происхождение героической способности (ROT-HA-01). */
export type HeroicOriginMode = 'standard' | 'doubleStandard' | 'custom'

/** Категория происхождения из таблицы d10; порядок совпадает с гранями 1–9. */
export type HeroicOriginType =
  | 'bloodline' | 'destiny' | 'artifact' | 'patron' | 'purpose'
  | 'lifeChangingEvent' | 'blessingOrCurse' | 'training' | 'wildMagic'

/** Личное название и происхождение героической способности. */
export interface HeroicIdentity {
  customName: string | null
  originMode: HeroicOriginMode | null
  originPrimary: HeroicOriginType | null
  originSecondary: HeroicOriginType | null
  originNarrative: string | null
  /** Фактические грани броска; 0 — специальный результат «бросить ещё дважды». */
  originRolls: number[]
  complete: boolean
}

/** Результат серверного броска по таблице происхождения. */
export interface HeroicOriginRollResult {
  rolls: number[]
  originMode: HeroicOriginMode
  originPrimary: HeroicOriginType
  originSecondary: HeroicOriginType | null
}

export interface HeroicSecondaryEffect {
  id: string
  code: string
  name: string
  nameRu: string
  description: string
  safeDescription: string
  descriptionEn?: string
  source: string
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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
  source: string
  isCustom: boolean
  /**
   * Навык, которому эффект доступен исключительно («Только Вера»); пусто — доступен нескольким.
   * По нему считается скидка священного символа (ROT-MAG-IMP-01).
   */
  restrictedSkill: string
  /**
   * Эффект можно добавлять к одному заклинанию несколько раз, каждый раз повышая сложность:
   * Дистанция удлиняет дальность на категорию за раз, Размер — силуэт.
   */
  repeatable: boolean
  /**
   * Направления, которым доступна запись (ROT-MAG-01). Матрицу доступности клиент не собирает
   * сам — иначе она разъедется с серверной.
   */
  allowedSkills: string[]
  /** Число из difficulty: базовая сложность действия или надбавка эффекта. */
  difficultyIncrease: number
  /** Коды эффектов, вместе с которыми этот выбрать нельзя. */
  exclusions: string[]
  /** Как эффект применяется: сразу, свойством, активацией преимуществ, очком сюжета. */
  resolution: SpellResolution
  /** Запись из опциональной книги (Expanded Player's Guide). */
  isOptional: boolean
  /** Числа эффекта берутся из рангов Знания заклинателя (ROT-MAG-10). */
  usesKnowledgeRating: boolean
  /** Свойства, получающие рейтинг по Знанию; пусто — рейтинг не у свойства. */
  ratedQualities: SpellRatedQuality[]
}

/** Свойство, чей рейтинг равен рангам Знания заклинателя (ROT-MAG-10). */
export interface SpellRatedQuality {
  code: string
  nameRu: string
  nameEn: string
}

/** Как применяется запись справочника магии (ROT-MAG-01). */
export type SpellResolution =
  | 'onSuccess' | 'passiveQuality' | 'activatedQuality' | 'advantageSpend'
  | 'storyPoint' | 'narrative' | 'parameter'

export type ArchetypeAbilityAutomationKind = 'passive' | 'activationCost' | 'timedEffect' | 'manual' | 'requiresGmDecision'

/** Исполняемый тип видового правила (ROT-SPECIES-01) — источник механики, не имя способности. */
export type SpeciesAbilityRuleKind =
  | 'manual' | 'moveStoryPointToPlayers' | 'setBaseDefense' | 'removeSetbackBySource'
  | 'forceCriticalInjuryRoll' | 'addSetbackWhenTargeted' | 'optionalSetbackForDamage'
  | 'strainThresholdRage' | 'boostAgainstMarkedTarget' | 'naturalWeapon'
  | 'freeSecondMoveManeuver' | 'setSilhouette' | 'boostAgainstLargerSilhouette'
  | 'conjureMinorItem' | 'chooseOneAbility' | 'skillGrantOnly'

export type AbilityUseScope = 'none' | 'encounter' | 'session' | 'round' | 'turn'

export interface ArchetypeAbility {
  code: string
  nameRu: string
  nameEn: string
  safeDescription: string
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
  automationKind: ArchetypeAbilityAutomationKind
  ruleKind: SpeciesAbilityRuleKind
  ruleValue: number
  ruleParameters: string
  usesPerScope: number
  useScope: AbilityUseScope
  storyPointCost: number
  /** Допустимые коды для способности-выбора; пусто у обычных способностей. */
  choiceOptions: string[] | null
}

export interface ArchetypeStartingSkill {
  skillName: string
  nameRu: string
  freeRanks: number
  isChoice: boolean
  choiceGroup: string
  choiceCount: number
  /** Выдача делает навык карьерным вдобавок к бесплатным рангам (ROT-CRE-01). */
  grantsCareerSkill: boolean
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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
  source: string
  isCustom: boolean
  abilities: ArchetypeAbility[]
  startingSkills: ArchetypeStartingSkill[]
  /** Размер существа: 1 у всех видов RoT, 0 у обоих гномов. */
  silhouette: number
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
  /** Английское описание; пусто, если не переведено. */
  descriptionEn?: string
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
  /** Английское описание (собственный copyright-safe парафраз); пусто, если не переведено. */
  descriptionEn?: string
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
  heroicSecondaryEffects: HeroicSecondaryEffect[]
  qualities: Quality[]
  /** Улучшения предметов (ROT-EQP-ATT-01). */
  attachments: AttachmentDef[]
  /** Покупаемый транспорт (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01): скакуны и повозки со статблоком. */
  mounts: MountDef[]
}

/** Скакун или транспортное средство (ROT-TRANSPORT-01). */
export type TransportKind = 'mount' | 'vehicle'

/** Режим движения транспорта: числовой скорости книга этим профилям не даёт. */
export type MovementMode = 'ground' | 'flight' | 'wheeled'

/** Профиль покупаемого транспорта (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). */
export interface MountDef {
  id: string
  code: string
  name: string
  nameRu: string
  transportKind: TransportKind
  movementMode: MovementMode
  /** Сам не движется: нужно тягловое животное. */
  requiresTraction: boolean
  kind: NpcKind
  characteristics: Record<Characteristic, number>
  soak: number
  woundThreshold: number
  /** Порог усталости; null у Minion — его у них нет. */
  strainThreshold: number | null
  meleeDefense: number
  rangedDefense: number
  silhouette: number
  /** Вместимость профиля: приоритетнее общего правила «5 + Мощь». */
  capacity: number
  /** null — бесценно: обычная покупка недоступна, цену называет ведущий. */
  price: number | null
  rarity: number
  /** Снаряжение, идущее вместе со скакуном: коды, локализация на клиенте. */
  includedGear: string[]
  /** В бою и под стрессом нужна проверка Верховой езды; сложность задаёт ведущий. */
  requiresRidingCheck: boolean
  skills: MountSkill[]
  abilities: MountAbility[]
  attacks: MountAttack[]
  description: string
  descriptionEn: string
  source: string
}

export interface MountSkill {
  name: string
  ranks: number
  /** Групповой навык Minion: ранг даёт группа, а не запись. */
  isGroupSkill: boolean
}

export interface MountAbility {
  name: string
  nameRu: string
  description: string
  descriptionEn: string
}

export interface MountAttack {
  name: string
  nameRu: string
  skillName: string
  damage: number
  critical: number
  range: WeaponRange
  qualityCodes: string[]
}

/** Транспорт персонажа: свой порог ран и свой груз, в переносимый вес владельца не входит. */
export interface CharacterMount {
  id: string
  mountDefId: string
  /** Кличка или название, если задано, иначе название профиля. */
  displayName: string
  name: string
  definition: MountDef
  woundsCurrent: number
  /** Загрузка по позициям груза; установленное снаряжение сюда не входит. */
  carriedLoad: number
  /** Вместимость профиля плюс прибавка от установленных сумок. */
  capacity: number
  isActive: boolean
  isOverloaded: boolean
  /** Раны достигли порога профиля — транспорт выведен из строя. */
  isIncapacitated: boolean
  provenance: ItemProvenance
  notes: string
  /** Тягловое животное; null — тяги нет. */
  drawnByMountId: string | null
  drawnByName: string
  /** Нужна тяга, а животного нет: транспорт стоит, но груз остаётся на нём. */
  needsTraction: boolean
  /** Поглощение и защита с учётом установленной попоны. */
  soak: number
  meleeDefense: number
  rangedDefense: number
  /** Позиции груза; установленное снаряжение помечено флагом позиции. */
  cargo: SheetItem[]
  /** Попона этому транспорту не положена по умолчанию — её ставит ведущий с причиной. */
  requiresGmApprovalForBarding: boolean
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
  availableXp: number
  woundsCurrent: number
  woundThreshold: number
  strainCurrent: number
  strainThreshold: number
  portraitUrl: string | null
}

export interface CharacterShareResponse {
  token: string
  path: string
}

export interface DicePool {
  ability: number
  proficiency: number
}

/** Взаимоисключающие режимы стартового снаряжения (ROT-CRE-03). */
export type StartingEquipmentMode = 'standardMoney' | 'careerPackage'

/** Откуда позиция инвентаря появилась у персонажа (ROT-CRE-03). */
export type ItemProvenance =
  | 'purchased' | 'careerPackage' | 'imported'
  /** Изготовлено персонажем (ROT-CRAFT-01) — метка «создано персонажем» на карточке. */
  | 'crafted'
  /** Сделано грубо, Выживанием: ведущий может сломать вещь на отчаянии проверки с ней. */
  | 'roughSurvival'

/** Источник карьерного статуса навыка (ROT-CRE-01). */
export interface CareerSkillSource {
  source: 'Career' | 'Species' | 'Talent'
  sourceName: string
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
  /** Все источники карьерного статуса: карьера, вид, таланты. Пусто — навык некарьерный. */
  careerSources: CareerSkillSource[]
  /**
   * Безусловные кости помех к этой проверке: снаряжение и перегруз (ROT-ARM-01, ROT-EQP-01).
   * Ровно столько чёрных кубов роллер подставляет сам.
   */
  setbackDice: number
  /** Кости умения к проверке от улучшений (ROT-EQP-ATT-01). */
  boostDice: number
  /** Из чего сложились помехи, включая условные вклады (их в пул не подставляют). */
  setbackSources: CheckModifierSource[]
  /** Фиолетовые кости сложности от длительных эффектов критических травм. */
  difficultyDice?: number
  /** Усиления сложности от длительных эффектов критических травм. */
  difficultyUpgrades?: number
  /** Критическая травма убирает все бонусные кости этого броска. */
  removeBoosts?: boolean
}

/** Один источник помех к проверке: снаряжение или перегруз. */
export interface CheckModifierSource {
  sourceType: 'Item' | 'Encumbrance' | string
  sourceName: string
  sourceNameRu: string
  /** Больше нуля — добавляет помехи, меньше — снимает. */
  setback: number
  /** Условие из книги; непустое — вклад показывается, но автоматически не применяется. */
  condition: string
  /** Кости умения от источника (например, установленного улучшения). */
  boost?: number
  /** Фиолетовые кости сложности от источника. */
  difficulty?: number
  /** Усиления сложности от источника. */
  difficultyUpgrades?: number
  /** Источник убирает все бонусные кости этого броска. */
  removeBoosts?: boolean
}

/** Как считается урон профиля: прибавка к Мощи или итоговое число (ROT-WPN-01). */
export type DamageKind = 'brawnPlus' | 'fixed'

/** Дистанция профиля атаки. */
export type WeaponRange = 'engaged' | 'short' | 'medium' | 'long' | 'extreme'

/** Вклад одного качества в пул атаки. */
export interface QualityContribution {
  nameEn: string
  nameRu: string
  boost: number
  setback: number
  difficulty: number
  advantage: number
  threat: number
}

/**
 * Что качества оружия делают с пулом атаки (GEN-EQP-QUAL-01). Автоматические преимущества и
 * угрозы кубами не являются — они прибавляются к результату броска.
 */
export interface AttackPoolModifiers {
  boost: number
  setback: number
  difficultyIncrease: number
  automaticAdvantage: number
  automaticThreat: number
  sources: QualityContribution[]
}

/**
 * Профиль атаки оружия (ROT-WPN-01): один экземпляр может бить в ближнем бою и метаться.
 * Урон разложен на тип и значение, поэтому строку «+3» клиент больше не разбирает.
 */
export interface WeaponAttackProfile {
  code: string
  nameRu: string
  nameEn: string
  isDefault: boolean
  skillName: string
  damageKind: DamageKind
  damageValue: number
  crit: number
  range: WeaponRange
  /** Оружием нельзя атаковать вплотную (пика). */
  cannotAttackEngaged: boolean
  /** Сложность, заданную самим оружием, роллер подставляет сам. */
  fixedDifficulty: number | null
  qualities: ItemQualityRef[]
  /** Базовый урон, посчитанный сервером под Мощь персонажа; null в справочнике. */
  baseDamage: number | null
  /** Изменение пула от качеств оружия; null в справочнике, где нет характеристик. */
  poolModifiers: AttackPoolModifiers | null
}

/** Влияние предмета на проверки навыков (ROT-ARM-01). */
export interface ItemCheckModifier {
  kind: 'AddSetback' | 'RemoveSetback'
  skillName: string
  characteristic: Characteristic | null
  value: number
  requiresWorn: boolean
  condition: string
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
  /** Английское описание; пусто, если не переведено. */
  descriptionEn?: string
  woundBonus: number
  strainBonus: number
  soakBonus: number
  meleeDefenseBonus: number
  rangedDefenseBonus: number
  grantsCharacteristic: boolean
  grantedCharacteristics: Characteristic[]
  /** Сохранённые выборы по рангам (ROT-TAL-03). */
  choices: CharacterTalentChoice[]
  /** Талант требует выбора, которого нет; эффект заблокирован до ручного исправления. */
  needsChoice: boolean
  /** Английский тайминг активации и возможность применения вне хода (ROT-TAL-01). */
  activationEn: string
  canUseOutOfTurn: boolean
  /** Стабильный bare-код определения таланта для структурных правил магии. */
  linkCode: string
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
  /** Copyright-safe описание; используется, когда private-описание недоступно. */
  safeDescription: string
  /** Английское описание; пусто, если не переведено. */
  descriptionEn?: string
  price: number | null
  skillName: string
  damage: string
  crit: string
  rangeBand: string
  properties: string
  /** Позиция выбрана активной бронёй: только она даёт защиту и поглощение (ROT-CMB-02). */
  isActiveArmor: boolean
  /**
   * Слоты улучшений по таблице книги (ROT-WPN-01/ROT-ARM-01); null — книжного значения нет.
   * Ноль означает «улучшения ставить некуда».
   */
  hardPoints: number | null
  /** Влияние предмета на проверки навыков: штраф Скрытности у тяжёлой брони и т. п. */
  checkModifiers: ItemCheckModifier[]
  /** Профили атаки с уже посчитанным базовым уроном (ROT-WPN-01). */
  attackProfiles: WeaponAttackProfile[]
  /** Оружие метнули и не подобрали: атаковать нельзя, качеств и веса не даёт. */
  isThrown: boolean
  /** Откуда позиция взялась; `crafted`/`roughSurvival` — изготовлена персонажем. */
  provenance: ItemProvenance
  /**
   * Описание изготовления: все траты символов словами. Половина из них — правила, которые
   * приложение не исполняет, поэтому текст показывается на карточке.
   */
  craftNote: string
  /**
   * Качество изготовления экземпляра (ROT-WPN-02). Числа позиции выше — уже с его поправками:
   * вес, поглощение, защита, слоты, цена, редкость и профили атаки.
   */
  craftsmanship: WeaponCraftsmanship
  /** Редкость экземпляра: Ancient задаёт ровно 10, остальные типы сдвигают каталожную. */
  rarity: number | null
  /** Экземпляр укреплён (Ancient): броня не поддаётся Pierce/Breach, а предмет — Sunder. */
  reinforced: boolean
  /** Разбор поправок: что качество изготовления изменило и с какого значения. */
  adjustments: ItemStatAdjustment[]
  /** Установленные улучшения (ROT-EQP-ATT-01). */
  attachments: CharacterAttachment[]
  /** Занято слотов улучшений из hardPoints. */
  usedHardPoints: number
  /** Улучшений больше, чем слотов: новые не ставятся, пока лишнее не снято. */
  overCapacity: boolean
  /** Правила улучшений, которые приложение не исполняет (показываются, а не теряются). */
  attachmentNotes: string[]
  /** Признаки формы предмета: по ним считается совместимость улучшений. */
  formTraits: string
  /**
   * Состояние повреждения экземпляра (GEN-EQP-DMG-01). Числа позиции выше — уже с его учётом:
   * у серьёзно повреждённого предмета поглощение, защита и прибавка к порогу веса обнулены.
   */
  damageState: ItemDamageState
  /** Предметом можно пользоваться: false — Серьёзное повреждение или Уничтожено. */
  isUsable: boolean
  /** Памятка по ремонту: сложность, время, доля и стоимость материалов. */
  repair: ItemRepair
  /** Магический инструмент (ROT-MAG-IMP-01); null — запись инструментом не является. */
  implement: ItemImplement | null
  /** Runebound shard и конфигурация конкретного экземпляра. */
  shard: ItemRuneboundShard | null
  sellable: boolean
  /** Предмет можно взять в руки или надеть; у верёвки и провизии этого состояния нет. */
  canEquip: boolean
  /** У предмета есть состояние поломки и ремонт (GEN-EQP-DMG-01). */
  canBeDamaged: boolean
  /**
   * Позиция лежит на транспорте (ROT-TRANSPORT-01); null — обычная позиция инвентаря. Такие
   * позиции приходят в карточке транспорта, а не в списке инвентаря владельца.
   */
  carriedByMountId: string | null
  /** Снаряжение установлено на транспорт, а не сложено в него грузом: попона, седельные сумки. */
  isInstalledOnMount: boolean
  /** Позицию можно установить на транспорт. */
  isMountGear: boolean
  /**
   * Это попона — единственное установленное снаряжение с ограничением по профилю. Вместе с
   * `CharacterMount.requiresGmApprovalForBarding` говорит, когда спрашивать причину у ведущего.
   */
  isBarding: boolean
}

/** Материал магического инструмента (ROT-MAG-MAT-01). */
export type ImplementMaterial = 'oak' | 'bone' | 'hazel' | 'willow' | 'yew'

/** Как инструмент удешевляет дополнительный эффект заклинания (ROT-MAG-IMP-01). */
export type ImplementDiscountKind =
  | 'none' | 'namedEffects' | 'firstNamedEffect' | 'restrictedSkillDiscount' | 'chosenEffects'

/** Паспорт магического инструмента: и у записи справочника, и внутри позиции инвентаря. */
export interface ImplementSpec {
  code: string
  /** Прибавка к базовому урону магической Атаки; не урон ближнего боя и не влияет на Лечение. */
  attackDamageBonus: number
  /** Бонусные кости к магической проверке: скипетр даёт одну. */
  boostDice: number
  /** Инструмент работает только с этим навыком; пусто — с любым. */
  requiredMagicSkill: string
  discount: ImplementDiscountKind
  /** Коды эффектов, которых касается скидка. */
  discountEffects: string[]
  /** Сколько эффектов выбирает ведущий при изготовлении экземпляра. */
  choiceCount: number
  choiceMaxIncreaseSum: number | null
  choiceExactIncrease: number | null
}

/** Магический инструмент в инвентаре: паспорт плюс то, что принадлежит экземпляру. */
export interface ItemImplement extends ImplementSpec {
  material: ImplementMaterial
  /** Эффекты, выбранные ведущим (коды). */
  chosenEffects: string[]
  /** Экземпляр ещё не настроен: обычные числа есть, бесплатный эффект не работает. */
  pending: boolean
  /** Помехи к магической проверке от состояния этого экземпляра. */
  damageSetbackDice: number
  /** Повышение сложности магической проверки от состояния этого экземпляра. */
  damageDifficultyIncrease: number
}

export type ShardSpellEffectMode = 'mandatoryFree' | 'optionalFree'
export type ShardActivationCost = 'maneuver' | 'action' | 'passive'

export interface ShardSpellEffect {
  action: string
  effectCode: string
  mode: ShardSpellEffectMode
  freeUses: number
  overridesSkillRestriction: boolean
}

export interface ShardDifficultyReduction {
  action: string
  amount: number
}

export interface ShardActivationQuality {
  code: string
  rating: number | null
}

export interface ShardActivationAttack {
  skill: string
  damage: number
  critical: number
  range: string
  qualities: ShardActivationQuality[]
}

/** Типизированный паспорт одной runebound shard (ROT-MAG-11). */
export interface RuneboundShardSpec {
  code: string
  requiredMagicSkill: string
  minimumSkillRank: number
  attackDamageBonus: number
  castingStrainReduction: number
  difficultyReductions: ShardDifficultyReduction[]
  spellEffects: ShardSpellEffect[]
  activationCost: ShardActivationCost
  activationFrequency: string
  activationAttack: ShardActivationAttack | null
  needsConfiguration: boolean
}

export interface ItemRuneboundShard {
  spec: RuneboundShardSpec
  activationChoice: string
  effectAction: string
  effectChoice: string
  pending: boolean
}

/** Состояние повреждения экземпляра (GEN-EQP-DMG-01). */
export type ItemDamageState = 'undamaged' | 'minor' | 'moderate' | 'major' | 'destroyed'

/**
 * Памятка по ремонту экземпляра: всё, что нужно знать до нажатия кнопки. Считает сервер —
 * стоимость идёт от цены экземпляра с учётом качества изготовления, а не от строки каталога.
 */
export interface ItemRepair {
  state: ItemDamageState
  /** Обычный ремонт доступен: уничтоженное чинится только особым правилом ведущего. */
  canRepair: boolean
  /** Базовая сложность проверки по книге: 1/2/3; null — ремонта нет. */
  difficulty: number | null
  hoursMin: number
  hoursMax: number
  /** Доля цены экземпляра на материалы: 25/50/100. */
  materialPercent: number
  /** Стоимость материалов; null — обычной цены нет, сумму называет ведущий. */
  materialCost: number | null
  /** Навык ремонта по умолчанию (английское имя). */
  skillName: string
  /** Денег в обычном кошельке хватает. */
  affordable: boolean
}

/** Вид эффекта улучшения (ROT-EQP-ATT-01). */
export type AttachmentEffectKind =
  | 'grantOrIncreaseQuality' | 'setQualityAtLeast' | 'grantQualityOrCancelOpposite'
  | 'damage' | 'critReduction' | 'soak' | 'meleeDefense' | 'rangedDefense' | 'encumbrance'
  | 'skillBoost' | 'automaticSymbol' | 'narrativeOnly'

/** Когда эффект улучшения действует. */
export type AttachmentEffectCondition = 'always' | 'wornAndActive'

/** Один типизированный эффект улучшения. */
export interface AttachmentEffect {
  kind: AttachmentEffectKind
  qualityCode: string
  skillName: string
  value: number
  increment: number
  condition: AttachmentEffectCondition
  note: string
  /** Приложение действительно считает этот эффект; false — правило только описано. */
  executed: boolean
}

/** Улучшение справочника: собственный тип контента, а не снаряжение. */
export interface AttachmentDef {
  id: string
  code: string
  name: string
  nameRu: string
  hardPointCost: number
  /** null — бесценно: обычная покупка недоступна, цену называет ведущий. */
  price: number | null
  rarity: number
  isEnchantment: boolean
  hostKind: ItemKind
  /** Флаги едут строкой «bladed, sword»; пусто или «none» — требований нет. */
  requiredTraits: string
  requiredAnyTraits: string
  forbiddenTraits: string
  description: string
  descriptionEn: string
  source: string
  effects: AttachmentEffect[]
}

/** Экземпляр улучшения у персонажа: в запасе (host = null) или на предмете. */
export interface CharacterAttachment {
  id: string
  attachmentDefId: string
  name: string
  nameRu: string
  hardPointCost: number
  isEnchantment: boolean
  price: number | null
  rarity: number
  hostCharacterItemId: string | null
  note: string
  effects: AttachmentEffect[]
  /**
   * Собственное состояние повреждения улучшения (GEN-EQP-DMG-01): сломанное не даёт эффекта,
   * но слот носителя не освобождает.
   */
  damageState: ItemDamageState
  /** Эффекты улучшения действуют: состояние не Серьёзное и не Уничтожено. */
  isUsable: boolean
  /** Памятка по ремонту улучшения. */
  repair: ItemRepair
}

/** Исход снятия улучшения. */
export type DetachOutcome = 'returned' | 'destroyed' | 'unusable'

/** Этап расчёта характеристик предмета (ROT-WPN-02). */
export type ItemStatStage = 'base' | 'craftsmanship' | 'attachments' | 'damageState' | 'situational'

/** Одна поправка к характеристике экземпляра: что изменилось, с чего на что и от чего. */
export interface ItemStatAdjustment {
  /** Стабильный код характеристики: encumbrance, soak, meleeDefense, price… */
  field: string
  base: number
  effective: number
  stage: ItemStatStage
  /** Источник поправки: значение WeaponCraftsmanship в PascalCase. */
  source: string
}

/** Один источник защиты для объяснения итога (ROT-CMB-03). */
export interface DefenseSource {
  sourceType: string
  sourceName: string
  value: number
}

/** Разбор канала защиты: что победило, что проигнорировано и где сработал предел 4. */
export interface DefenseBreakdown {
  raw: number
  effective: number
  capped: boolean
  provider: DefenseSource | null
  ignoredProviders: DefenseSource[]
  increases: DefenseSource[]
}

/** Состояние перегруза: помехи, бесплатный манёвр и цена манёвра. */
export interface Encumbrance {
  overload: number
  setbackDice: number
  hasFreeManoeuvre: boolean
  strainPerManoeuvre: number
  zeroEncumbranceLoad: number
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
  /** Как сложилась ближняя защита; null у старых ответов (ROT-CMB-03). */
  meleeDefenseBreakdown: DefenseBreakdown | null
  /** Как сложилась дальняя защита. */
  rangedDefenseBreakdown: DefenseBreakdown | null
  /** Точная цена перегруза (ROT-EQP-01); null у старых ответов. */
  encumbrance: Encumbrance | null
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
  portraitUrl?: string | null
  /** Доступный XP показываем только владельцу персонажа. */
  availableXp?: number | null
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

export interface CampaignChronicleChapter {
  id: string
  title: string
  content: string
  sortOrder: number
  currentVersion: number
  createdAt: string
  updatedAt: string
  updatedBy: string
}

export interface CampaignChronicleRevision {
  id: string
  version: number
  title: string
  content: string
  editedAt: string
  editedBy: string
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
  silhouette: number
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
  /** Авторитетное число действующих участников группы после потерь. */
  remainingCount?: number
  /** Индивидуальный WT миньона, null для обычного NPC и неоднозначного legacy snapshot. */
  perMemberWoundThreshold?: number | null
  woundsCurrent: number
  woundsThreshold: number
  strainCurrent: number
  strainThreshold: number | null
  soak: number
  meleeDefense: number
  rangedDefense: number
  boostDice: number
  setbackDice: number
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
  boostDice?: number | null
  setbackDice?: number | null
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
  heroicUpgrades: {
    powerRank: number
    durationRanks: number
    frequencyRanks: number
    story: boolean
    secondaryEffects: HeroicSecondaryEffect[]
  }
  /** Личность героической способности; null, пока способность не выбрана (ROT-HA-01). */
  heroicIdentity: HeroicIdentity | null
  /** Способность выбрана, но личность не заполнена — улучшения заблокированы. */
  heroicIdentityIncomplete: boolean
  /** Параметр primary effect; null, пока способность не выбрана (ROT-HA-02). */
  heroicConfiguration: HeroicConfiguration | null
  /** Способность требует параметр, а он не выбран — улучшения заблокированы. */
  heroicConfigurationIncomplete: boolean
  /** Выбранная активная броня; null — броня защиты не даёт (ROT-CMB-02). */
  activeArmorCharacterItemId: string | null
  items: SheetItem[]
  /** Все улучшения персонажа, включая лежащие в запасе (ROT-EQP-ATT-01). */
  attachments: CharacterAttachment[]
  // Мотивации и предыстория (U-22)
  desire: string | null
  fear: string | null
  strength: string | null
  flaw: string | null
  background: string | null
  // Критические ранения (U-23)
  criticalInjuries: CriticalInjury[]
  portraitUrl: string | null
  /** Откуда персонаж берёт рейтинг эффектов заклинания (ROT-MAG-10). */
  knowledgeRating: KnowledgeRating | null
  /** Скакуны персонажа (ROT-MOUNT-ITEM-01): в переносимый вес не входят. */
  mounts: CharacterMount[]
}

/**
 * Части листа, которые запрашиваются по отдельности. Лист играющего персонажа весит около 116 КБ,
 * и две трети из них — инвентарь, который главной вкладке не нужен вовсе. Вкладка берёт свои части
 * при открытии и не платит за чужие.
 */
export type SheetSliceName = 'base' | 'items' | 'talents' | 'mounts' | 'attachments'

/** Базовый лист: всё, кроме тяжёлых коллекций. */
export type BaseSheet =
  Omit<CharacterSheet, 'items' | 'talents' | 'talentTierCounts' | 'mounts' | 'attachments'>

/**
 * Ответ сервера с запрошенными частями. Незапрошенная часть значит «не загружено», а не «пусто»:
 * пустой массив значит, что предметов (талантов, транспорта) у персонажа действительно нет.
 *
 * Незагруженное приходит именно как `null`, а не отсутствующим полем: сервер сериализует `null`-ы.
 * Поэтому у полей `| null` — чтобы проверку «загружено ли» нельзя было написать через `undefined`
 * и молча получить «загружено и пусто».
 */
export interface SheetSlices {
  base?: BaseSheet | null
  items?: SheetItem[] | null
  talents?: SheetTalent[] | null
  talentTierCounts?: Record<string, number> | null
  mounts?: CharacterMount[] | null
  attachments?: CharacterAttachment[] | null
  /**
   * Идентификатор только что созданной записи — у покупки предмета, транспорта, улучшения.
   * Иначе за ним пришлось бы оставлять отдельный ответ, а вместе с ним и второй запрос за листом.
   */
  createdId?: string | null
}

/**
 * Источники числового рейтинга эффектов заклинания (ROT-MAG-10). Список, а не одно число:
 * «Тёмное прозрение» даёт игроку выбор, и делать его за него нельзя.
 */
export interface KnowledgeRating {
  /** Первый — навык из правил системы, дальше — исключения таланта. */
  options: KnowledgeRatingOption[]
}

export interface KnowledgeRatingOption {
  /** Английское имя навыка — стабильный код выбора. */
  skill: string
  skillRu: string
  ranks: number
  /** `default` — навык из правил системы, `darkInsight` — исключение таланта. */
  reason: 'default' | 'darkInsight'
}

/** Критическое ранение персонажа (U-23). */
export interface CriticalInjury {
  id: string
  ruleCode: string | null
  nameRu: string
  severity: string | null
  rollResult: number | null
  notes: string | null
  /** Эффект строки справочника; null у ручной травмы или старого ответа. */
  effect?: string | null
  effectEn?: string | null
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
  | 'heroicIdentitySet' | 'heroicOriginRolled'
  | 'heroicParameterSet' | 'signatureWeaponReplaced' | 'activeArmorChanged'

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
  | 'weaponProperty' | 'combatActionManeuver' | 'magicActionManeuver'

export interface RuleTableEntry {
  id: string
  kind: RuleTableKind
  code: string
  nameRu: string
  nameEn: string
  groupRu: string
  /** Английское имя группы; пусто → используем groupRu. */
  groupEn?: string
  sortOrder: number
  rollRange: string
  symbolCost: string
  body: string
  /** Английский парафраз body; пусто, если не переведено. */
  bodyEn?: string
  notes: string
  /** Английский парафраз notes; пусто, если не переведено. */
  notesEn?: string
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

// ─────────────────────────── Ремесло (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01) ───────────────────────────

/** Что изготавливается: предмет, зелье или зачарование готовой основы. */
export type CraftingKind = 'item' | 'potion' | 'enchantment'

export type CraftingProjectStatus = 'draft' | 'resolved' | 'cancelled'

/** Механика траты; `descriptive` — приложение её не исполняет, она остаётся текстом. */
export type CraftingSpendEffect =
  | 'descriptive' | 'time' | 'encumbrance' | 'hardPoints' | 'addQuality'
  | 'qualityRating' | 'extraQuantity' | 'fragile' | 'combineDose' | 'timeHalved'

/** Каким символом оплачена трата. */
export type CraftingSymbol = 'advantage' | 'threat' | 'triumph' | 'despair'

/** Строка таблицы трат символов. */
export interface CraftingSpend {
  code: string
  /** Внутри строки эффекты взаимоисключающие: за одну цену берут один. */
  rowCode: string
  table: CraftingKind
  nameRu: string
  nameEn: string
  description: string
  descriptionEn: string
  advantageCost: number
  threatCost: number
  triumphCost: number
  despairCost: number
  isNegative: boolean
  repeatable: boolean
  requiresGmConfirmation: boolean
  requiresParameter: boolean
  effect: CraftingSpendEffect
  weaponOnly: boolean
  sortOrder: number
}

export interface CraftingSpendChoice {
  code: string
  count?: number
  parameter?: string
  paidWith: CraftingSymbol
}

export interface CraftingProjectSpend {
  code: string
  count: number
  parameter: string
  paidWith: string
  textRu: string
  textEn: string
}

/** Числа проекта, посчитанные сервером до любой записи. */
export interface CraftingPreview {
  kind: CraftingKind
  targetName: string
  targetPrice: number | null
  targetRarity: number | null
  craftsmanship: WeaponCraftsmanship
  material: ImplementMaterial
  skillName: string
  baseDifficulty: number
  difficulty: number
  baseTime: number
  time: number
  timeUnit: 'days' | 'hours'
  listedCost: number
  costPercent: number
  costOverride: number | null
  cost: number
  isWeapon: boolean
  spends: CraftingSpend[]
}

export interface CraftingProject {
  id: string
  kind: CraftingKind
  status: CraftingProjectStatus
  itemDefId: string
  baseCharacterItemId: string | null
  targetName: string
  targetPrice: number | null
  targetRarity: number | null
  craftsmanship: WeaponCraftsmanship
  material: ImplementMaterial
  skillName: string
  baseDifficulty: number
  difficulty: number
  difficultyReason: string
  baseTime: number
  time: number
  timeUnit: 'days' | 'hours'
  timeReason: string
  listedCost: number
  costPercent: number
  costOverride: number | null
  costOverrideReason: string
  cost: number
  /** Инструменты и компоненты своими словами; ни на что не проверяются. */
  requirements: string
  intent: string
  roughSurvival: boolean
  netSuccesses: number
  advantages: number
  threats: number
  triumphs: number
  despairs: number
  createdCharacterItemId: string | null
  outcome: string
  spends: CraftingProjectSpend[]
  createdAt: string
  resolvedAt: string | null
}

/** Тело создания проекта и предпросмотра. */
export interface CraftingProjectInput {
  itemDefId: string
  baseCharacterItemId?: string | null
  kind?: CraftingKind
  skillName?: string
  costPercent?: number
  costOverride?: number | null
  costOverrideReason?: string
  difficultyOverride?: number | null
  difficultyReason?: string
  timeOverride?: number | null
  timeReason?: string
  requirements?: string
  intent?: string
  roughSurvival?: boolean
  craftsmanship?: WeaponCraftsmanship
  material?: ImplementMaterial
}
