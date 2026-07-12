/**
 * Справочник свойств (качеств) предметов и оружия Genesys / Realms of Terrinoth.
 * Используется для всплывающих подсказок (тултипов) у названий свойств в инвентаре и магазине.
 *
 * Описания — собственный пересказ механик (не официальный текст книги), на языке интерфейса.
 * Свойства хранятся в каталоге предметов строкой вида «Точное 1, Оборонительное 2»;
 * здесь они сопоставляются по названию (русскому, английскому или алиасу) и снабжаются
 * человекочитаемым описанием.
 */

import { t } from '../i18n'

export interface ItemQuality {
  /** Каноническое русское название свойства. */
  nameRu: string
  /** Английское название (как в Core Rulebook). */
  nameEn: string
  /** Есть ли у свойства рейтинг (число после названия). */
  rated: boolean
  /** Описание механики свойства (на языке интерфейса). */
  description: string
  /**
   * Альтернативные написания, встречающиеся в каталоге предметов
   * (нормализуются и тоже сопоставляются с этим свойством).
   */
  aliases?: string[]
}

/** Название свойства на языке интерфейса. */
export const qualityName = (q: ItemQuality) => t(q.nameRu, q.nameEn)

export const ITEM_QUALITIES: ItemQuality[] = [
  {
    nameRu: 'Автоматическое', nameEn: 'Auto-fire', rated: false,
    description: t(
      'Позволяет вести непрерывный огонь и поражать одну цель несколько раз или несколько заранее ' +
      'объявленных целей. При использовании свойства сложность боевой проверки увеличивается на 1. ' +
      'При успехе каждое срабатывание (2 преимущества) даёт дополнительное попадание с базовым уроном оружия.',
      'Allows continuous fire, hitting one target several times or several pre-declared targets. ' +
      'Using this quality increases the difficulty of the combat check by 1. On success, each ' +
      'activation (2 Advantage) scores an additional hit dealing the weapon\'s base damage.',
    ),
  },
  {
    nameRu: 'Бронебойное', nameEn: 'Breach', rated: true,
    description: t(
      'Попадание игнорирует броню транспорта: 1 пункт за каждый ранг свойства. В персональном масштабе ' +
      'каждый ранг обычно игнорирует 10 пунктов поглощения. Рассчитано на тяжёлое и транспортное вооружение.',
      'A hit ignores vehicle armor: 1 point per rank of the quality. At personal scale each rank ' +
      'typically ignores 10 points of soak. Meant for heavy and vehicle-mounted weapons.',
    ),
  },
  {
    nameRu: 'Взрыв', nameEn: 'Blast', rated: true,
    description: t(
      'При успешной атаке и активации (2 преимущества) все персонажи вплотную к исходной цели получают ' +
      'попадание с уроном, равным рейтингу Взрыва + неотменённые успехи. При промахе можно потратить ' +
      '3 преимущества: цель и соседи получают урон, равный только рейтингу Взрыва.',
      'On a successful attack with activation (2 Advantage), every character engaged with the original ' +
      'target suffers a hit dealing damage equal to the Blast rating plus uncancelled successes. On a ' +
      'miss, 3 Advantage may be spent: the target and those adjacent suffer damage equal to the Blast rating only.',
    ),
  },
  {
    nameRu: 'Высококритичное', nameEn: 'Vicious', rated: true,
    description: t(
      'При нанесении критической травмы или критического попадания добавляет +10 к броску по таблице ' +
      'критических травм за каждый ранг свойства. Работает, только когда крит уже вызывается по обычным правилам.',
      'When a critical injury or critical hit is inflicted, adds +10 to the roll on the critical injury ' +
      'table per rank of the quality. Only applies when a crit is already triggered by the normal rules.',
    ),
  },
  {
    nameRu: 'Громоздкое', nameEn: 'Cumbersome', rated: true,
    description: t(
      'Для нормального использования нужна Сила (Мощь) не ниже рейтинга свойства. За каждый пункт Мощи ' +
      'ниже рейтинга владелец увеличивает сложность всех проверок с этим оружием на 1.',
      'Requires Brawn no lower than the quality\'s rating to use properly. For each point of Brawn ' +
      'below the rating, the wielder increases the difficulty of all checks with this weapon by 1.',
    ),
  },
  {
    nameRu: 'Дезориентация', nameEn: 'Disorient', rated: true, aliases: ['Дезориентирующее'],
    description: t(
      'При активации (2 преимущества) цель становится дезориентированной на число раундов, равное ' +
      'рейтингу свойства. Дезориентированная цель добавляет штрафную кость к своим проверкам.',
      'On activation (2 Advantage) the target is disoriented for a number of rounds equal to the ' +
      'quality\'s rating. A disoriented target adds a Setback die to its checks.',
    ),
  },
  {
    nameRu: 'Жжение', nameEn: 'Burn', rated: true,
    description: t(
      'При активации (2 преимущества) цель загорается: в начале каждого своего хода в течение числа ' +
      'раундов, равного рейтингу, она получает базовый урон оружия (уменьшается поглощением). Цель может ' +
      'действием попытаться прекратить эффект проверкой Координации.',
      'On activation (2 Advantage) the target catches fire: at the start of each of its turns, for a ' +
      'number of rounds equal to the rating, it suffers the weapon\'s base damage (reduced by soak). ' +
      'The target may spend an action on a Coordination check to end the effect.',
    ),
  },
  {
    nameRu: 'Залповое', nameEn: 'Linked', rated: true, aliases: ['Сцепленное'],
    description: t(
      'Оружие способно наносить несколько попаданий по одной цели. Каждое срабатывание (2 преимущества) ' +
      'даёт дополнительное попадание по исходной цели с базовым уроном оружия. Число доп. попаданий не ' +
      'превышает рейтинг свойства.',
      'The weapon can score multiple hits on a single target. Each activation (2 Advantage) adds another ' +
      'hit on the original target with the weapon\'s base damage. Extra hits cannot exceed the quality\'s rating.',
    ),
  },
  {
    nameRu: 'Захват', nameEn: 'Tractor', rated: true,
    description: t(
      'Оружие удерживает цель лучом или полем. Транспорт не может перемещаться, пока пилот не пройдёт ' +
      'проверку управления (сложность зависит от рейтинга). Персонаж-цель становится обездвиженным на время эффекта.',
      'The weapon holds the target with a beam or field. A vehicle cannot move until the pilot passes a ' +
      'piloting check (difficulty depends on the rating). A targeted character is immobilized while the effect lasts.',
    ),
  },
  {
    nameRu: 'Медленное', nameEn: 'Slow-Firing', rated: true,
    description: t(
      'После выстрела оружию нужно время на перезарядку или подготовку. Рейтинг показывает, сколько ' +
      'раундов должно пройти, прежде чем оружие можно использовать снова.',
      'After firing, the weapon needs time to recharge or reset. The rating shows how many rounds ' +
      'must pass before the weapon can be used again.',
    ),
  },
  {
    nameRu: 'Наведение', nameEn: 'Guided', rated: true,
    description: t(
      'Если атака промахнулась, можно активировать (3 преимущества): снаряд продолжает отслеживать цель ' +
      'и в конце раунда совершает повторную боевую проверку, используя кости по рейтингу Наведения.',
      'If the attack misses, it may be activated (3 Advantage): the projectile keeps tracking the target ' +
      'and at the end of the round makes another combat check using dice equal to the Guided rating.',
    ),
  },
  {
    nameRu: 'Неточное', nameEn: 'Inaccurate', rated: true,
    description: t(
      'При атаке этим оружием добавляется одна штрафная кость за каждый ранг свойства. Отражает грубое, ' +
      'плохо сбалансированное или неудобное в наведении оружие.',
      'Attacks with this weapon add one Setback die per rank of the quality. Represents crude, poorly ' +
      'balanced or hard-to-aim weapons.',
    ),
  },
  {
    nameRu: 'Низкопробное', nameEn: 'Inferior', rated: false,
    description: t(
      'Предмет низкого качества: при всех проверках с этим оружием или предметом автоматически добавляется ' +
      '1 угроза к результату. Отражает ненадёжное, плохо сделанное или изношенное снаряжение.',
      'A low-quality item: all checks with this weapon or item automatically add 1 Threat to the result. ' +
      'Represents unreliable, poorly made or worn-out gear.',
    ),
  },
  {
    nameRu: 'Нокдаун', nameEn: 'Knockdown', rated: false,
    description: t(
      'При активации (2 преимущества + 1 за каждый силуэт цели выше 1) цель, по которой попала атака, ' +
      'становится распластанной (сбитой с ног).',
      'On activation (2 Advantage, plus 1 per silhouette of the target above 1) the target hit by the ' +
      'attack is knocked prone.',
    ),
  },
  {
    nameRu: 'Оборонительное', nameEn: 'Defensive', rated: true,
    description: t(
      'Предмет увеличивает ближнюю защиту владельца на значение рейтинга свойства, пока удерживается ' +
      'или используется соответствующим образом.',
      'The item increases the wielder\'s melee defense by the quality\'s rating while held or used appropriately.',
    ),
  },
  {
    nameRu: 'Оглушающий урон', nameEn: 'Stun Damage', rated: false,
    description: t(
      'Оружие наносит урон усталости вместо ран. Поскольку это именно урон, он уменьшается поглощением цели ' +
      '(в отличие от свойства «Оглушение», которое игнорирует поглощение).',
      'The weapon deals strain instead of wounds. Since this is damage, it is reduced by the target\'s ' +
      'soak (unlike the Stun quality, which ignores soak).',
    ),
  },
  {
    nameRu: 'Оглушение', nameEn: 'Stun', rated: true, aliases: ['Оглушающее'],
    description: t(
      'При активации (2 преимущества) цель получает усталость, равную рейтингу свойства. Это не урон ' +
      'усталости, поэтому поглощение цели не применяется.',
      'On activation (2 Advantage) the target suffers strain equal to the quality\'s rating. This is not ' +
      'strain damage, so the target\'s soak does not apply.',
    ),
  },
  {
    nameRu: 'Ограниченный боезапас', nameEn: 'Limited Ammo', rated: true,
    description: t(
      'Оружие можно использовать число раз, равное рейтингу, после чего нужен манёвр перезарядки и ' +
      'подходящие боеприпасы. Для одноразовых предметов (гранаты) рейтинг 1 означает расход предмета.',
      'The weapon may be used a number of times equal to the rating, after which it requires a reload ' +
      'maneuver and suitable ammunition. For single-use items (grenades) a rating of 1 means the item is expended.',
    ),
  },
  {
    nameRu: 'Отражающее', nameEn: 'Deflection', rated: true,
    description: t(
      'Предмет увеличивает дальнюю защиту владельца на значение рейтинга свойства. Обычно щиты, парирование ' +
      'снарядов или защитные поля.',
      'The item increases the wielder\'s ranged defense by the quality\'s rating. Usually shields, ' +
      'projectile parrying or protective fields.',
    ),
  },
  {
    nameRu: 'Ошеломление', nameEn: 'Concussive', rated: true,
    description: t(
      'При активации (2 преимущества) цель становится ошеломлённой на число раундов, равное рейтингу ' +
      'свойства. Ошеломлённая цель не может совершать действия.',
      'On activation (2 Advantage) the target is staggered for a number of rounds equal to the ' +
      'quality\'s rating. A staggered target cannot perform actions.',
    ),
  },
  {
    nameRu: 'Повреждение', nameEn: 'Sunder', rated: false,
    description: t(
      'Активация (1 преимущество) повреждает открыто используемый предмет цели на одну ступень ' +
      '(целый → слабое → среднее → сильное → уничтожен). Можно активировать даже при промахе.',
      'Activation (1 Advantage) damages an item the target is openly using by one step ' +
      '(undamaged → minor → moderate → major → destroyed). Can be activated even on a miss.',
    ),
  },
  {
    nameRu: 'Подготовка', nameEn: 'Prepare', rated: true,
    description: t(
      'Перед использованием предмета нужно потратить число манёвров, равное рейтингу свойства ' +
      '(для оружия использование — это атака). Ведущий может потребовать повторной подготовки после перемещения.',
      'Before using the item you must spend a number of maneuvers equal to the quality\'s rating ' +
      '(for a weapon, "use" means an attack). The GM may require preparing again after moving.',
    ),
  },
  {
    nameRu: 'Превосходное', nameEn: 'Superior', rated: false,
    description: t(
      'Оружие или предмет высокого качества: при всех проверках с ним автоматически добавляется ' +
      '1 преимущество к результату.',
      'A high-quality weapon or item: all checks with it automatically add 1 Advantage to the result.',
    ),
  },
  {
    nameRu: 'Проникающее', nameEn: 'Pierce', rated: true,
    description: t(
      'Попадание игнорирует поглощение цели в размере, равном рейтингу свойства. Если рейтинг выше ' +
      'поглощения, лишнее значение не даёт дополнительного эффекта.',
      'A hit ignores the target\'s soak up to the quality\'s rating. If the rating exceeds the soak, ' +
      'the excess has no additional effect.',
    ),
  },
  {
    nameRu: 'Сковывание', nameEn: 'Ensnare', rated: true,
    description: t(
      'При активации (2 преимущества) цель становится обездвиженной на число раундов, равное рейтингу ' +
      '(не может совершать манёвры). В свой ход цель может действием пройти сложную проверку Атлетики, чтобы освободиться.',
      'On activation (2 Advantage) the target is immobilized for a number of rounds equal to the rating ' +
      '(cannot perform maneuvers). On its turn the target may spend an action on a Hard Athletics check to break free.',
    ),
  },
  {
    nameRu: 'Сноровка', nameEn: 'Unwieldy', rated: true,
    description: t(
      'Для нормального использования нужна Ловкость не ниже рейтинга свойства. За каждый пункт Ловкости ' +
      'ниже рейтинга владелец увеличивает сложность всех проверок с этим оружием на 1.',
      'Requires Agility no lower than the quality\'s rating to use properly. For each point of Agility ' +
      'below the rating, the wielder increases the difficulty of all checks with this weapon by 1.',
    ),
  },
  {
    nameRu: 'Точное', nameEn: 'Accurate', rated: true,
    description: t(
      'Оружием проще целиться: за каждый ранг свойства атакующий добавляет 1 бонусную кость к боевым ' +
      'проверкам с этим оружием.',
      'The weapon is easier to aim: for each rank of the quality the attacker adds 1 Boost die to ' +
      'combat checks with this weapon.',
    ),
  },
  {
    nameRu: 'Укреплённое', nameEn: 'Reinforced', rated: false,
    description: t(
      'Оружие или предмет невосприимчивы к свойству «Повреждение». Броня с этим свойством невосприимчива ' +
      'к «Бронебойному» и «Проникающему». Важно для магических предметов, щитов и древнего снаряжения.',
      'The weapon or item is immune to the Sunder quality. Armor with this quality is immune to Breach ' +
      'and Pierce. Matters for magic items, shields and ancient gear.',
    ),
  },
]

/** Нормализация названия свойства для поиска: нижний регистр, ё→е, без рейтинга и лишних пробелов. */
function normalizeName(raw: string): string {
  return raw
    .toLowerCase()
    .replace(/ё/g, 'е')
    .replace(/\s+\d+\s*$/, '') // отбросить завершающий рейтинг («Точное 1» → «точное»)
    .trim()
}

const QUALITY_BY_NAME: Map<string, ItemQuality> = (() => {
  const map = new Map<string, ItemQuality>()
  for (const q of ITEM_QUALITIES) {
    map.set(normalizeName(q.nameRu), q)
    map.set(normalizeName(q.nameEn), q)
    for (const alias of q.aliases ?? []) map.set(normalizeName(alias), q)
  }
  return map
})()

/** Одно разобранное свойство из строки каталога. */
export interface ParsedProperty {
  /** Исходный текст токена («Точное 1»). */
  raw: string
  /** Найденное описание свойства (если сопоставлено). */
  quality: ItemQuality | null
  /** Рейтинг свойства, если указан числом. */
  rating: number | null
}

/** Ищет описание свойства по его названию (с рейтингом или без). */
export function findQuality(name: string): ItemQuality | null {
  return QUALITY_BY_NAME.get(normalizeName(name)) ?? null
}

/**
 * Разбирает строку свойств каталога («Точное 1, Оборонительное 2, Нокдаун»)
 * в список токенов с привязанными описаниями.
 */
export function parseProperties(properties: string | null | undefined): ParsedProperty[] {
  if (!properties) return []
  return properties
    .split(',')
    .map(part => part.trim())
    .filter(Boolean)
    .map(raw => {
      const match = raw.match(/(\d+)\s*$/)
      return {
        raw,
        quality: findQuality(raw),
        rating: match ? Number(match[1]) : null,
      }
    })
}

/**
 * Нормализованные теги предмета из строки свойств: каноничное имя свойства (на языке
 * интерфейса) без рейтинга («Оборонительное 2» → «Оборонительное»). Используется как
 * набор тегов для фильтра магазина, пока нет отдельного поля тегов. Дубликаты убираются.
 */
export function itemTags(properties: string | null | undefined): string[] {
  const tags = parseProperties(properties)
    .map(p => (p.quality ? qualityName(p.quality) : p.raw.replace(/\s*\d+\s*$/, '')).trim())
    .filter(Boolean)
  return [...new Set(tags)]
}
