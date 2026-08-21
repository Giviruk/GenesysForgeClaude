import type {
  Account,
  AuthResponse, AuthProviders, CampaignChronicleChapter, CampaignChronicleRevision, CampaignDetail, CampaignListItem, CampaignNote, CharacterListItem, CharacterNote,
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
    invalidateCharacterCache()
    invalidateCharacterList()
  },
}

export class ApiError extends Error {
  status: number
  reasonCode?: string
  constructor(status: number, message: string, reasonCode?: string) {
    super(message)
    this.status = status
    this.reasonCode = reasonCode
  }
}

export const API_TIMING_EVENT = 'genesysforge:api-timing'

export interface ApiTimingDetail {
  method: string
  url: string
  durationMs: number
  ok: boolean
  status?: number
  responseBytes?: number
  serverTiming?: string
}

const timingNow = () => globalThis.performance?.now?.() ?? Date.now()

function reportApiTiming(detail: ApiTimingDetail) {
  if (typeof window === 'undefined' || typeof CustomEvent === 'undefined') return
  window.dispatchEvent(new CustomEvent<ApiTimingDetail>(API_TIMING_EVENT, { detail }))
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
 * Кэш листа персонажа на время жизни вкладки. Полный лист нужен печати и магазину, а части —
 * обычному листу: общий кэш не даёт им повторно читать один и тот же персонаж после перехода между
 * вкладками. В памяти хранятся Promise, чтобы параллельные открытия одного персонажа склеивались.
 */
const characterSheetCache = new Map<string, Promise<CharacterSheet>>()
const characterSlicesCache = new Map<string, SheetSlices>()
const characterSlicesRequests = new Map<string, Promise<SheetSlices>>()
const characterNotesCache = new Map<string, Promise<CharacterNote[]>>()
const characterAuditCache = new Map<string, Promise<CharacterAuditEntry[]>>()
const characterCraftingCache = new Map<string, Promise<CraftingProject[]>>()

type CachedSheetPart = SheetSliceName | 'talentTierCounts'
const SHEET_SLICE_NAMES: CachedSheetPart[] = ['base', 'items', 'talents', 'talentTierCounts', 'mounts', 'attachments']

const hasCachedSlice = (slices: SheetSlices | undefined, name: CachedSheetPart) => slices?.[name] != null

/** Объединяет только реально загруженные части: null означает «не запрашивали», а не «пусто». */
function mergeCharacterSlices(prev: SheetSlices | undefined, got: SheetSlices): SheetSlices {
  return {
    base: got.base ?? prev?.base,
    items: got.items ?? prev?.items,
    talents: got.talents ?? prev?.talents,
    talentTierCounts: got.talentTierCounts ?? prev?.talentTierCounts,
    mounts: got.mounts ?? prev?.mounts,
    attachments: got.attachments ?? prev?.attachments,
  }
}

function slicesFromSheet(sheet: CharacterSheet): SheetSlices {
  const { items, talents, talentTierCounts, mounts, attachments, ...base } = sheet
  return { base, items, talents, talentTierCounts, mounts, attachments }
}

/** Восстанавливает полный лист только когда загружены все его части. */
function sheetFromSlices(slices: SheetSlices | undefined): CharacterSheet | null {
  if (!slices?.base || !SHEET_SLICE_NAMES.slice(1).every(name => hasCachedSlice(slices, name))) return null
  return {
    ...slices.base,
    items: slices.items!,
    talents: slices.talents!,
    talentTierCounts: slices.talentTierCounts!,
    mounts: slices.mounts!,
    attachments: slices.attachments!,
  }
}

function cacheCharacterSlices(characterId: string, got: SheetSlices): SheetSlices {
  const merged = mergeCharacterSlices(characterSlicesCache.get(characterId), got)
  characterSlicesCache.set(characterId, merged)
  return merged
}

/** Сбрасывает кэш одного персонажа или всех персонажей (например, при смене пользователя). */
export const invalidateCharacterCache = (characterId?: string) => {
  if (!characterId) {
    characterSheetCache.clear()
    characterSlicesCache.clear()
    characterSlicesRequests.clear()
    characterNotesCache.clear()
    characterAuditCache.clear()
    characterCraftingCache.clear()
    freshSlices.clear()
    return
  }
  characterSheetCache.delete(characterId)
  characterSlicesCache.delete(characterId)
  for (const key of characterSlicesRequests.keys()) {
    if (key.startsWith(`${characterId}|`)) characterSlicesRequests.delete(key)
  }
  characterNotesCache.delete(characterId)
  for (const key of characterAuditCache.keys()) {
    if (key.startsWith(`${characterId}|`)) characterAuditCache.delete(key)
  }
  characterCraftingCache.delete(characterId)
  freshSlices.delete(characterId)
}

let characterListCache: Promise<CharacterListItem[]> | null = null

/** Сбрасывает кэш списка персонажей после создания, удаления или правки листа. */
export const invalidateCharacterList = () => { characterListCache = null }

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
 * Хранятся по персонажам и забираются один раз: `takeFreshSlices` отдаёт и очищает запись. Поэтому
 * обновление после правки использует её ответ, а параллельные правки разных персонажей не могут
 * случайно подменить лист друг друга.
 */
const freshSlices = new Map<string, SheetSlices>()

/** Просить ли части листа в ответе: у правок конкретного персонажа они и нужны. */
const wantsSheetBack = (method: string, url: string) =>
  isCharacterMutation(method, url)

/** POST-preview вычисляет данные, но ничего не меняет и не должен сбрасывать кэш. */
const isCharacterReadOnlyAction = (url: string) =>
  url === '/api/characters/import/preview' || url.endsWith('/crafting/preview')

const isCharacterMutation = (method: string, url: string) =>
  method !== 'GET' && characterIdOf(url) !== null && !isCharacterReadOnlyAction(url)

const isCharacterCollectionMutation = (method: string, url: string) =>
  method !== 'GET'
  && url.startsWith('/api/characters/')
  && !isCharacterReadOnlyAction(url)

/** Персонаж, которого правит этот запрос: `/api/characters/{id}` и всё, что под ним. */
function characterIdOf(url: string): string | null {
  // Хвост обязателен не всегда: правка денег и опыта уходит в сам `/api/characters/{id}`.
  const match = /^\/api\/characters\/([^/?]+)(?:[/?]|$)/.exec(url)
  // Коллекционный маршрут `/import` не принадлежит существующему персонажу.
  return match && match[1] !== 'import' ? match[1] : null
}

/**
 * Забирает части, приехавшие с последней правкой этого персонажа, — или `null`, если их нет.
 * Одноразовые: второй вызов вернёт `null`, чтобы устаревшие данные не осели в интерфейсе.
 */
export function takeFreshSlices(characterId: string): SheetSlices | null {
  const hit = freshSlices.get(characterId) ?? null
  freshSlices.delete(characterId)
  return hit
}

type RequestMetrics = Pick<ApiTimingDetail, 'status' | 'responseBytes' | 'serverTiming'>

async function requestCore<T>(
  method: string, url: string, body?: unknown, retried = false, metrics: RequestMetrics = {},
): Promise<T> {
  const hadToken = tokenStorage.get() !== null
  const response = await rawFetch(method, url, body)
  metrics.status = response.status
  const contentLengthHeader = response.headers.get('Content-Length')
  const contentLength = contentLengthHeader === null ? Number.NaN : Number(contentLengthHeader)
  metrics.responseBytes = Number.isFinite(contentLength) && contentLength >= 0 ? contentLength : undefined
  metrics.serverTiming = response.headers.get('Server-Timing') ?? undefined

  if (!response.ok) {
    // 401 на защищённом запросе: пробуем тихо обновить access-токен по refresh-cookie и повторить.
    // (на /api/auth/* не обновляем — там неверный логин/refresh сам по себе)
    if (response.status === 401 && !isAuthPath(url)) {
      if (!retried && await tryRefresh()) {
        return requestCore<T>(method, url, body, true, metrics)
      }
      if (hadToken) {
        tokenStorage.clear()
        onUnauthorized?.()
      }
    }
    let message = t(`Ошибка ${response.status}`, `Error ${response.status}`)
    let reasonCode: string | undefined
    try {
      const data = await response.json() as { message?: unknown; reasonCode?: unknown }
      if (typeof data?.message === 'string' && data.message) message = data.message
      if (typeof data?.reasonCode === 'string' && data.reasonCode) reasonCode = data.reasonCode
    } catch {
      // тело не JSON — оставляем статус
    }
    throw new ApiError(response.status, message, reasonCode)
  }
  // Сбрасываем только после успеха: неудавшаяся правка каталог не меняла.
  if (invalidatesReference(method, url)) invalidateReference()
  const editedCharacterId = isCharacterMutation(method, url) ? characterIdOf(url) : null
  if (isCharacterCollectionMutation(method, url)) invalidateCharacterList()
  if (editedCharacterId !== null) {
    // Если сервер не вернёт части, следующий экран должен получить свежий лист. Ответ с частями
    // ниже сразу положит их обратно в кэш и избавит от этого запроса.
    invalidateCharacterCache(editedCharacterId)
  }
  if (response.status === 204) return undefined as T

  const data = await response.json()
  // Части, приехавшие вместе с правкой: запоминаем, чтобы обновление обошлось без запроса.
  if (editedCharacterId !== null && wantsSheetBack(method, url) && isSlices(data)) {
    cacheCharacterSlices(editedCharacterId, data)
    freshSlices.set(editedCharacterId, data)
  }
  return data as T
}

/**
 * Клиентская длительность полного API-действия, включая чтение JSON и прозрачный refresh/retry.
 * Событие можно собирать в devtools/телеметрии без привязки API-клиента к конкретному провайдеру.
 */
async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const startedAt = timingNow()
  const metrics: RequestMetrics = {}
  try {
    const result = await requestCore<T>(method, url, body, false, metrics)
    reportApiTiming({ method, url, durationMs: timingNow() - startedAt, ok: true, ...metrics })
    return result
  } catch (error) {
    reportApiTiming({
      method, url, durationMs: timingNow() - startedAt, ok: false,
      ...metrics, status: error instanceof ApiError ? error.status : metrics.status,
    })
    throw error
  }
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

  characters: () => {
    if (characterListCache) return characterListCache
    const pending = request<CharacterListItem[]>('GET', '/api/characters/')
      .catch((err: unknown) => {
        if (characterListCache === pending) characterListCache = null
        throw err
      })
    characterListCache = pending
    return pending
  },
  createCharacter: (name: string, system: GameSystem, archetypeId: string, careerId: string,
    freeCareerSkillNames: string[], archetypeSkillChoices: ArchetypeSkillChoice[] = [],
    careerGearChoices: CareerGearChoice[] = [], bio: CharacterBio = {},
    startingEquipmentMode: StartingEquipmentMode = 'standardMoney',
    speciesAbilityChoiceCode?: string) =>
    request<{ id: string }>('POST', '/api/characters/',
      { name, system, archetypeId, careerId, freeCareerSkillNames, archetypeSkillChoices, careerGearChoices,
        startingEquipmentMode, speciesAbilityChoiceCode, ...bio }),
  /** Весь лист сразу: нужен печати и магазину, где показывают всё разом. */
  sheet: (id: string) => {
    const cached = sheetFromSlices(characterSlicesCache.get(id))
    if (cached) return Promise.resolve(cached)
    const existing = characterSheetCache.get(id)
    if (existing) return existing

    const pending = request<CharacterSheet>('GET', `/api/characters/${id}`)
      .then(full => {
        // Инвалидация во время запроса означает, что ответ уже мог устареть.
        if (characterSheetCache.get(id) === pending) {
          characterSlicesCache.set(id, slicesFromSheet(full))
        }
        return full
      })
      .catch((err: unknown) => {
        if (characterSheetCache.get(id) === pending) characterSheetCache.delete(id)
        throw err
      })
    characterSheetCache.set(id, pending)
    return pending
  },
  /**
   * Только названные части листа. Инвентарь — две трети веса листа, и главной вкладке он не нужен;
   * вкладка берёт своё при открытии.
   */
  sheetSlices: (id: string, include: SheetSliceName[]) => {
    const missing = include.filter(name => !hasCachedSlice(characterSlicesCache.get(id), name))
    if (missing.length === 0) return Promise.resolve(characterSlicesCache.get(id) ?? {})

    // Порядок запроса важен только для читаемого URL; ключ нормализуем для дедупликации.
    const key = `${id}|${[...new Set(missing)].sort().join(',')}`
    const existing = characterSlicesRequests.get(key)
    if (existing) return existing

    const pending = request<SheetSlices>('GET', `/api/characters/${id}/slices?include=${missing.join(',')}`)
      .then(got => {
        if (characterSlicesRequests.get(key) === pending) cacheCharacterSlices(id, got)
        return got
      })
      .catch((err: unknown) => {
        if (characterSlicesRequests.get(key) === pending) characterSlicesRequests.delete(key)
        throw err
      })
    characterSlicesRequests.set(key, pending)
    return pending
  },
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
  characterAudit: (id: string, take = 100) => {
    const key = `${id}|${take}`
    const existing = characterAuditCache.get(key)
    if (existing) return existing
    const pending = request<CharacterAuditEntry[]>('GET', `/api/characters/${id}/audit?take=${take}`)
      .catch((err: unknown) => {
        if (characterAuditCache.get(key) === pending) characterAuditCache.delete(key)
        throw err
      })
    characterAuditCache.set(key, pending)
    return pending
  },
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
  crafting: (id: string) => {
    const existing = characterCraftingCache.get(id)
    if (existing) return existing
    const pending = request<CraftingProject[]>('GET', `/api/characters/${id}/crafting`)
      .catch((err: unknown) => {
        if (characterCraftingCache.get(id) === pending) characterCraftingCache.delete(id)
        throw err
      })
    characterCraftingCache.set(id, pending)
    return pending
  },
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

  createCustomSkill: (campaignId: string, skill: { system: GameSystem; name: string; characteristic: string; kind: string }) =>
    request<SkillDef>('POST', `/api/campaigns/${campaignId}/custom/skills`, skill),
  createCustomTalent: (campaignId: string, talent: {
    system: GameSystem; name: string; tier: number; isRanked: boolean; category: TalentCategory; activation: string; description: string
    woundBonus: number; strainBonus: number; soakBonus: number; meleeDefenseBonus: number; rangedDefenseBonus: number
  }) => request<TalentDef>('POST', `/api/campaigns/${campaignId}/custom/talents`, talent),
  createCustomItem: (campaignId: string, item: {
    system: GameSystem; name: string; kind: string; encumbrance: number; soakBonus: number
    meleeDefense: number; rangedDefense: number; encumbranceThresholdBonus: number
    description: string; price: number; rarity: number
    skillName?: string; damage?: string; crit?: string; rangeBand?: string; properties?: string
  }) => request<ItemDef>('POST', `/api/campaigns/${campaignId}/custom/items`, item),
  createCustomHeroicAbility: (campaignId: string, ability: { name: string; description: string }) =>
    request<HeroicAbility>('POST', `/api/campaigns/${campaignId}/custom/heroic-abilities`, ability),
  createCustomArchetype: (campaignId: string, archetype: CustomArchetypeInput) =>
    request<Archetype>('POST', `/api/campaigns/${campaignId}/custom/archetypes`, archetype),
  createCustomCareer: (campaignId: string, career: CustomCareerInput) =>
    request<Career>('POST', `/api/campaigns/${campaignId}/custom/careers`, career),

  updateCustomSkill: (campaignId: string, id: string, skill: { system: GameSystem; name: string; characteristic: string; kind: string }) =>
    request<SkillDef>('PUT', `/api/campaigns/${campaignId}/custom/skills/${id}`, skill),
  updateCustomTalent: (campaignId: string, id: string, talent: {
    system: GameSystem; name: string; tier: number; isRanked: boolean; category: TalentCategory; activation: string; description: string
    woundBonus: number; strainBonus: number; soakBonus: number; meleeDefenseBonus: number; rangedDefenseBonus: number
  }) => request<TalentDef>('PUT', `/api/campaigns/${campaignId}/custom/talents/${id}`, talent),
  updateCustomItem: (campaignId: string, id: string, item: {
    system: GameSystem; name: string; kind: string; encumbrance: number; soakBonus: number
    meleeDefense: number; rangedDefense: number; encumbranceThresholdBonus: number
    description: string; price: number; rarity: number
    skillName?: string; damage?: string; crit?: string; rangeBand?: string; properties?: string
  }) => request<ItemDef>('PUT', `/api/campaigns/${campaignId}/custom/items/${id}`, item),
  updateCustomHeroicAbility: (campaignId: string, id: string, ability: { name: string; description: string }) =>
    request<HeroicAbility>('PUT', `/api/campaigns/${campaignId}/custom/heroic-abilities/${id}`, ability),
  updateCustomArchetype: (campaignId: string, id: string, archetype: CustomArchetypeInput) =>
    request<Archetype>('PUT', `/api/campaigns/${campaignId}/custom/archetypes/${id}`, archetype),
  updateCustomCareer: (campaignId: string, id: string, career: CustomCareerInput) =>
    request<Career>('PUT', `/api/campaigns/${campaignId}/custom/careers/${id}`, career),

  notes: (characterId: string) => {
    const existing = characterNotesCache.get(characterId)
    if (existing) return existing
    const pending = request<CharacterNote[]>('GET', `/api/characters/${characterId}/notes/`)
      .catch((err: unknown) => {
        if (characterNotesCache.get(characterId) === pending) characterNotesCache.delete(characterId)
        throw err
      })
    characterNotesCache.set(characterId, pending)
    return pending
  },
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
  campaignMemberAudit: (campaignId: string, characterId: string, take = 100) =>
    request<CharacterAuditEntry[]>('GET',
      `/api/campaigns/${campaignId}/characters/${characterId}/audit?take=${take}`),
  createCampaignNote: (campaignId: string, note: { title: string; body: string; isPrivate: boolean }) =>
    request<CampaignNote>('POST', `/api/campaigns/${campaignId}/notes`, note),
  updateCampaignNote: (campaignId: string, noteId: string, note: { title: string; body: string; isPrivate: boolean }) =>
    request<CampaignNote>('PUT', `/api/campaigns/${campaignId}/notes/${noteId}`, note),
  deleteCampaignNote: (campaignId: string, noteId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/notes/${noteId}`),
  campaignChronicle: (campaignId: string) =>
    request<CampaignChronicleChapter[]>('GET', `/api/campaigns/${campaignId}/chronicle`),
  createChronicleChapter: (campaignId: string, chapter: { title: string; content: string }) =>
    request<CampaignChronicleChapter>('POST', `/api/campaigns/${campaignId}/chronicle/chapters`, chapter),
  uploadChronicleImage: (campaignId: string, file: Blob) =>
    request<{ imageUrl: string }>('POST', `/api/campaigns/${campaignId}/chronicle/images`, file),
  updateChronicleChapter: (campaignId: string, chapterId: string, chapter: { title: string; content: string; expectedVersion: number }) =>
    request<CampaignChronicleChapter>('PUT', `/api/campaigns/${campaignId}/chronicle/chapters/${chapterId}`, chapter),
  deleteChronicleChapter: (campaignId: string, chapterId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/chronicle/chapters/${chapterId}`),
  chronicleHistory: (campaignId: string, chapterId: string) =>
    request<CampaignChronicleRevision[]>('GET', `/api/campaigns/${campaignId}/chronicle/chapters/${chapterId}/history`),
  restoreChronicleRevision: (campaignId: string, chapterId: string, revisionId: string) =>
    request<CampaignChronicleChapter>('POST', `/api/campaigns/${campaignId}/chronicle/chapters/${chapterId}/restore/${revisionId}`),

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

  deleteCustomSkill: (campaignId: string, id: string) => request<void>('DELETE', `/api/campaigns/${campaignId}/custom/skills/${id}`),
  deleteCustomTalent: (campaignId: string, id: string) => request<void>('DELETE', `/api/campaigns/${campaignId}/custom/talents/${id}`),
  deleteCustomItem: (campaignId: string, id: string) => request<void>('DELETE', `/api/campaigns/${campaignId}/custom/items/${id}`),
  deleteCustomHeroicAbility: (campaignId: string, id: string) => request<void>('DELETE', `/api/campaigns/${campaignId}/custom/heroic-abilities/${id}`),
  deleteCustomArchetype: (campaignId: string, id: string) => request<void>('DELETE', `/api/campaigns/${campaignId}/custom/archetypes/${id}`),
  deleteCustomCareer: (campaignId: string, id: string) => request<void>('DELETE', `/api/campaigns/${campaignId}/custom/careers/${id}`),
}
