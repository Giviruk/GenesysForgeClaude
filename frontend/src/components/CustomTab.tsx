import { useState } from 'react'
import { api } from '../api/client'
import type {
  Archetype, Career, GameSystem,
  HeroicAbility, ItemDef, Reference, SkillDef, TalentDef,
} from '../api/types'
import {
  CHARACTERISTIC_LABELS, ITEM_KIND_LABELS, SKILL_KIND_LABELS,
  TALENT_CATEGORY_LABELS,
} from '../utils/labels'
import { t } from '../i18n'
import { CustomEditedDate } from './HomebrewPackHistory'

import { SkillForm, TalentForm, ItemForm, ArchetypeForm, CareerForm, HeroicForm } from './CustomContentForms'

interface Props {
  campaignId?: string
  system: GameSystem
  reference: Reference
  onError: (message: string) => void
  refresh: () => Promise<void>
}

type Section = 'skill' | 'talent' | 'item' | 'heroic' | 'archetype' | 'career'

export function CustomTab({ campaignId, system, reference, onError, refresh }: Props) {
  const [section, setSection] = useState<Section>('skill')
  const [notice, setNotice] = useState<string | null>(null)
  // редактируемый объект текущей секции (null — режим создания)
  const [editingSkill, setEditingSkill] = useState<SkillDef | null>(null)
  const [editingTalent, setEditingTalent] = useState<TalentDef | null>(null)
  const [editingItem, setEditingItem] = useState<ItemDef | null>(null)
  const [editingHeroic, setEditingHeroic] = useState<HeroicAbility | null>(null)
  const [editingArchetype, setEditingArchetype] = useState<Archetype | null>(null)
  const [editingCareer, setEditingCareer] = useState<Career | null>(null)

  async function run(action: () => Promise<unknown>, successMessage: string) {
    setNotice(null)
    try {
      await action()
      await refresh()
      setNotice(successMessage)
    } catch (err) {
      onError(err instanceof Error ? err.message : t('Ошибка', 'Error'))
      return false
    }
  }

  const customSkills = reference.skills.filter(s => s.isCustom)
  const customTalents = reference.talents.filter(t => t.isCustom)
  const customItems = reference.items.filter(i => i.isCustom)
  const customHeroics = reference.heroicAbilities.filter(h => h.isCustom)
  const customArchetypes = reference.archetypes.filter(a => a.isCustom)
  const customCareers = reference.careers.filter(c => c.isCustom)

  return (
    <div>
      <section className="panel">
        <h3>{t('Кастомный контент', 'Custom content')}</h3>
        <p className="hint">
          {t(
            'Создавайте контент для своего аккаунта. Мастер может подключить набор к кампании, чтобы участники использовали его. Удаление недоступно, пока контент используется персонажем.',
            'Create content for your account. A GM can enable a pack for a campaign so its members can use it. Content in use by a character cannot be deleted.',
          )}
        </p>
        <div className="tabs">
          <button className={section === 'skill' ? 'tab active' : 'tab'} onClick={() => setSection('skill')}>{t('Навыки', 'Skills')}</button>
          <button className={section === 'talent' ? 'tab active' : 'tab'} onClick={() => setSection('talent')}>{t('Таланты', 'Talents')}</button>
          <button className={section === 'item' ? 'tab active' : 'tab'} onClick={() => setSection('item')}>{t('Предметы', 'Items')}</button>
          <button className={section === 'archetype' ? 'tab active' : 'tab'} onClick={() => setSection('archetype')}>{t('Архетипы', 'Archetypes')}</button>
          <button className={section === 'career' ? 'tab active' : 'tab'} onClick={() => setSection('career')}>{t('Карьеры', 'Careers')}</button>
          {system === 'realmsOfTerrinoth' && (
            <button className={section === 'heroic' ? 'tab active' : 'tab'} onClick={() => setSection('heroic')}>{t('Героич. способности', 'Heroic abilities')}</button>
          )}
        </div>
        {notice && <div className="notice">{notice}</div>}

        {section === 'skill' && (
          <>
            <SkillForm key={editingSkill?.id ?? 'new'} campaignId={campaignId} system={system} run={run} editing={editingSkill}
              onDone={() => setEditingSkill(null)} />
            <CustomList lastEditedAt={reference.customLastEditedAt} editableIds={reference.editableCustomIds} items={customSkills.map(s => ({ id: s.id, label: `${s.name} · ${CHARACTERISTIC_LABELS[s.characteristic]} · ${SKILL_KIND_LABELS[s.kind]}` }))}
              onEdit={id => setEditingSkill(customSkills.find(s => s.id === id)!)}
              onDelete={id => run(() => api.deleteCustomSkill(campaignId, id), t('Навык удалён.', 'Skill deleted.'))} />
          </>
        )}
        {section === 'talent' && (
          <>
            <TalentForm key={editingTalent?.id ?? 'new'} campaignId={campaignId} system={system} run={run} editing={editingTalent}
              onDone={() => setEditingTalent(null)} />
            <CustomList lastEditedAt={reference.customLastEditedAt} editableIds={reference.editableCustomIds} items={customTalents.map(tal => ({
              id: tal.id,
              label: `${tal.name} · ${TALENT_CATEGORY_LABELS[tal.category]} · ${t('Тир', 'Tier')} ${tal.tier}${tal.isRanked ? t(' · ранговый', ' · ranked') : ''}`,
            }))}
              onEdit={id => setEditingTalent(customTalents.find(tal => tal.id === id)!)}
              onDelete={id => run(() => api.deleteCustomTalent(campaignId, id), t('Талант удалён.', 'Talent deleted.'))} />
          </>
        )}
        {section === 'item' && (
          <>
            <ItemForm key={editingItem?.id ?? 'new'} campaignId={campaignId} system={system} reference={reference} run={run} editing={editingItem}
              onDone={() => setEditingItem(null)} />
            <CustomList lastEditedAt={reference.customLastEditedAt} editableIds={reference.editableCustomIds} items={customItems.map(i => ({ id: i.id, label: `${i.name} · ${ITEM_KIND_LABELS[i.kind]} · ${t('вес', 'enc.')} ${i.encumbrance}` }))}
              onEdit={id => setEditingItem(customItems.find(i => i.id === id)!)}
              onDelete={id => run(() => api.deleteCustomItem(campaignId, id), t('Предмет удалён.', 'Item deleted.'))} />
          </>
        )}
        {section === 'heroic' && system === 'realmsOfTerrinoth' && (
          <>
            <HeroicForm key={editingHeroic?.id ?? 'new'} campaignId={campaignId} run={run} editing={editingHeroic}
              onDone={() => setEditingHeroic(null)} />
            <CustomList lastEditedAt={reference.customLastEditedAt} editableIds={reference.editableCustomIds} items={customHeroics.map(h => ({ id: h.id, label: h.name }))}
              onEdit={id => setEditingHeroic(customHeroics.find(h => h.id === id)!)}
              onDelete={id => run(() => api.deleteCustomHeroicAbility(campaignId, id), t('Способность удалена.', 'Ability deleted.'))} />
          </>
        )}
        {section === 'archetype' && (
          <>
            <ArchetypeForm key={editingArchetype?.id ?? 'new'} campaignId={campaignId} system={system} run={run}
              editing={editingArchetype} onDone={() => setEditingArchetype(null)} />
            <CustomList lastEditedAt={reference.customLastEditedAt} editableIds={reference.editableCustomIds} items={customArchetypes.map(a => ({ id: a.id, label: `${a.nameRu || a.name} · XP ${a.startingXp}` }))}
              onEdit={id => setEditingArchetype(customArchetypes.find(a => a.id === id)!)}
              onDelete={id => run(() => api.deleteCustomArchetype(campaignId, id), t('Архетип удалён.', 'Archetype deleted.'))} />
          </>
        )}
        {section === 'career' && (
          <>
            <CareerForm key={editingCareer?.id ?? 'new'} campaignId={campaignId} system={system} reference={reference} run={run}
              editing={editingCareer} onDone={() => setEditingCareer(null)} />
            <CustomList lastEditedAt={reference.customLastEditedAt} editableIds={reference.editableCustomIds} items={customCareers.map(c => ({ id: c.id, label: `${c.nameRu || c.name} · ${c.careerSkillNames.length} ${t('навыков', 'skills')}` }))}
              onEdit={id => setEditingCareer(customCareers.find(c => c.id === id)!)}
              onDelete={id => run(() => api.deleteCustomCareer(campaignId, id), t('Карьера удалена.', 'Career deleted.'))} />
          </>
        )}

      </section>
    </div>
  )
}

function CustomList({ items, editableIds, lastEditedAt, onEdit, onDelete }: {
  lastEditedAt?: Record<string, string>
  editableIds?: string[]
  items: { id: string; label: string }[]
  onEdit: (id: string) => void
  onDelete: (id: string) => void
}) {
  if (items.length === 0) return <p className="hint">{t('Пока нет своего контента в этом разделе.', 'No custom content in this section yet.')}</p>
  return (
    <div className="custom-list">
      <div className="label-line">{t('Ваш контент', 'Your content')} ({items.length}):</div>
      {items.map(it => (
        <div key={it.id} className="custom-list-row">
          <span>{it.label}{lastEditedAt?.[it.id] && <> · <CustomEditedDate at={lastEditedAt[it.id]} /></>}</span>
          <span className="custom-list-actions">
            {editableIds && !editableIds.includes(it.id) ? <span className="muted">{t('Контент автора', 'Author content')}</span> : <>
              <button className="small" onClick={() => onEdit(it.id)}>{t('Изменить', 'Edit')}</button>
              <button className="danger small" onClick={() => { if (confirm(t(`Удалить «${it.label}»?`, `Delete "${it.label}"?`))) onDelete(it.id) }}>{t('Удалить', 'Delete')}</button>
            </>}
          </span>
        </div>
      ))}
    </div>
  )
}
