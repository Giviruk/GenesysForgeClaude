import type {
  Account,
  AuthResponse, AuthProviders, CampaignDetail, CampaignListItem, CampaignNote, CharacterListItem, CharacterNote,
  ActivateAbilityResult, ActivateCharacterAbilityResult, AddParticipantRequest, CharacterBio, CharacterSheet, CharacterShareResponse, CreatureTemplate, GameSession, GameSystem, HeroicAbility, HeroicOriginMode, HeroicOriginRollResult, HeroicOriginType, InitiativeSlotType, SignatureWeaponImprovement, SignatureWeaponProfile, WeaponCraftsmanship,
  Archetype, Career, CustomArchetypeInput, CustomCareerInput, ItemDef, ItemState, NpcDetail, NpcFilter, NpcInput, NpcListItem, QuickDraftRequest, Reference,
  SkillDef, Spell, TalentCategory, TalentDef, UpdateParticipantRequest,
  AddEncounterParticipantRequest, EncounterDetail, EncounterFilter, EncounterInput, EncounterListItem,
  SendToTableMode, UpdateEncounterParticipantRequest,
  ContentPackDetail, ContentPackEntryInput, ContentPackListItem,
  HomebrewPackDocument, HomebrewPackImportResult, HomebrewPackListItem, HomebrewPackShare,
  CharacterExport, ImportPreview, ImportResult,
  RollLogEntry, CreateRollRequest,
  CharacterAuditEntry,
  RulesResponse, SearchResponse, SheetSlices, SheetSliceName,
  ArchetypeSkillChoice, CareerGearChoice, StartingEquipmentMode,
  DetachOutcome, ImplementMaterial, ItemDamageState,
  CraftingPreview, CraftingProject, CraftingProjectInput, CraftingSpendChoice,
} from './types'
import { t } from '../i18n'
import { ariadneAnonymousId } from '../analytics/ariadne'

const TOKEN_KEY = 'genesysforge.token'

export const tokenStorage = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => {
    localStorage.removeItem(TOKEN_KEY)
    // Конец сессии: у следующего пользователя свой кастомный контент, чужой каталог ему не отдаём.
    invalidateReference()
  },
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
  // Просим сервер вернуть обновлённые части листа вместе с ответом на правку: иначе за ними
  // пришлось бы идти вторым запросом. Просим ровно то, что сейчас на экране, — остальное всё равно
  // перечитается при открытии своей вкладки. Сервер без этого заголовка отвечает как раньше.
  if (wantsSheetBack(method, url)) headers['X-Return-Slices'] = activeSlices.join(',')
  return fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : isBlob ? body : JSON.stringify(body),
    credentials: 'include', // отправлять/принимать refresh-cookie
  })
}

/**
 * Кэш справочника на сессию. Справочник — это каталог игры (предметы, таланты, качества…), он не
 * зависит от действий с конкретным персонажем, но лист перезапрашивал его после каждого клика:
 * ~560 КБ и около десятка запросов в БД на каждое действие. Ключ — тот же URL, что уходит на
 * сервер, поэтому контекст (персонаж, кампания) учитывается сам собой.
 *
 * Хранится именно Promise, а не результат: два одновременных запроса склеиваются в один.
 */
const referenceCache = new Map<string, Promise<Reference>>()

/** Сбрасывает кэш справочника: следующий запрос сходит на сервер. */
export const invalidateReference = () => referenceCache.clear()

/**
 * Что может изменить каталог. Правило центральное и от обратного: перечислять по одному два десятка
 * методов значит однажды забыть новый и показать игроку устаревший справочник. Лишний сброс стоит
 * один запрос, пропущенный — это баг.
 *
 * Правки самого персонажа каталог не трогают: они меняют его собственные данные, а не справочник.
 * Единственное исключение — подключение homebrew-пака: маршрут висит на персонаже
 * (`PUT /api/characters/{id}/homebrew-packs/{packId}`), но состав справочника от него зависит
 * напрямую. Поэтому проверка по `/characters/` одна его бы и пропустила.
 */
const invalidatesReference = (method: string, url: string) =>
  method !== 'GET'
  && (!url.startsWith('/api/characters/') || url.includes('/homebrew-packs/'))

/**
 * Части листа, которые сейчас показаны на экране, — их и просим вернуть в ответе на правку.
 * Задаёт лист персонажа при смене вкладки; по умолчанию — только базовая часть.
 */
let activeSlices: SheetSliceName[] = ['base']
export const setActiveSlices = (slices: SheetSliceName[]) => { activeSlices = slices }

/**
 * Части листа, приехавшие вместе с ответом на правку. Интерфейс после каждой правки всё равно
 * перечитывает лист, и это стоило отдельного обращения к серверу — на проде четверть-полсекунды
 * даже при уже установленном соединении. Сервер отдаёт их сразу, если попросить заголовком
 * `X-Return-Slices`.
 *
 * Хранятся ровно одни — самые свежие — и забираются один раз: `takeFreshSlices` их отдаёт и
 * очищает. Поэтому цепочка «правка → правка → обновление» использует результат последней правки,
 * а обновление без правки перед ним честно идёт на сервер.
 */
let freshSlices: { characterId: string; slices: SheetSlices } | null = null

/** Просить ли части листа в ответе: у правок конкретного персонажа они и нужны. */
const wantsSheetBack = (method: string, url: string) =>
  method !== 'GET' && characterIdOf(url) !== null

/** Персонаж, которого правит этот запрос: `/api/characters/{id}` и всё, что под ним. */
function characterIdOf(url: string): string | null {
  // Хвост обязателен не всегда: правка денег и опыта уходит в сам `/api/characters/{id}`.
  const match = /^\/api\/characters\/([^/?]+)(?:[/?]|$)/.exec(url)
  return match ? match[1] : null
}

/**
 * Забирает части, приехавшие с последней правкой этого персонажа, — или `null`, если их нет.
 * Одноразовые: второй вызов вернёт `null`, чтобы устаревшие данные не осели в интерфейсе.
 */
export function takeFreshSlices(characterId: string): SheetSlices | null {
  const hit = freshSlices?.characterId === characterId ? freshSlices.slices : null
  freshSlices = null
  return hit
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
  // Сбрасываем только после успеха: неудавшаяся правка каталог не меняла.
  if (invalidatesReference(method, url)) invalidateReference()
  if (response.status === 204) return undefined as T

  const data = await response.json()
  // Части, приехавшие вместе с правкой: запоминаем, чтобы обновление обошлось без запроса.
  const editedCharacterId = wantsSheetBack(method, url) ? characterIdOf(url) : null
  if (editedCharacterId !== null && isSlices(data)) {
    freshSlices = { characterId: editedCharacterId, slices: data }
  }
  return data as T
}

/**
 * Ответ на правку — это части листа, а не что-то своё. У части маршрутов группы своё тело ответа
 * (`{ id }` у покупки, ссылка у share), и подменять их нельзя. Отличаем по именам частей: пустой
 * набор тоже валиден, поэтому проверяем, что лишних ключей нет.
 */
const SLICE_KEYS = ['base', 'items', 'talents', 'talentTierCounts', 'mounts', 'attachments', 'createdId']
const isSlices = (data: unknown): data is SheetSlices =>
  typeof data === 'object' && data !== null && !Array.isArray(data)
  && Object.keys(data).length > 0
  && Object.keys(data).every(key => SLICE_KEYS.includes(key))

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
    const url = `/api/reference/${system === 'genesysCore' ? 'GenesysCore' : 'RealmsOfTerrinoth'}${qs}`

    const cached = referenceCache.get(url)
    if (cached) return cached
    // Провалившийся запрос из кэша убираем, иначе одна сетевая ошибка залипла бы на всю сессию.
    const pending = request<Reference>('GET', url)
      .catch((err: unknown) => { referenceCache.delete(url); throw err })
    referenceCache.set(url, pending)
    return pending
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
  /** Весь лист сразу: нужен печати и экспорту, где показывают всё разом. */
  sheet: (id: string) => request<CharacterSheet>('GET', `/api/characters/${id}`),
  /**
   * Только названные части листа. Инвентарь — две трети веса листа, и главной вкладке он не нужен;
   * вкладка берёт своё при открытии.
   */
  sheetSlices: (id: string, include: SheetSliceName[]) =>
    request<SheetSlices>('GET', `/api/characters/${id}/slices?include=${include.join(',')}`),
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
  /** Метнуть оружие или подобрать его обратно (ROT-WPN-01). */
  setItemThrown: (id: string, itemId: string, isThrown: boolean) =>
    request<void>('PUT', `/api/characters/${id}/items/${itemId}/thrown`, { isThrown }),
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
    /** Базовое улучшение именного оружия (ROT-HA-02). */
    baseAttachmentDefId?: string | null
  }) => request<void>('PUT', `/api/characters/${id}/heroic-configuration`, body),
  replaceSignatureWeapon: (id: string, body: {
    lost: boolean
    weaponProfile?: SignatureWeaponProfile | null
    craftsmanship?: WeaponCraftsmanship | null
    narrativeForm?: string | null
    formTraits?: string | null
    baseAttachmentDefId?: string | null
  }) => request<void>('POST', `/api/characters/${id}/heroic-configuration/signature-weapon`, body),
  /** Выбор Improved/Supreme именного оружия (ROT-HA-05): фиксируется при покупке. */
  setSignatureWeaponUpgrades: (id: string, body: {
    improvement?: SignatureWeaponImprovement | null
    supremeAttachmentDefId?: string | null
  }) => request<void>('POST', `/api/characters/${id}/heroic-configuration/signature-weapon/upgrades`, body),
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
  /**
   * Добавление предмета. `craftsmanship` выбирается один раз и дальше не меняется (ROT-WPN-02);
   * цену с его учётом считает сервер.
   */
  addItem: (id: string, itemDefId: string, quantity: number, state: ItemState,
    opts?: {
      free?: boolean; priceOverride?: number; overrideReason?: string
      /** Доля цены экземпляра при торге: 50…200 % с шагом 25. Сумму по ней считает сервер. */
      pricePercent?: number
      craftsmanship?: WeaponCraftsmanship
      /** Материал магического инструмента (ROT-MAG-MAT-01); цену с его учётом считает сервер. */
      material?: ImplementMaterial
    }) =>
    request<SheetSlices>('POST', `/api/characters/${id}/items`, { itemDefId, quantity, state, ...opts }),
  /** Услуга списывает деньги и пишется в audit, но не создаёт строку инвентаря. */
  buyService: (id: string, itemDefId: string, quantity = 1, free = false) =>
    request<void>('POST', `/api/characters/${id}/services`, { itemDefId, quantity, free }),
  updateItem: (id: string, itemId: string, patch: { state?: ItemState; quantity?: number }) =>
    request<void>('PATCH', `/api/characters/${id}/items/${itemId}`, patch),
  /**
   * Настройка магического инструмента ведущим (ROT-MAG-IMP-01): фолиант берёт до двух эффектов,
   * палочка — ровно один с надбавкой +1. Выбор делается один раз и дальше не меняется.
   */
  setImplementConfiguration: (id: string, itemId: string, effectCodes: string[], overrideReason?: string) =>
    request<void>('PUT', `/api/characters/${id}/items/${itemId}/implement`,
      { effectCodes, overrideReason }),
  /** Одноразовая настройка Lesser Rune: описание активации и ровно один эффект Runes с +1. */
  setLesserRuneConfiguration: (id: string, itemId: string, activationDescription: string,
    actionCode: string, effectCode: string) =>
    request<void>('PUT', `/api/characters/${id}/items/${itemId}/lesser-rune`,
      { activationDescription, actionCode, effectCode }),

  // ── Состояние повреждения и ремонт (GEN-EQP-DMG-01) ──
  /** Меняет состояние предмета: и Sunder в бою, и порча по сюжету приходят сюда. */
  setItemDamageState: (id: string, itemId: string, state: ItemDamageState, reason?: string) =>
    request<void>('PUT', `/api/characters/${id}/items/${itemId}/damage-state`, { state, reason }),
  /**
   * Чинит предмет по кнопке: броска проверки нет, сервер списывает материалы и возвращает
   * целое состояние. `netAdvantages` — скидка 10 % за каждое чистое преимущество.
   */
  repairItem: (id: string, itemId: string,
    opts?: { free?: boolean; netAdvantages?: number; costOverride?: number; overrideReason?: string }) =>
    request<void>('POST', `/api/characters/${id}/items/${itemId}/repair`, opts ?? {}),
  /** Меняет состояние улучшения; слот носителя при этом не освобождается. */
  setAttachmentDamageState: (id: string, attachmentId: string, state: ItemDamageState, reason?: string) =>
    request<void>('PUT', `/api/characters/${id}/attachments/${attachmentId}/damage-state`, { state, reason }),
  /** Чинит улучшение теми же правилами, что и предмет. */
  repairAttachment: (id: string, attachmentId: string,
    opts?: { free?: boolean; netAdvantages?: number; costOverride?: number; overrideReason?: string }) =>
    request<void>('POST', `/api/characters/${id}/attachments/${attachmentId}/repair`, opts ?? {}),

  // ── Улучшения предметов (ROT-EQP-ATT-01) ──
  /** Покупает улучшение в запас персонажа; сумму считает сервер. */
  buyAttachment: (id: string, attachmentDefId: string,
    opts?: { free?: boolean; priceOverride?: number; overrideReason?: string }) =>
    request<SheetSlices>('POST', `/api/characters/${id}/attachments`, { attachmentDefId, ...opts }),
  /**
   * Ставит улучшение на предмет. Броска проверки нет: правило книги показано подсказкой.
   * `overrideReason` нужен, только когда чары ставит персонаж без магического навыка.
   */
  installAttachment: (id: string, characterAttachmentId: string, hostCharacterItemId: string,
    overrideReason?: string) =>
    request<void>('POST', `/api/characters/${id}/attachments/install`,
      { characterAttachmentId, hostCharacterItemId, overrideReason }),
  /** Снимает улучшение с предмета с явным исходом. */
  detachAttachment: (id: string, attachmentId: string, outcome: DetachOutcome = 'returned', note?: string) =>
    request<void>('POST', `/api/characters/${id}/attachments/${attachmentId}/detach`, { outcome, note }),
  /** Убирает улучшение из запаса без выручки. */
  removeAttachment: (id: string, attachmentId: string) =>
    request<void>('DELETE', `/api/characters/${id}/attachments/${attachmentId}`),
  /**
   * Продажа: сумму всегда считает сервер. Один из способов — `netSuccesses` (доля по правилу),
   * `percent` (доля цены каталога) или `priceOverride` с `overrideReason` (договорная цена за
   * штуку). Без всего — полная цена каталога.
   */
  sellItem: (id: string, itemId: string, quantity: number,
    opts?: {
      netSuccesses?: number
      percent?: number
      priceOverride?: number
      overrideReason?: string
      conditionMultiplier?: number
      conditionReason?: string
    }) =>
    request<void>('POST', `/api/characters/${id}/items/${itemId}/sell`, { quantity, ...opts }),
  removeItem: (id: string, itemId: string) =>
    request<void>('DELETE', `/api/characters/${id}/items/${itemId}`),

  // ── Скакуны (ROT-MOUNT-ITEM-01) ──
  /**
   * Покупает или выдаёт скакуна: создаётся существо со статблоком, а не строка инвентаря.
   * Способы оплаты те же, что у предметов: `free` — выдача без оплаты, `pricePercent` — торг,
   * `priceOverride` с `overrideReason` — договорная цена. Сумму считает сервер.
   */
  buyMount: (id: string, mountDefId: string,
    opts?: {
      free?: boolean
      priceOverride?: number
      overrideReason?: string
      pricePercent?: number
      name?: string
    }) =>
    request<SheetSlices>('POST', `/api/characters/${id}/mounts`, { mountDefId, ...opts }),
  /**
   * Кличка, раны, «под седлом», заметка и тягловое животное. Присланные поля меняются, остальные
   * остаются. Груз здесь не трогается — для него `moveCargo`.
   */
  updateMount: (id: string, mountId: string, patch: {
    name?: string
    woundsCurrent?: number
    isActive?: boolean
    notes?: string
    drawnByMountId?: string
    clearDrawnBy?: boolean
  }) =>
    request<void>('PATCH', `/api/characters/${id}/mounts/${mountId}`, patch),
  /**
   * Переносит позицию между персонажем и транспортом (ROT-TRANSPORT-01). `mountId: null` — забрать
   * владельцу; `quantity` меньше стопки отделяет часть; `install` ставит попону или сумки.
   */
  moveCargo: (id: string, itemId: string,
    body: {
      mountId: string | null
      quantity?: number
      install?: boolean
      /** Решение ведущего поставить попону не на боевого скакуна; попадает в историю. */
      installOverrideReason?: string
    }) =>
    request<void>('PATCH', `/api/characters/${id}/items/${itemId}/location`, body),
  /** Продажа скакуна: те же три способа, что у предметов, сумму считает сервер. */
  sellMount: (id: string, mountId: string,
    opts?: {
      netSuccesses?: number
      percent?: number
      priceOverride?: number
      overrideReason?: string
      conditionMultiplier?: number
      conditionReason?: string
    }) =>
    request<void>('POST', `/api/characters/${id}/mounts/${mountId}/sell`, opts ?? {}),
  // Ремесло (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01). Доступно владельцу листа — и
  // игроку, и ведущему. Ресурсы приложение не списывает и не проверяет: они остаются описанием.
  /** Проекты персонажа, свежие сверху. */
  crafting: (id: string) =>
    request<CraftingProject[]>('GET', `/api/characters/${id}/crafting`),
  /** Сложность, время и стоимость до подтверждения; ничего не пишет. */
  craftingPreview: (id: string, body: CraftingProjectInput) =>
    request<CraftingPreview>('POST', `/api/characters/${id}/crafting/preview`, body),
  startCrafting: (id: string, body: CraftingProjectInput) =>
    request<{ id: string }>('POST', `/api/characters/${id}/crafting`, body),
  /**
   * Разрешение проекта: символы броска и распределение трат. Символы приходят из роллера, как
   * нетто-успехи при продаже; всё остальное считает сервер.
   */
  resolveCrafting: (id: string, projectId: string, body: {
    netSuccesses: number
    advantages?: number
    threats?: number
    triumphs?: number
    despairs?: number
    spends?: CraftingSpendChoice[]
  }) =>
    request<CraftingProject>('POST', `/api/characters/${id}/crafting/${projectId}/resolve`, body),
  cancelCrafting: (id: string, projectId: string) =>
    request<void>('DELETE', `/api/characters/${id}/crafting/${projectId}`),

  /** Удаляет скакуна без выручки: погиб, отпущен или заведён по ошибке. */
  removeMount: (id: string, mountId: string) =>
    request<void>('DELETE', `/api/characters/${id}/mounts/${mountId}`),

  // Критические ранения (U-23): из таблицы U-11 (ruleCode) или вручную (nameRu).
  addCriticalInjury: (id: string, body: { ruleCode?: string; nameRu?: string; severity?: string; rollResult?: number; notes?: string }) =>
    request<SheetSlices>('POST', `/api/characters/${id}/critical-injuries`, body),
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
