import type {
  AuthResponse, CampaignDetail, CampaignListItem, CampaignNote, CharacterListItem, CharacterNote,
  CharacterSheet, GameSystem, HeroicAbility, ItemDef, ItemState, Reference, SkillDef, Spell, TalentDef,
} from './types'

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

async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  const token = tokenStorage.get()
  if (token) headers.Authorization = `Bearer ${token}`

  const response = await fetch(url, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    // 401 при наличии токена — сессия истекла: чистим токен и уводим на логин.
    // (на /auth/login и /auth/register токена нет, поэтому неверный пароль сюда не попадает)
    if (response.status === 401 && token) {
      tokenStorage.clear()
      onUnauthorized?.()
    }
    let message = `Ошибка ${response.status}`
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

  reference: (system: GameSystem) =>
    request<Reference>('GET', `/api/reference/${system === 'genesysCore' ? 'GenesysCore' : 'RealmsOfTerrinoth'}`),
  spells: (system: GameSystem) =>
    request<Spell[]>('GET', `/api/spells/${system === 'genesysCore' ? 'GenesysCore' : 'RealmsOfTerrinoth'}`),

  characters: () => request<CharacterListItem[]>('GET', '/api/characters/'),
  createCharacter: (name: string, system: GameSystem, archetypeId: string, careerId: string, freeCareerSkillNames: string[]) =>
    request<{ id: string }>('POST', '/api/characters/', { name, system, archetypeId, careerId, freeCareerSkillNames }),
  sheet: (id: string) => request<CharacterSheet>('GET', `/api/characters/${id}`),
  deleteCharacter: (id: string) => request<void>('DELETE', `/api/characters/${id}`),
  updateCharacter: (id: string, patch: { name?: string; totalXp?: number; woundsCurrent?: number; strainCurrent?: number; money?: number }) =>
    request<void>('PATCH', `/api/characters/${id}`, patch),
  completeCreation: (id: string) => request<void>('POST', `/api/characters/${id}/complete-creation`),

  buyCharacteristic: (id: string, characteristic: string) =>
    request<void>('POST', `/api/characters/${id}/characteristics/${characteristic}/buy`),
  buySkillRank: (id: string, skillDefId: string) =>
    request<void>('POST', `/api/characters/${id}/skills/${skillDefId}/buy-rank`),
  buyTalent: (id: string, talentDefId: string, characteristic?: string) =>
    request<void>('POST', `/api/characters/${id}/talents/buy`, { talentDefId, characteristic }),
  refundCharacteristic: (id: string, characteristic: string) =>
    request<void>('POST', `/api/characters/${id}/characteristics/${characteristic}/refund`),
  refundSkillRank: (id: string, skillDefId: string) =>
    request<void>('POST', `/api/characters/${id}/skills/${skillDefId}/refund-rank`),
  refundTalent: (id: string, talentDefId: string) =>
    request<void>('POST', `/api/characters/${id}/talents/refund`, { talentDefId }),
  setHeroicAbility: (id: string, heroicAbilityId: string | null) =>
    request<void>('PUT', `/api/characters/${id}/heroic-ability`, { heroicAbilityId }),

  // cost — сколько монет списать при покупке; не передавайте (или 0) для бесплатного добавления.
  addItem: (id: string, itemDefId: string, quantity: number, state: ItemState, cost?: number) =>
    request<{ id: string }>('POST', `/api/characters/${id}/items`, { itemDefId, quantity, state, cost }),
  updateItem: (id: string, itemId: string, patch: { state?: ItemState; quantity?: number }) =>
    request<void>('PATCH', `/api/characters/${id}/items/${itemId}`, patch),
  sellItem: (id: string, itemId: string, quantity: number, proceeds: number) =>
    request<void>('POST', `/api/characters/${id}/items/${itemId}/sell`, { quantity, proceeds }),
  removeItem: (id: string, itemId: string) =>
    request<void>('DELETE', `/api/characters/${id}/items/${itemId}`),

  createCustomSkill: (skill: { system: GameSystem; name: string; characteristic: string; kind: string }) =>
    request<SkillDef>('POST', '/api/custom/skills', skill),
  createCustomTalent: (talent: {
    system: GameSystem; name: string; tier: number; isRanked: boolean; activation: string; description: string
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

  updateCustomSkill: (id: string, skill: { system: GameSystem; name: string; characteristic: string; kind: string }) =>
    request<SkillDef>('PUT', `/api/custom/skills/${id}`, skill),
  updateCustomTalent: (id: string, talent: {
    system: GameSystem; name: string; tier: number; isRanked: boolean; activation: string; description: string
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

  notes: (characterId: string) =>
    request<CharacterNote[]>('GET', `/api/characters/${characterId}/notes/`),
  createNote: (characterId: string, title: string, body: string) =>
    request<CharacterNote>('POST', `/api/characters/${characterId}/notes/`, { title, body }),
  updateNote: (characterId: string, noteId: string, title: string, body: string) =>
    request<CharacterNote>('PUT', `/api/characters/${characterId}/notes/${noteId}`, { title, body }),
  deleteNote: (characterId: string, noteId: string) =>
    request<void>('DELETE', `/api/characters/${characterId}/notes/${noteId}`),

  campaigns: () => request<CampaignListItem[]>('GET', '/api/campaigns/'),
  campaign: (id: string) => request<CampaignDetail>('GET', `/api/campaigns/${id}`),
  createCampaign: (name: string, description: string) =>
    request<CampaignDetail>('POST', '/api/campaigns/', { name, description }),
  joinCampaign: (joinCode: string, characterId: string) =>
    request<CampaignDetail>('POST', '/api/campaigns/join', { joinCode, characterId }),
  removeCampaignCharacter: (campaignId: string, characterId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/characters/${characterId}`),
  createCampaignNote: (campaignId: string, note: { title: string; body: string; isPrivate: boolean }) =>
    request<CampaignNote>('POST', `/api/campaigns/${campaignId}/notes`, note),
  updateCampaignNote: (campaignId: string, noteId: string, note: { title: string; body: string; isPrivate: boolean }) =>
    request<CampaignNote>('PUT', `/api/campaigns/${campaignId}/notes/${noteId}`, note),
  deleteCampaignNote: (campaignId: string, noteId: string) =>
    request<void>('DELETE', `/api/campaigns/${campaignId}/notes/${noteId}`),

  deleteCustomSkill: (id: string) => request<void>('DELETE', `/api/custom/skills/${id}`),
  deleteCustomTalent: (id: string) => request<void>('DELETE', `/api/custom/talents/${id}`),
  deleteCustomItem: (id: string) => request<void>('DELETE', `/api/custom/items/${id}`),
  deleteCustomHeroicAbility: (id: string) => request<void>('DELETE', `/api/custom/heroic-abilities/${id}`),
}
