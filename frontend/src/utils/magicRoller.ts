import type { Spell } from '../api/types'
import type { AdvantageSpendOption } from './advantageSpends'

/** Контекстные траты преимуществ для уже собранного магического действия. */
export function magicAdvantageSpends(action: Spell, effects: Spell[]): AdvantageSpendOption[] {
  const options: AdvantageSpendOption[] = []
  const add = (option: AdvantageSpendOption) => {
    if (!options.some(existing => existing.id === option.id)) options.push(option)
  }

  if (action.nameEn === 'Heal') {
    add({
      id: 'magic-heal-strain',
      cost: 1,
      labelRu: 'Восстановить цели 1 усталость',
      labelEn: 'Heal 1 strain on the target',
      detailRu: 'По 1 усталости за каждое потраченное преимущество.',
      detailEn: 'One strain per advantage spent.',
      requiresSuccess: true,
    })
  }

  for (const effect of effects) {
    if (effect.nameEn === 'Additional Target') {
      add({
        id: 'magic-additional-target',
        cost: 1,
        labelRu: 'Воздействовать ещё на одну цель в пределах дистанции',
        labelEn: 'Affect one more target within range',
        detailRu: 'Можно повторять, оплачивая каждую дополнительную цель отдельно.',
        detailEn: 'Repeatable; pay separately for every additional target.',
        requiresSuccess: true,
      })
    }
    if (effect.nameEn === 'Additional Summon') {
      add({
        id: 'magic-additional-summon',
        cost: 1,
        labelRu: 'Призвать ещё один предмет, оружие или существо',
        labelEn: 'Conjure one more item, weapon, or creature',
        detailRu: 'Можно повторять, оплачивая каждый дополнительный призыв отдельно.',
        detailEn: 'Repeatable; pay separately for every additional conjuration.',
        requiresSuccess: true,
      })
    }
    if (effect.nameEn === 'Additional Illusion') {
      add({
        id: 'magic-additional-illusion',
        cost: 2,
        labelRu: 'Создать ещё одну иллюзию или замаскировать ещё одного персонажа',
        labelEn: 'Create one more illusion or disguise one more character',
        detailRu: 'Можно повторять, оплачивая каждую иллюзию отдельно.',
        detailEn: 'Repeatable; pay separately for every illusion.',
        requiresSuccess: true,
      })
    }
    if (effect.nameEn === 'Realism') {
      add({
        id: 'magic-realism',
        cost: 2,
        labelRu: 'Ещё на 1 повысить сложность распознавания иллюзии',
        labelEn: 'Increase the difficulty to identify the illusion by 1 more',
        detailRu: 'Можно повторять.', detailEn: 'Repeatable.',
        requiresSuccess: true,
      })
    }
    if (effect.nameEn === 'Additional Questions') {
      add({
        id: 'magic-additional-question',
        cost: 2,
        labelRu: 'Задать ещё один вопрос',
        labelEn: 'Ask one more question',
        detailRu: 'Можно повторять, оплачивая каждый вопрос отдельно.',
        detailEn: 'Repeatable; pay separately for every question.',
        requiresSuccess: true,
      })
    }
    if (action.nameEn === 'Predict' && effect.nameEn === 'Empowered') {
      add({
        id: 'magic-empowered-predict',
        cost: 3,
        labelRu: 'Усилить выгоду предсказания до двух бонусных и двух штрафных костей',
        labelEn: 'Improve Predict’s benefit to two boost and two setback dice',
        requiresSuccess: true,
      })
    }

    const activated: Record<string, AdvantageSpendOption[]> = {
      Blast: [{
        id: 'magic-blast', cost: 2,
        labelRu: 'Активировать «Взрыв»', labelEn: 'Activate Blast',
        detailRu: 'Нанести попадание подходящим соседним целям.', detailEn: 'Hit eligible nearby targets.',
        requiresSuccess: true,
      }],
      Ice: [{
        id: 'magic-ensnare', cost: 2,
        labelRu: 'Активировать «Сковывание»', labelEn: 'Activate Ensnare',
        requiresSuccess: true,
      }],
      Lightning: [
        {
          id: 'magic-stun', cost: 2,
          labelRu: 'Активировать «Оглушение»', labelEn: 'Activate Stun',
          requiresSuccess: true,
        },
        {
          id: 'magic-auto-fire', cost: 2,
          labelRu: 'Активировать дополнительное попадание «Автоматического»',
          labelEn: 'Activate an additional Auto-fire hit',
          detailRu: 'Каждое дополнительное попадание оплачивается отдельно.',
          detailEn: 'Pay separately for every additional hit.',
          requiresSuccess: true,
        },
      ],
      Fire: [{
        id: 'magic-burn', cost: 2,
        labelRu: 'Активировать «Жжение»', labelEn: 'Activate Burn',
        requiresSuccess: true,
      }],
      Deadly: [{
        id: 'magic-critical', cost: 2,
        labelRu: 'Нанести критическую травму', labelEn: 'Inflict a critical injury',
        detailRu: 'Только если попадание нанесло урон после поглощения.',
        detailEn: 'Only if the hit dealt damage past soak.',
        requiresSuccess: true,
      }],
      Impact: [
        {
          id: 'magic-knockdown', cost: 1,
          costLabelRu: '+ разница силуэтов', costLabelEn: '+ silhouette difference',
          labelRu: 'Активировать «Нокдаун»', labelEn: 'Activate Knockdown',
          detailRu: 'Добавьте 1 преимущество за каждый пункт силуэта цели выше 1.',
          detailEn: 'Add 1 advantage for every point of the target’s silhouette above 1.',
          requiresSuccess: true,
        },
        {
          id: 'magic-disorient', cost: 2,
          labelRu: 'Активировать «Дезориентацию»', labelEn: 'Activate Disorient',
          requiresSuccess: true,
        },
      ],
      Manipulative: [{
        id: 'magic-manipulative', cost: 1,
        labelRu: 'Переместить цель на одну категорию дистанции',
        labelEn: 'Move the target one range band',
        requiresSuccess: true,
      }],
      Destructive: [{
        id: 'magic-sunder', cost: 1,
        labelRu: 'Активировать «Повреждение»', labelEn: 'Activate Sunder',
        detailRu: 'Этот эффект разрешён даже при промахе.', detailEn: 'This effect may be activated even on a miss.',
      }],
    }
    for (const option of activated[effect.nameEn] ?? []) add(option)
  }

  return options
}
