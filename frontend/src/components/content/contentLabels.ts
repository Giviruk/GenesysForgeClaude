import type { BaseContentCategory, CustomEntryType } from '../../api/types'
import type { IconName } from '../Icon'
import { t } from '../../i18n'

export const ENTRY_LABELS: Record<CustomEntryType, string> = {
  skill: t('Навыки', 'Skills'), talent: t('Таланты', 'Talents'), item: t('Предметы', 'Items'),
  archetype: t('Архетипы', 'Archetypes'), career: t('Карьеры', 'Careers'), heroicAbility: t('Героика', 'Heroic abilities'),
  attachment: t('Улучшения', 'Attachments'), mount: t('Транспорт', 'Mounts'),
}
export const ENTRY_ICONS: Record<CustomEntryType,IconName> = {skill:'dice',talent:'bolt',item:'package',archetype:'user',career:'users',heroicAbility:'flame',attachment:'shield',mount:'map'}
export const CATEGORY_LABELS: Record<BaseContentCategory, string> = { ...ENTRY_LABELS, magic: t('Магия', 'Magic') }
export const ENTRY_TYPES = Object.keys(ENTRY_LABELS) as CustomEntryType[]
export const BASE_CATEGORIES: BaseContentCategory[] = ['skill', 'career', 'archetype', 'talent', 'magic', 'heroicAbility', 'item']
export const EDITABLE_TYPES: CustomEntryType[] = ENTRY_TYPES.filter(x => x !== 'attachment' && x !== 'mount')

export const PROPOSAL_LABELS = { active: t('Одобрено', 'Approved'), pending: t('Ожидает решения', 'Pending'), declined: t('Отклонено', 'Declined') }
