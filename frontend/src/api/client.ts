import type {
  Account,
  AuthResponse, AuthProviders, CampaignDetail, CampaignListItem, CampaignNote, CharacterListItem, CharacterNote,
  ActivateAbilityResult, ActivateCharacterAbilityResult, AddParticipantRequest, CharacterBio, CharacterSheet, CharacterShareResponse, CreatureTemplate, GameSession, GameSystem, HeroicAbility, HeroicOriginMode, HeroicOriginRollResult, HeroicOriginType, InitiativeSlotType, SignatureWeaponProfile, WeaponCraftsmanship,
  Archetype, Career, CustomArchetypeInput, CustomCareerInput, ItemDef, ItemState, NpcDetail, NpcFilter, NpcInput, NpcListItem, QuickDraftRequest, Reference,
  SkillDef, Spell, TalentCategory, TalentDef, UpdateParticipantRequest,
  AddEncounterParticipantRequest, EncounterDetail, EncounterFilter, EncounterInput, EncounterListItem,
  SendToTableMode, UpdateEncounterParticipantRequest,
  ContentPackDetail, ContentPackEntryInput, ContentPackListItem,
  HomebrewPackDocument, HomebrewPackImportResult, HomebrewPackListItem, HomebrewPackShare,
  CharacterExport, ImportPreview, ImportResult,
  RollLogEntry, CreateRollRequest,
  CharacterAuditEntry,
  RulesResponse, SearchResponse,
  ArchetypeSkillChoice, CareerGearChoice, StartingEquipmentMode,
} from './types'
import { t } from '../i18n'
import { ariadneAnonymousId } from '../analytics/ariadne'

const TOKEN_KEY = 'genesysforge.token'

export const tokenStorage = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

// Вызывается при 401 на запросе с токеном (протухшая/невалидная сессия).
let onUnauthorized: (() => void) | null = null
export const setUnauthorizedHandler = (handler: (() => void) | null) => { onUnauthorized = handler }

const isAuthPath = (url: string) => url.startsWith('/api/auth/')

// Обновление access-токена по refresh-cookie (single-flight: параллельные 401 ждут один запрос).
let refreshing: Promise<boolean> | null = null
function tryRefresh(): Promise<boolean> {
  refreshing ??= (async () => {
    try {
      const r = await fetch('/api/auth/refresh', { method: 'POST', credentials: 'include' })
      if (!r.ok) return false
      const data = await r.json() as AuthResponse
      tokenStorage.set(data.token)
      return true
    } catch {
      return false
    }
  })().finally(() => { refreshing = null })
  return refreshing
}

async function rawFetch(method: string, url: string, body: unknown): Promise<Response> {
  const headers: Record<string, string> = {}
  // Blob (файл) уходит сырым телом — сервер определяет формат по содержимому, не по Content-Type.
  const isBlob = typeof Blob !== 'undefined' && body instanceof Blob
  if (body !== undefined && !isBlob) headers['Content-Type'] = 'application/json'
  const token = tokenStorage.get()
  if (token) headers.Authorization = `Bearer ${token}`
  // Регистрация — единственный запрос, которому нужен анонимный ID визита: по нему «Ариадна»
  // склеивает серверное registration_completed с браузерной частью воронки.
  if (url === '/api/auth/register') {
    const anonymousId = ariadneAnonymousId()
    if (anonymousId) headers['X-Ariadne-Anonymous-Id'] = anonymousId
  }
  return fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : isBlob ? body : JSON.stringify(body),
    credentials: 'include', // отправлять/принимать refresh-cookie
  })
}

async function request<T>(method: string, url: string, body?: unknown, retried = false): Promise<T> {
  const hadToken = tokenStorage.get() !== null
  const response = await rawFetch(method, url, body)

  if (!response.ok) {
    // 401 на защищённом запросе: пробуем тихо обновить access-токен по refresh-cookie и повторить.
    // (на /api/auth/* не обновляем — там неверный логин/refresh сам по себе)
    if (response.status === 401 && !isAuthPath(url)) {
      if (!retried && await tryRefresh()) {
        return request<T>(method, url, body, true)
      }
      if (hadToken) {
        tokenStorage.clear()
        onUnauthorized?.()
      }
    }
    let message = t(`Ошибка ${response.status}`, `Error ${response.status}`)
    try {
      const data = await response.json()
      if (data?.message) message = data.message
    } catch {
      // тело не JSON — оставляем статус
    }
    throw new ApiError(response.status, message)
  }
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

export const api = {
  register: (email: string, password: string, displayName: string) =>
    request<AuthResponse>('POST', '/api/auth/register', { email, password, displayName }),
  login: (email: string, password: string) =>
    request<AuthResponse>('POST', '/api/auth/login', { email, password }),
  requestPasswordReset: (email: string) =>
    request<void>('POST', '/api/auth/password-reset/request', { email }),
  confirmPasswordReset: (token: string, newPassword: string) =>
    request<void>('POST', '/api/auth/password-reset/confirm', { token, newPassword }),
  authProviders: () => request<AuthProviders>('GET', '/api/auth/providers'),
  googleSignIn: (idToken: string) =>
    request<AuthResponse>('POST', '/api/auth/google', { idToken }),
  // Восстановление сессии по refresh-cookie (бросает при отсутствии валидного refresh-токена).
  refresh: () => request<AuthResponse>('POST', '/api/auth/refresh'),
  // Выход: сервер отзывает семейство refresh-токенов и чистит cookie.
  logout: () => request<void>('POST', '/api/auth/logout'),

  reference: (system: GameSystem, context?: { characterId?: string; campaignId?: string }) => {
    const params = new URLSearchParams()
    if (context?.characterId) params.set('characterId', context.characterId)
    if (context?.campaignId) params.set('campaignId', context.campaignId)
    const qs = params.size ? `?${params}` : ''
    return request<Reference>('GET', `/api/reference/${system === 'genesysCore' ? 'GenesysCore' : 'RealmsOfTerrinoth'}${qs}`)
  },

  // Справочные таблицы правил (U-11). Системо-независимы; опц. q — фильтр по подстроке.
  rules: (q?: string) =>
    request<RulesResponse>('GET', `/api/reference/rules${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  // Глобальный поиск по справочнику/контенту/сущностям (U-11).
  search: (system: GameSystem, q: string) =>
    request<SearchResponse>('GET',
      `/api/search?system=${system === 'genesysCore' ? 'GenesysCore' : 'RealmsOfTerrinoth'}&q=${encodeURIComponent(q)}`),
  spells: (system: GameSystem) =>
    request<Spell[]>('GET', `/api/spells/${system === 'genesysCore' ? 'GenesysCore' : 'RealmsOfTerrinoth'}`),

  characters: () => request<CharacterListItem[]>('GET', '/api/characters/'),
  createCharacter: (name: string, system: GameSystem, archetypeId: string, careerId: string,
    freeCareerSkillNames: string[], archetypeSkillChoices: ArchetypeSkillChoice[] = [],
    careerGearChoices: CareerGearChoice[] = [], bio: CharacterBio = {},
    startingEquipmentMode: StartingEquipmentMode = 'standardMoney',
    speciesAbilityChoiceCode?: string) =>
    request<{ id: string }>('POST', '/api/characters/',
      { name, system, archetypeId, careerId, freeCareerSkillNames, archetypeSkillChoices, careerGearChoices,
        startingEquipmentMode, speciesAbilityChoiceCode, ...bio }),
  sheet: (id: string) => request<CharacterSheet>('GET', `/api/characters/${id}`),
  sharedSheet: (token: string) => request<CharacterSheet>('GET', `/api/share/${encodeURIComponent(token)}`),
  duplicateCharacter: (id: string) => request<{ id: string }>('POST', `/api/characters/${id}/duplicate`),
  // Файл уходит сырым телом; формат (JPEG/PNG/WebP) и размер сервер проверяет по содержимому.
  uploadCharacterPortrait: (id: string, file: Blob) =>
    request<{ portraitUrl: string }>('POST', `/api/characters/${id}/portrait`, file),
  shareCharacter: (id: string) => request<CharacterShareResponse>('POST', `/api/characters/${id}/share`),
  revokeCharacterShares: (id: string) => request<void>('DELETE', `/api/characters/${id}/share`),
  exportCharacter: (id: string) => request<CharacterExport>('GET', `/api/characters/${id}/export`),
  importCharacter: (payload: CharacterExport) => request<ImportResult>('POST', '/api/characters/import', payload),
  previewImport: (payload: CharacterExport) => request<ImportPreview>('POST', '/api/characters/import/preview', payload),
  deleteCharacter: (id: string) => request<void>('DELETE', `/api/characters/${id}`),
  updateCharacter: (id: string, patch: { name?: string; totalXp?: number; woundsCurrent?: number; strainCurrent?: number; money?: number } & CharacterBio) =>
    request<void>('PATCH', `/api/characters/${id}`, patch),
  completeCreation: (id: string) => request<void>('POST', `/api/characters/${id}/complete-creation`),

  // История персонажа / выдача XP (U-09).
  characterAudit: (id: string, take = 100) =>
    request<CharacterAuditEntry[]>('GET', `/api/characters/${id}/audit?take=${take}`),
  awardXp: (id: string, body: { amount: number; note?: string }) =>
    request<void>('POST', `/api/characters/${id}/xp-awards`, body),

  buyCharacteristic: (id: string, characteristic: string) =>
    request<void>('POST', `/api/characters/${id}/characteristics/${characteristic}/buy`),
  buySkillRank: (id: string, skillDefId: string) =>
    request<void>('POST', `/api/characters/${id}/skills/${skillDefId}/buy-rank`),
  buyTalent: (id: string, talentDefId: string, characteristic?: string, choices?: string[]) =>
    request<void>('POST', `/api/characters/${id}/talents/buy`, { talentDefId, characteristic, choices }),
  refundCharacteristic: (id: string, characteristic: string) =>
    request<void>('POST', `/api/characters/${id}/characteristics/${characteristic}/refund`),
  refundSkillRank: (id: string, skillDefId: string) =>
    request<void>('POST', `/api/characters/${id}/skills/${skillDefId}/refund-rank`),
  refundTalent: (id: string, talentDefId: string) =>
    request<void>('POST', `/api/characters/${id}/talents/refund`, { talentDefId }),
  setHeroicAbility: (id: string, heroicAbilityId: string | null) =>
    request<void>('PUT', `/api/characters/${id}/heroic-ability`, { heroicAbilityId }),
  setActiveArmor: (id: string, characterItemId: string | null) =>
    request<void>('PUT', `/api/characters/${id}/active-armor`, { characterItemId }),
  setHeroicIdentity: (id: string, body: {
    customName: string
    originMode?: HeroicOriginMode | null
    originPrimary?: HeroicOriginType | null
    originSecondary?: HeroicOriginType | null
    originNarrative?: string | null
  }) => request<void>('PUT', `/api/characters/${id}/heroic-identity`, body),
  rollHeroicOrigin: (id: string) =>
    request<HeroicOriginRollResult>('POST', `/api/characters/${id}/heroic-identity/roll-origin`),
  setHeroicConfiguration: (id: string, body: {
    paragonSkillDefId?: string | null
    sixthSenseSubject?: string | null
    weaponProfile?: SignatureWeaponProfile | null
    craftsmanship?: WeaponCraftsmanship | null
    narrativeForm?: string | null
    /** Флаги формы одной строкой: «oneHanded, sword». */
    formTraits?: string | null
  }) => request<void>('PUT', `/api/characters/${id}/heroic-configuration`, body),
  replaceSignatureWeapon: (id: string, body: {
    lost: boolean
    weaponProfile?: SignatureWeaponProfile | null
    craftsmanship?: WeaponCraftsmanship | null
    narrativeForm?: string | null
    formTraits?: string | null
  }) => request<void>('POST', `/api/characters/${id}/heroic-configuration/signature-weapon`, body),
  setHeroicUpgradeRank: (id: string, rank: number) =>
    request<void>('PUT', `/api/characters/${id}/heroic-upgrade`, { rank }),
  setHeroicUpgrades: (id: string, body: {
    powerRank: number
    durationRanks: number
    frequencyRanks: number
    story: boolean
    secondaryEffectIds: string[]
  }) => request<void>('PUT', `/api/characters/${id}/heroic-upgrades`, body),
  activateCharacterAbility: (id: string) =>
    request<ActivateCharacterAbilityResult>('POST', `/api/characters/${id}/activate-ability`),

  /**
   * Покупка: сумму считает сервер по цене каталога (ROT-ECO-01). `free` — выдача без оплаты,
   * `priceOverride` — цена ведущего, требующая причины.
   */
  addItem: (id: string, itemDefId: string, quantity: number, state: ItemState,
    opts?: { free?: boolean; priceOverride?: number; overrideReason?: string }) =>
    request<{ id: string }>('POST', `/api/characters/${id}/items`, { itemDefId, quantity, state, ...opts }),
  updateItem: (id: string, itemId: string, patch: { state?: ItemState; quantity?: number }) =>
    request<void>('PATCH', `/api/characters/${id}/items/${itemId}`, patch),
  /**
   * Продажа: сумму всегда считает сервер от цены каталога. Либо `netSuccesses` (доля по правилу),
   * либо `percent` для простой продажи без проверки; без обоих — полная цена.
   */
  sellItem: (id: string, itemId: string, quantity: number,
    opts?: { netSuccesses?: number; percent?: number; conditionMultiplier?: number; conditionReason?: string }) =>
    request<void>('POST', `/api/characters/${id}/items/${itemId}/sell`, { quantity, ...opts }),
  removeItem: (id: string, itemId: string) =>
    request<void>('DELETE', `/api/characters/${id}/items/${itemId}`),

  // Критические ранения (U-23): из таблицы U-11 (ruleCode) или вручную (nameRu).
  addCriticalInjury: (id: string, body: { ruleCode?: string; nameRu?: string; severity?: string; rollResult?: number; notes?: string }) =>
    request<{ id: string }>('POST', `/api/characters/${id}/critical-injuries`, body),
  removeCriticalInjury: (id: string, injuryId: string) =>
    request<void>('DELETE', `/api/characters/${id}/critical-injuries/${injuryId}`),

  createCustomSkill: (skill: { system: GameSystem; name: string; characteristic: string; kind: string }) =>
    request<SkillDef>('POST', '/api/custom/skills', skill),
  createCustomTalent: (talent: {
    system: GameSystem; name: string; tier: number; isRanked: boolean; category: TalentCategory; activation: string; description: string
    woundBonus: number; strainBonus: number; soakBonus: number; meleeDefenseBonus: number; rangedDefenseBonus: number
  }) => request<TalentDef>('POST', '/api/custom/talents', talent),
  createCustomItem: (item: {
    system: GameSystem; name: string; kind: string; encumbrance: number; soakBonus: number
    meleeDefense: number; rangedDefense: number; encumbranceThresholdBonus: number
    description: string; price: number; rarity: number
    skillName?: string; damage?: string; crit?: string; rangeBand?: string; properties?: string
  }) => request<ItemDef>('POST', '/api/custom/items', item),
  createCustomHeroicAbility: (ability: { name: string; description: string }) =>
    request<HeroicAbility>('POST', '/api/custom/heroic-abilities', ability),
  createCustomArchetype: (archetype: CustomArchetypeInput) =>
    request<Archetype>('POST', '/api/custom/archetypes', archetype),
  createCustomCareer: (career: CustomCareerInput) =>
    request<Career>('POST', '/api/custom/careers', career),

  updateCustomSkill: (id: string, skill: { system: GameSystem; name: string; characteristic: string; kind: string }) =>
    request<SkillDef>('PUT', `/api/custom/skills/${id}`, skill),
  updateCustomTalent: (id: string, talent: {
    system: GameSystem; name: string; tier: number; isRanked: boolean; category: TalentCategory; activation: string; description: string
    woundBonus: number; strainBonus: number; soakBonus: number; meleeDefenseBonus: number; rangedDefenseBonus: number
  }) => request<TalentDef>('PUT', `/api/custom/talents/${id}`, talent),
  updateCustomItem: (id: string, item: {
    system: GameSystem; name: string; kind: string; encumbrance: number; soakBonus: number
    meleeDefense: number; rangedDefense: number; encumbranceThresholdBonus: number
    description: string; price: number; rarity: number
    skillName?: string; damage?: string; crit?: string; rangeBand?: string; properties?: string
  }) => request<ItemDef>('PUT', `/api/custom/items/${id}`, item),
  updateCustomHeroicAbility: (id: string, ability: { name: string; description: string }) =>
    request<HeroicAbility>('PUT', `/api/custom/heroic-abilities/${id}`, ability),
  updateCustomArchetype: (id: string, archetype: CustomArchetypeInput) =>
    request<Archetype>('PUT', `/api/custom/archetypes/${id}`, archetype),
  updateCustomCareer: (id: string, career: CustomCareerInput) =>
    request<Career>('PUT', `/api/custom/careers/${id}`, career),

  notes: (characterId: string) =>
    request<CharacterNote[]>('GET', `/api/characters/${characterId}/notes/`),
  createNote: (characterId: string, title: string, body: string) =>
    request<CharacterNote>('POST', `/api/characters/${characterId}/notes/`, { title, body }),
  updateNote: (characterId: string, noteId: string, title: string, body: string) =>
    request<CharacterNote>('PUT', `/api/characters/${characterId}/notes/${noteId}`, { title, body }),
  deleteNote: (characterId: string, noteId: string) =>
    request<void>('DELETE', `/api/characters/${characterId}/notes/${noteId}`),

  // Профиль / аккаунт (U-21)
  account: () => request<Account>('GET', '/api/account/'),
  updateAccount: (data: { displayName?: string; avatarUrl?: string }) =>
    request<Account>('PATCH', '/api/account/', data),
  // Файл уходит сырым телом; формат (JPEG/PNG/WebP) и размер сервер проверяет по содержимому.
  uploadAvatar: (file: Blob) => request<Account>('POST', '/api/account/avatar', file),
  changePassword: (currentPassword: string, newPassword: string) =>
    request<void>('POST', '/api/account/change-password', { currentPassword, newPassword }),

  campaigns: () => request<CampaignListItem[]>('GET', '/api/campaigns/'),
  campaign: (id: string) => request<CampaignDetail>('GET', `/api/campaigns/${id}`),
  createCampaign: (name: string, description: string) =>
    request<CampaignDetail>('POST', '/api/campaigns/', { name, description }),
  joinCampaign: (joinCode: string, characterId: string) =>
    request<CampaignDetail>('POST', '/api/campaigns/join', { joinCode, characterId }),
  removeCampaignCharacter: (campaignId: string, characterId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/characters/${characterId}`),
  // GM открывает read-only лист персонажа участника своей кампании (U-20).
  campaignMemberSheet: (campaignId: string, characterId: string) =>
    request<CharacterSheet>('GET', `/api/campaigns/${campaignId}/characters/${characterId}/sheet`),
  createCampaignNote: (campaignId: string, note: { title: string; body: string; isPrivate: boolean }) =>
    request<CampaignNote>('POST', `/api/campaigns/${campaignId}/notes`, note),
  updateCampaignNote: (campaignId: string, noteId: string, note: { title: string; body: string; isPrivate: boolean }) =>
    request<CampaignNote>('PUT', `/api/campaigns/${campaignId}/notes/${noteId}`, note),
  deleteCampaignNote: (campaignId: string, noteId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/notes/${noteId}`),

  npcs: (filter: NpcFilter = {}) => {
    const params = new URLSearchParams()
    if (filter.search) params.set('search', filter.search)
    if (filter.system) params.set('system', filter.system)
    if (filter.kind) params.set('kind', filter.kind)
    if (filter.role) params.set('role', filter.role)
    if (filter.campaignId) params.set('campaignId', filter.campaignId)
    if (filter.tag) params.set('tag', filter.tag)
    if (filter.sort) params.set('sort', filter.sort)
    const qs = params.toString()
    return request<NpcListItem[]>('GET', `/api/npcs/${qs ? `?${qs}` : ''}`)
  },
  npc: (id: string) => request<NpcDetail>('GET', `/api/npcs/${id}`),
  createNpc: (input: NpcInput) => request<NpcDetail>('POST', '/api/npcs/', input),
  updateNpc: (id: string, input: NpcInput) => request<NpcDetail>('PUT', `/api/npcs/${id}`, input),
  deleteNpc: (id: string) => request<void>('DELETE', `/api/npcs/${id}`),
  duplicateNpc: (id: string) => request<NpcDetail>('POST', `/api/npcs/${id}/duplicate`),
  quickDraftNpc: (req: QuickDraftRequest) => request<NpcDetail>('POST', '/api/npcs/quick-draft', req),
  // Live preview черновика: генерация тем же кодом, что и создание, но без сохранения.
  previewQuickDraftNpc: (req: QuickDraftRequest) => request<NpcDetail>('POST', '/api/npcs/quick-draft/preview', req),
  applyNpcTemplate: (input: NpcInput, template: CreatureTemplate) =>
    request<NpcDetail>('POST', '/api/npcs/apply-template', { input, template }),

  // Игровой стол / GM Cockpit (сцена кампании). GET возвращает 204 → null, если активной сцены нет.
  session: async (campaignId: string): Promise<GameSession | null> => {
    const r = await request<GameSession | undefined>('GET', `/api/campaigns/${campaignId}/session/`)
    return r ?? null
  },
  createSession: (campaignId: string, body: { name: string; description: string; playerStoryPoints: number; gmStoryPoints: number }) =>
    request<GameSession>('POST', `/api/campaigns/${campaignId}/session/`, body),
  updateSession: (campaignId: string, patch: {
    name?: string; description?: string; publicNotes?: string; gmNotes?: string
    playerStoryPoints?: number; gmStoryPoints?: number; allowPlayerEdits?: boolean
  }) => request<GameSession>('PATCH', `/api/campaigns/${campaignId}/session/`, patch),
  resetSession: (campaignId: string) => request<GameSession>('POST', `/api/campaigns/${campaignId}/session/reset`),
  nextTurn: (campaignId: string) => request<GameSession>('POST', `/api/campaigns/${campaignId}/session/next-turn`),
  endSession: (campaignId: string) => request<void>('DELETE', `/api/campaigns/${campaignId}/session/`),

  addParticipant: (campaignId: string, body: AddParticipantRequest) =>
    request<GameSession>('POST', `/api/campaigns/${campaignId}/session/participants`, body),
  updateParticipant: (campaignId: string, participantId: string, patch: UpdateParticipantRequest) =>
    request<GameSession>('PATCH', `/api/campaigns/${campaignId}/session/participants/${participantId}`, patch),
  removeParticipant: (campaignId: string, participantId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/session/participants/${participantId}`),
  activateAbility: (campaignId: string, participantId: string, abilityCode: string) =>
    request<ActivateAbilityResult>('POST',
      `/api/campaigns/${campaignId}/session/participants/${participantId}/activate`, { abilityCode }),

  // Лог бросков стола (U-08).
  rolls: (campaignId: string, take = 30) =>
    request<RollLogEntry[]>('GET', `/api/campaigns/${campaignId}/rolls/?take=${take}`),
  createRoll: (campaignId: string, body: CreateRollRequest) =>
    request<RollLogEntry>('POST', `/api/campaigns/${campaignId}/rolls/`, body),

  addSlot: (campaignId: string, body: { slotType: InitiativeSlotType; assignedParticipantId?: string | null; notes?: string }) =>
    request<GameSession>('POST', `/api/campaigns/${campaignId}/session/slots`, body),
  updateSlot: (campaignId: string, slotId: string, patch: { slotType?: InitiativeSlotType; order?: number; assignedParticipantId?: string | null; notes?: string }) =>
    request<GameSession>('PATCH', `/api/campaigns/${campaignId}/session/slots/${slotId}`, patch),
  removeSlot: (campaignId: string, slotId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/session/slots/${slotId}`),

  // Encounter Builder (подготовка сцен кампании).
  encounters: (campaignId: string, filter: EncounterFilter = {}) => {
    const params = new URLSearchParams()
    if (filter.search) params.set('search', filter.search)
    if (filter.type) params.set('type', filter.type)
    if (filter.tag) params.set('tag', filter.tag)
    const qs = params.toString()
    return request<EncounterListItem[]>('GET', `/api/campaigns/${campaignId}/encounters/${qs ? `?${qs}` : ''}`)
  },
  encounter: (id: string) => request<EncounterDetail>('GET', `/api/encounters/${id}`),
  createEncounter: (campaignId: string, input: EncounterInput) =>
    request<EncounterDetail>('POST', `/api/campaigns/${campaignId}/encounters/`, input),
  updateEncounter: (id: string, input: EncounterInput) =>
    request<EncounterDetail>('PUT', `/api/encounters/${id}`, input),
  deleteEncounter: (id: string) => request<void>('DELETE', `/api/encounters/${id}`),
  addEncounterParticipant: (id: string, body: AddEncounterParticipantRequest) =>
    request<EncounterDetail>('POST', `/api/encounters/${id}/participants`, body),
  addEncounterCharacters: (id: string, characterIds: string[] | null) =>
    request<EncounterDetail>('POST', `/api/encounters/${id}/participants/characters`, { characterIds }),
  updateEncounterParticipant: (id: string, participantId: string, patch: UpdateEncounterParticipantRequest) =>
    request<EncounterDetail>('PATCH', `/api/encounters/${id}/participants/${participantId}`, patch),
  removeEncounterParticipant: (id: string, participantId: string) =>
    request<void>('DELETE', `/api/encounters/${id}/participants/${participantId}`),
  sendEncounterToTable: (id: string, mode: SendToTableMode) =>
    request<GameSession>('POST', `/api/encounters/${id}/send-to-table`, { mode }),

  // Campaign Handbook / Content Packs.
  contentPacks: (campaignId: string) =>
    request<ContentPackListItem[]>('GET', `/api/campaigns/${campaignId}/content-packs/`),
  contentPack: (id: string) => request<ContentPackDetail>('GET', `/api/content-packs/${id}`),
  createContentPack: (campaignId: string, body: { name: string; description: string; system: GameSystem }) =>
    request<ContentPackDetail>('POST', `/api/campaigns/${campaignId}/content-packs/`, body),
  updateContentPack: (id: string, patch: { name?: string; description?: string; system?: GameSystem; isPublicToCampaign?: boolean }) =>
    request<ContentPackDetail>('PATCH', `/api/content-packs/${id}`, patch),
  deleteContentPack: (id: string) => request<void>('DELETE', `/api/content-packs/${id}`),
  addContentPackEntry: (id: string, input: ContentPackEntryInput) =>
    request<ContentPackDetail>('POST', `/api/content-packs/${id}/entries`, input),
  updateContentPackEntry: (id: string, entryId: string, input: ContentPackEntryInput) =>
    request<ContentPackDetail>('PUT', `/api/content-packs/${id}/entries/${entryId}`, input),
  removeContentPackEntry: (id: string, entryId: string) =>
    request<void>('DELETE', `/api/content-packs/${id}/entries/${entryId}`),

  homebrewPacks: () => request<HomebrewPackListItem[]>('GET', '/api/homebrew-packs/'),
  exportHomebrewPack: (id: string) => request<HomebrewPackDocument>('GET', `/api/homebrew-packs/${id}/export`),
  importHomebrewPack: (document: HomebrewPackDocument) =>
    request<HomebrewPackImportResult>('POST', '/api/homebrew-packs/import', document),
  shareHomebrewPack: (id: string) => request<HomebrewPackShare>('POST', `/api/homebrew-packs/${id}/share`),
  importSharedHomebrewPack: (token: string) =>
    request<HomebrewPackImportResult>('POST', `/api/homebrew-packs/shared/${encodeURIComponent(token)}/import`),
  setHomebrewPackDefault: (id: string, isEnabled: boolean) =>
    request<void>('PUT', `/api/homebrew-packs/${id}/default`, { isEnabled }),
  setCharacterHomebrewPack: (characterId: string, packId: string, isEnabled: boolean) =>
    request<void>('PUT', `/api/characters/${characterId}/homebrew-packs/${packId}`, { isEnabled }),
  setCampaignHomebrewPack: (campaignId: string, packId: string, isEnabled: boolean) =>
    request<void>('PUT', `/api/campaigns/${campaignId}/homebrew-packs/${packId}`, { isEnabled }),

  deleteCustomSkill: (id: string) => request<void>('DELETE', `/api/custom/skills/${id}`),
  deleteCustomTalent: (id: string) => request<void>('DELETE', `/api/custom/talents/${id}`),
  deleteCustomItem: (id: string) => request<void>('DELETE', `/api/custom/items/${id}`),
  deleteCustomHeroicAbility: (id: string) => request<void>('DELETE', `/api/custom/heroic-abilities/${id}`),
  deleteCustomArchetype: (id: string) => request<void>('DELETE', `/api/custom/archetypes/${id}`),
  deleteCustomCareer: (id: string) => request<void>('DELETE', `/api/custom/careers/${id}`),
}
