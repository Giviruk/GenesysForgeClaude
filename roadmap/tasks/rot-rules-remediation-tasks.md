# Genesys Forge: полная спецификация исправлений Genesys Core / Realms of Terrinoth

- **Ветка:** `feature/rot-rules-remediation-tasks`
- **База:** `master`
- **Тип работы:** документационная декомпозиция аудита; этот PR не меняет игровую логику
- **Статус:** 🚧 Ready for implementation review
- **PR:** будет указан после создания

## 0. Назначение и обязательные соглашения

Этот файл — самостоятельное техническое задание на исправление выбранных расхождений
Genesys Forge с Genesys Core Rulebook (далее Core), Realms of Terrinoth (далее RoT) и
официальной Genesys FAQ/Errata v1.1. Для реализации перечисленных ниже задач не должно
требоваться восстанавливать смысл пункта из предыдущего аудита.

**Границы скопа.** Правила, которым нужен исполняющий движок столкновения — выбор цели и
применение попадания, авторитетный серверный бросок, автоматическое списание ран и усталости,
траты символов конкретного броска, длительности и раунды — вынесены в
[rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md) вместе с их разбором. Там же записано,
какие части оставшихся здесь задач не делаются. Приложение остаётся листом персонажа со
справочниками, каталогами и калькуляторами, а столкновение ведёт человек.

### 0.1. Приоритет правил

1. Для персонажа, NPC, предмета или столкновения с системой `RealmsOfTerrinoth`
   сначала применяется явное правило RoT.
2. Если RoT не заменяет базовое правило, применяется Core. Наличие механики только в
   Core само по себе не является ошибкой.
3. Официальная FAQ/Errata v1.1 исправляет обе книги и имеет приоритет над их печатным
   текстом.
4. Expanded Player's Guide (EPG) разрешён только там, где задача прямо называет
   механику EPG. Она должна быть помечена `Optional/EPG` и не выдаваться за правило
   Core или RoT.
5. Домашнее правило разрешено только как явно включаемая GM-опция. По умолчанию оно
   выключено и не может называться «исправлением по книге».

### 0.2. Что означает «реализовано»

Пункт считается выполненным только при наличии единого правила в backend Domain /
Application, его сохранения при необходимости, безопасного API, понятного UI и
автоматических тестов. Текст в справочнике без исполняемой механики не закрывает пункт,
если эффект можно однозначно автоматизировать. UI никогда не является источником
истины и не может обходить server-side validation.

Для всех новых или изменённых команд обязательны:

- проверка владельца, системы и фазы создания;
- предварительная валидация полного запроса до первого изменения состояния;
- атомарность списания XP, Story Points, денег, действий и расходников;
- idempotency для повторной доставки одной команды;
- стабильные `code`/ID вместо сравнения отображаемых русских или английских имён;
- сохранение в duplicate, JSON export/import, печати и Game Table, если сущность там
  используется;
- versioned export и явное предупреждение для неполных legacy-данных вместо
  выдуманного значения;
- запись значимого действия в существующий audit/event log;
- domain error с машинным `reasonCode`, а не молчаливое исправление запроса.

### 0.3. Контент, режимы видимости и авторские права

Каждая встроенная запись должна иметь:

- стабильный `Code`, систему, официальный источник и печатную страницу;
- числовые/структурные параметры механики;
- короткий авторский `SafeDescription` для `PublicSafe`;
- полный авторский парафраз на русском и английском для `PrivateFull`;
- признак `Retired` там, где запись исключена из новых выборов, но нужна старым данным.

`PrivateFull` обязан отдавать **полные по механическому смыслу** парафразы всех
затронутых разделов книг: условия, стоимость, timing, цель, дальность, длительность,
ограничения, варианты и траты символов. Заглушки наподобие «эффект зависит от
описания», одно имя без правила и сокращённый текст, из которого нельзя разрешить
эффект, запрещены. Это требование распространяется на виды, таланты, Heroic
Abilities, навыки, заклинания, качества, предметы, attachments, crafting/alchemy,
противников, скакунов и справочные таблицы. Оригинальные абзацы книги копировать
нельзя: только собственный парафраз и структурные данные.

`PublicSafe` не должен случайно получить полный текст через вложенный DTO, поиск,
экспорт, print endpoint, custom-content endpoint или кэш. Интеграционный тест обязан
сравнивать все поверхности обоих режимов. Пользовательский контент
(`OwnerUserId != null`) не изменять seed-синхронизацией.

### 0.4. Общая модель механических эффектов

Таланты, видовые способности, Heroic Abilities, предметы и заклинания должны
использовать совместимый структурный словарь эффектов:

`trigger`, `timing`, `actionCost`, `resourceCost`, `frequency`, `targetFilter`,
`range`, `duration`, `diceModification`, `numericModification`, `stackingMode`,
`choiceSchema`, `usageScope`, `cleanupHook`, `sourceCode`.

Не требуется помещать всю механику в один универсальный JSON-интерпретатор. Допустимы
типизированные domain-strategy для сложных правил, но каталог всё равно должен
указывать исполняемый тип эффекта. Нельзя выводить механику парсингом текста.

---

## 1. Создание персонажа и стартовая прогрессия

### ROT-CRE-01 (аудит 1.4). Видовые карьерные навыки и общий предел рангов

#### Нормативное правило

- Deep Elf получает `Discipline 1`, а также `Knowledge (Forbidden) 2`; последний
  навык одновременно становится карьерным.
- Highborn Elf получает `Negotiation 1` и `Divine 1`; `Divine` одновременно
  становится карьерным.
- До завершения создания итоговый ранг любого навыка не может быть выше 2. В предел
  входят все бесплатные ранги: вид, карьера, выбор игрока и иной источник.
- Поэтому Deep Elf не может добавить ещё один стартовый ранг `Knowledge (Forbidden)`;
  Highborn Elf может один раз добавить стартовый ранг `Divine` и получить ранг 2.
- Бесплатные ранги стоят 0 XP, не превращаются в оплаченные и не возвращают XP.
  После создания переход с ранга 2 на 3 для карьерного навыка стоит 15 XP.

#### Изменения

- В стартовом навыке вида хранить `GrantsCareerSkill`; `true` только у двух выдач,
  названных выше.
- Создать общий `CareerSkillResolver`: базовые навыки карьеры ∪ видовые выдачи ∪
  выдачи талантов. Дедупликация — по стабильному `SkillDefId`.
- Создание сначала строит полный план всех прибавок, проверяет итоговые ранги и только
  затем сохраняет его. При превышении возвращается ошибка; обрезать ранг до 2 нельзя.
- Resolver используют создание, buy/refund, расчёт цены, лист, импорт и магические
  ограничения. Хранимый старый флаг `CharacterSkill.IsCareer` не должен быть
  единственным источником истины.
- UI показывает источники карьерного статуса. Для Deep Elf уже заполненный
  `Knowledge (Forbidden) 2` недоступен с объяснением; для Highborn Elf `Divine`
  доступен до ранга 2.

#### Legacy, критерии и тесты

- Совпадение вида и карьеры не создаёт дубль и не выдаёт второй ранг.
- Старым персонажам исправить эффективный карьерный статус, но не менять их ранги,
  XP и историю.
- Проверить domain/API сценарии обоих эльфов, атомарный отказ при ранге 3,
  отсутствие дублей, цену ранга 3 после creation, duplicate/export/import.
- В frontend проверить badges источников, disabled reason и точный payload.
- **Миграция:** поле/metadata и безопасный backfill; persistent schema обновить в
  `docs/database.md`.

### ROT-CRE-02 (аудит 1.5). Заморозка Wound/Strain Threshold после создания

#### Нормативное правило

Пока создание не завершено, preview:

```text
Wound Threshold  = Species.WoundBase  + current Brawn
Strain Threshold = Species.StrainBase + current Willpower
```

Покупка или возврат характеристики сразу меняет preview. В момент
`CompleteCreation` оба результата фиксируются. Последующее изменение Brawn или
Willpower, включая `Dedication`, не пересчитывает сохранённые пороги. Brawn
по-прежнему меняет soak и encumbrance threshold. Только эффект, который прямо
увеличивает порог, прибавляется поверх snapshot: `Toughened` даёт +2 WT за ранг,
`Grit` — +1 ST за ранг.

#### Изменения

- Добавить к `Character` неизменяемые после completion
  `CreationWoundThreshold` и `CreationStrainThreshold`.
- `CompleteCreation` вычисляет и сохраняет оба значения в той же транзакции до
  переключения фазы; повторный вызов не делает новый snapshot.
- Один domain-calculator применяется в полном/кратком листе, print, duplicate,
  campaign и Game Table. Явные бонусы талантов считаются поверх snapshot ровно один
  раз.
- Export/import сохраняет snapshot. Старый формат без него импортируется с
  предупреждением и детерминированным вычислением, а не с нулём.

#### Legacy, критерии и тесты

- Для завершённых legacy-персонажей миграция не должна угадывать старую Brawn или
  Willpower до `Dedication`. Backfill выполняется только по следующему алгоритму:
  1. Посчитать отдельно все **явные** действующие модификаторы порога
     (`ToughenedRanks × 2` для WT, `GritRanks × 1` для ST и другие источники с
     typed threshold effect). Назовём суммы `explicitWtBonus` и `explicitStBonus`.
  2. Если event/audit log однозначно восстанавливает Brawn/Willpower на момент
     completion, snapshot равен species base плюс восстановленная characteristic.
  3. Иначе взять текущий отображаемый итоговый threshold и вычесть соответствующий
     explicit bonus: `snapshot = max(1, visibleTotal - explicitBonus)`. Это
     сохраняет видимый результат после того, как calculator снова прибавит bonus,
     и не удваивает Grit/Toughened.
  4. Если старый `visibleTotal` нельзя восстановить, использовать species base плюс
     текущую characteristic, выставить `ThresholdSnapshotProvenance =
     LegacyEstimated` и `RulesReviewRequired`; не применять это молча.
  Во всех остальных случаях provenance — `CreationCompleted` или
  `LegacyAuditReconstructed`.
- Current wounds/strain не обрезать даже при превышении нового порога.
- Тесты: изменение характеристик до и после completion; Brawn меняет soak/load, но
  не WT; Grit/Toughened; idempotent completion; одинаковый результат на всех
  поверхностях; round-trip.
- **Миграция:** две колонки и задокументированная legacy-стратегия.

### ROT-CRE-03 (аудит 1.6). Стандартные деньги или целый карьерный комплект

#### Нормативное правило

На шаге стартового снаряжения используются взаимоисключающие режимы:

1. `StandardMoney`: персонаж получает бюджет 500 silver для стартовых покупок и не
   получает автоматического карьерного комплекта. Отдельные стартовые карманные
   деньги `d100` сохраняются как реальная валюта; бюджет покупки и карманные деньги
   нельзя дважды прибавить к одному балансу.
2. `CareerPackage`: только с разрешения GM персонаж вместо бюджета 500 получает
   **весь** комплект своей RoT-карьеры: все фиксированные позиции, ровно одну опцию
   каждой группы выбора и указанную комплектом фиксированную/случайную сумму денег.
   Частичный комплект, комплект плюс 500 и выбор предметов другой карьеры запрещены.

Каталог комплекта является данными: `fixedItems`, упорядоченные `choiceGroups`,
`moneyFormula`, `source`. Формула броска выполняется сервером через инъецируемый RNG,
а фактический результат сохраняется в audit log.

Полный каталог комплектов (скобки обозначают одну option; знак `+` внутри option
означает, что выдаются все названные позиции):

| Career | Fixed | Choice groups по порядку | Money |
|---|---|---|---:|
| Disciple | Mace | `Holy Icon` **или** `Shield + Leather Armor`; `Lantern + Herbs of Healing ×2` **или** `Traveling Gear` | `1d100` |
| Envoy | Dagger; Padded Armor | `Sword` **или** `Musical Instrument`; `Fine Cloak` **или** `Traveling Gear` | `200 + 1d100` |
| Mage | — | `Magic Staff` **или** `Magic Wand`; `Dagger` **или** `Sling`; `Heavy Robes` **или** `Stamina Elixir ×1` | `1d100` |
| Primalist | — | `Staff` **или** `Greataxe + Leather Armor`; `Apothecary’s Kit` **или** `Traveling Gear` | `1d100` |
| Scholar | Dagger | `Alchemist’s Kit` **или** `Sword`; `Lantern` **или** `Herbs of Healing ×1`; `Fine Cloak` **или** `Traveling Gear` | `1d100` |
| Scoundrel | Traveling Gear | `Dagger` **или** `Cestus`; `Sword + Dagger` **или** `Bow`; `Fine Cloak` **или** `Thieves’ Tools` | `1d100` |
| Scout | Leather Armor; Traveling Gear | `Bow` **или** `Light Spear`; `Dagger` **или** `Health Elixir ×2`; `Herbs of Healing + Climbing Gear` **или** `Winter Clothing` | `1d100` |
| Warrior | Leather Armor; Health Elixir ×2; Traveling Gear | `Sword + Shield` **или** `Axe + Shield` **или** `Halberd` | `1d100` |

`Traveling Gear` — не один предмет и не контейнер. Это точная выдача:
`Backpack ×1`, `Bedroll ×1`, `Rope ×1`, `Flint and Steel ×1`, `Torch ×3`,
`Waterskin ×1` в пустом состоянии. Для оружия и implements комплект использует
steel craftsmanship и oak material, если к предмету применима эта характеристика.

#### Изменения

- Typed request:
  `startingEquipmentMode = standardMoney | careerPackage`; отсутствие поля старого
  клиента трактуется как `standardMoney`.
- В стандартном режиме непустые package choices дают 400. В режиме комплекта сервер
  требует точное множество group IDs, одну допустимую option на группу, отсутствие
  дублей и разрешение всех ItemDef. Невалидный запрос не создаёт ни персонажа, ни
  деньги, ни один предмет.
- UI показывает явный radio с безопасным default `StandardMoney`; при смене режима
  или карьеры сбрасывает устаревшие choices. Итоговый preview перечисляет каждый
  предмет и сумму.
- Provenance стартовых денег/предметов хранить так, чтобы duplicate и audit не
  считали их обычной покупкой.

#### Критерии и тесты

- Default/отсутствующее поле → 500 purchase budget, нет package items.
- Валидный пакет → нет бюджета 500, все fixed, по одной option, одна вычисленная
  сумма.
- Матрица invalid: неизвестный режим/группа/option, пропуск, дубль, чужая карьера,
  скрытый custom item, stale выбор после смены карьеры; везде zero mutations.
- Domain-тест parser денежных формул и deterministic RNG; API-тест транзакции;
  frontend-тест radio/gating/reset/payload.
- **Миграция:** не обязательна, если provenance уже хранится; иначе добавить её и
  versioned export.

### ROT-CRE-04 (аудит 1.7). Комплект Scout с учётом errata

Первый выбор Scout — ровно `Bow` **или** `Light Spear`. `Leather Armor` остаётся
отдельным фиксированным предметом и не входит в ветку копья. Полный результат:

- fixed: `Leather Armor ×1` и разложенный `Traveling Gear`;
- group 1: `Bow ×1` или `Light Spear ×1`;
- group 2: `Dagger ×1` или `Health Elixir ×2`;
- group 3: `Herbs of Healing ×1 + Climbing Gear ×1` или
  `Winter Clothing ×1`;
- money: `1d100 silver`.

- Seed заменяет встроенные дочерние строки idempotent-операцией: две однопредметные
  option и одна fixed leather; custom packages не затрагиваются.
- В результате bow-ветка даёт `Bow ×1 + Leather Armor ×1`, spear-ветка —
  `Light Spear ×1 + Leather Armor ×1`; `Leather Armor ×2` недопустим.
- Исторический инвентарь автоматически не переписывать: нельзя отличить ошибочный
  второй доспех от купленного.
- Тестировать точный seed после двух синхронизаций, оба пути allocator и UI-label.
- **Миграция:** нет; исправление seed. **Зависимость:** ROT-CRE-03.

---

## 2. Все видовые способности RoT

### ROT-SPECIES-01. Полный каталог 14 вариантов и исполняемые способности

#### Базовые профили

Порядок характеристик в таблице:
`Brawn / Agility / Intellect / Cunning / Willpower / Presence`.
WT и ST в таблице — базовые значения вида; при создании к ним прибавляются Brawn и
Willpower согласно ROT-CRE-02.

| Вид | Характеристики | WT base | ST base | XP | Бесплатные навыки |
|---|---:|---:|---:|---:|---|
| Human | 2/2/2/2/2/2 | 10 | 10 | 110 | выбор двух разных не-карьерных навыков по 1 |
| Deep Elf | 2/3/2/2/1/2 | 9 | 10 | 90 | Discipline 1; Knowledge (Forbidden) 2 |
| Free Cities Elf | 2/3/2/2/1/2 | 9 | 10 | 90 | Streetwise 1 |
| Highborn Elf | 2/3/2/2/1/2 | 9 | 10 | 90 | Negotiation 1; Divine 1 |
| Lowborn Elf | 2/3/2/2/1/2 | 9 | 10 | 90 | Survival 1 |
| Dunwarr Dwarf | 2/1/2/2/3/2 | 11 | 10 | 90 | Resilience 1 |
| Forge Dwarf | 2/1/2/2/3/2 | 11 | 10 | 90 | Negotiation 1 |
| Broken Plains Orc | 3/2/2/2/2/1 | 12 | 8 | 100 | Coercion 1 |
| Stone-Dweller Orc | 3/2/2/2/2/1 | 12 | 8 | 100 | Cool 1 |
| Sunderlands Orc | 3/2/2/2/2/1 | 12 | 8 | 100 | Alchemy 1 |
| Catfolk | 2/2/1/3/2/2 | 9 | 8 | 90 | Perception 1 |
| Half-Catfolk | 2/2/2/2/2/2 | 10 | 9 | 100 | Cool 1 |
| Burrow Gnome | 1/2/2/3/1/3 | 6 | 11 | 90 | Charm 1; Resilience 1 |
| Wanderer Gnome | 1/2/2/3/1/3 | 6 | 11 | 90 | Charm 1; Stealth 1 |

Все бесплатные ранги агрегируются по ID с другими источниками и подчиняются общему
creation cap 2. Deep Elf и Highborn Elf дополнительно получают карьерные навыки из
ROT-CRE-01. У всех видов silhouette 1, кроме обоих gnome: silhouette 0.

#### Точная матрица способностей

1. **Human — Ready for Adventure.** Один раз за игровую сессию, out-of-turn
   incidental: переместить один Story Point из GM pool в player pool. Если у GM нет
   Story Point, активация отклоняется и использование не тратится.
2. **Deep Elf.** Только названные выше навыки и карьерная выдача; не добавлять
   придуманного пассивного бонуса.
3. **Free Cities Elf — Nimble.** Устанавливает базовые melee и ranged Defense в 1.
   Это provider/set, а не `+1`: с бронёй Defense 1 итог остаётся 1 до настоящих
   additive modifiers.
4. **Highborn Elf.** Только названные навыки и карьерная выдача.
5. **Lowborn Elf — Nimble.** То же правило, что у Free Cities Elf.
6. **Dunwarr Dwarf.**
   - `Dark Vision`: убрать не более двух Setback, причина которых помечена именно
     `darkness`, из собственной skill check. Другие Setback не затрагиваются.
   - `Tough as Nails`: один раз за сессию сразу после броска новой Critical Injury,
     out-of-turn incidental; потратить один player Story Point и считать
     первоначальный результат d100 равным `01`. Модификаторы Critical Injury затем
     применяются общим pipeline.
7. **Forge Dwarf.**
   - `Stubborn`: добавить один Setback к social check, целью которой является dwarf.
   - `Tough as Nails`: идентично Dunwarr Dwarf.
8. **Broken Plains Orc — Battle Rage.** До melee attack игрок явно может добавить
   один Setback к своей проверке. Если он выбрал риск и атака успешна, ровно один hit
   получает +2 damage; при нескольких попаданиях бонус не размножается.
9. **Stone-Dweller Orc — Hot Tempered.** Эффект активен, только когда current strain
   строго больше половины текущего derived ST. На собственные social checks
   добавляются два Setback; при каждой успешной melee attack ровно один hit получает
   +1 damage. При strain, равном половине ST, эффект неактивен.
10. **Sunderlands Orc — Tenacious.** После первого успешного combat check,
    попавшего по конкретной цели, последующие combat checks владельца против той же
    цели получают один Boost до конца encounter. Метка не складывается сама с собой;
    разные цели имеют независимые метки.
11. **Catfolk.**
    - `Claws`: виртуальный natural attack, а не покупаемый предмет: Brawl,
      Damage `Brawn + 1`, Critical 3, Engaged, Vicious 1.
    - `Fleet of Paw`: если вторым maneuver в ходе является movement maneuver,
      отменить обычные 2 strain за второй maneuver. Лимит два maneuvers за ход
      сохраняется; немобильный второй maneuver оплачивается обычно.
12. **Half-Catfolk.** При создании выбрать ровно одну неизменяемую способность:
    `Claws` **или** `Fleet of Paw` с полным правилом из Catfolk. Нельзя пропустить
    выбор, получить обе или сменить после completion.
13. **Burrow Gnome.**
    - `Small`: silhouette 0.
    - `Militia Training`: один Boost к combat check, только если silhouette
      выбранной цели строго больше silhouette gnome. Число существ в группе не
      считается silhouette.
14. **Wanderer Gnome.**
    - `Small`: silhouette 0.
    - `Tricksy`: один раз за encounter, в собственный ход, потратить один player
      Story Point и получить один ранее не заявленный предмет с Encumbrance ≤ 1 и
      Rarity ≤ 4. Предмет должен существовать и быть доступен в текущем content
      scope. Оружие допустимо только тогда, когда его effective profile **уже**
      имеет `Limited Ammo 1`; Tricksy не добавляет и не изменяет качество. Поэтому
      обычный меч и иное оружие без `Limited Ammo 1` недопустимы, даже если проходят
      Enc/Rarity. Полученный предмет остаётся после encounter как обычный owned
      instance; повторное применение способности не может выдать второй предмет в
      том же encounter.

#### Реализация

- `ArchetypeAbilityDef` получает типизированный `RuleKind`, параметры, timing,
  frequency, target/condition и choice schema; запрещено распознавать эффект по
  имени или Description.
- Обязательный выбор Half-Catfolk хранится на персонаже и участвует в
  duplicate/export/import. Silhouette — отдельное числовое поле.
- Claws реализуются как virtual attack profile. Они не появляются в магазине,
  quantity и экономике.
- Единый check context включает actor, stable target, skill/action, range,
  silhouette, encounter и Setback с `sourceTag`. Это позволяет не удалить
  Dark Vision случайный штраф и не применить Militia Training без цели.
- Story Point, use counter и эффект меняются одной транзакцией. Счётчики имеют scope
  `session`/`encounter`; `EndSession` и `EndEncounter` сбрасывают нужный scope.
- Battle Rage — явный pre-roll toggle. Tough as Nails доступен только в коротком
  окне сразу после новой Critical Injury. Fleet записывает структурированные
  maneuvers. Tenacious хранит stable target ID и очищается в конце encounter.
- Tricksy создаёт `CharacterItem` с provenance, но без instance-quality override.
  Сервер читает Enc/Rarity/effective `Limited Ammo 1` из authoritative definition;
  клиентские значения не принимаются на доверии.
- Reference, sheet, print и `PrivateFull` возвращают полный парафраз и структурные
  параметры; Game Table даёт реальные controls, use counters и причины отказа.
  Вне активной сессии интерфейс может показать manual guidance, но не изображает
  успешное списание Story Point.

#### Legacy, критерии и тесты

- Старым Half-Catfolk без выбора показать `SpeciesChoiceIncomplete`; не выбирать
  способность автоматически. До исправления choice блокировать её автоматизацию.
- Legacy species rows сопоставлять по stable code; custom same-name не подменяет
  built-in.
- Domain table tests проверяют все 14 профилей и каждое значение в таблице.
- По каждой способности нужны positive/negative tests: timing, лимит, цена, цель,
  range/tag/threshold, cleanup и атомарность. Отдельно проверить Defense stacking,
  критический roll 01, один hit Battle Rage, строгое сравнение Hot Tempered,
  Tenacious reset, второй maneuver, silhouette equality, разрешённое оружие с уже
  имеющимся `Limited Ammo 1` и отказ для подходящего по Enc/Rarity оружия без него.
- API-тесты: ownership, concurrent Story spend, session/encounter reset,
  export/import/duplicate. Frontend: обязательный choice, natural attack,
  activation controls и локализованные error reasons.
- **Миграция:** metadata вида, silhouette, choice и use/target state. Старый
  Tricksy-instance с искусственно выданным `Limited Ammo 1` не переписывать молча:
  сохранить snapshot, отметить `LegacyRuleMismatch` и предложить GM review.
  Данные custom content не менять.

---

## 3. Таланты RoT, включая подходящую errata

### ROT-TAL-01. Точный состав каталога и metadata

#### Состав

В RoT scope должно быть ровно **112 активных встроенных талантов**: 107 записей
печатной таблицы плюс 5 официальных errata-добавлений.

Ниже — исчерпывающий manifest. Он является expected fixture для seed test; запись,
которой нет в таблице, не является доступным для покупки RoT PC талантом.
`Out-of-turn Incidental` — отдельный timing, не обычный Incidental. Существующие
stable codes сохраняются ради ссылочной целостности, даже если некоторые из них
исторически транслитерированы.

| Stable code | Canonical name | Tier | Activation | Ranked |
|---|---|---:|---|:---:|
| `apothecary` | Apothecary | 1 | Passive | да |
| `pokupka-informatsii` | Bought Info | 1 | Action | нет |
| `bullrush` | Bullrush | 1 | Incidental | нет |
| `challenge` | Challenge! | 1 | Maneuver | да |
| `ostroumnyy-otvet` | Clever Retort | 1 | Out-of-turn Incidental | нет |
| `dark-insight` | Dark Insight | 1 | Incidental | нет |
| `otchayannoe-vosstanovlenie` | Desperate Recovery | 1 | Passive | нет |
| `duelyant` | Duelist | 1 | Passive | нет |
| `dungeoneer` | Dungeoneer | 1 | Passive | да |
| `krepost` | Durable | 1 | Passive | да |
| `finesse` | Finesse | 1 | Incidental | нет |
| `dobytchik` | Forager | 1 | Passive | нет |
| `uporstvo` | Grit | 1 | Passive | да |
| `obezdvizhivayuschiy-vystrel` | Hamstring Shot | 1 | Action | нет |
| `podskok` | Jump Up | 1 | Incidental | нет |
| `kvalifikatsiya` | Knack for It | 1 | Passive | да |
| `torgovye-svyazi` | Know Somebody | 1 | Incidental | да |
| `poehali` | Let’s Ride | 1 | Incidental | нет |
| `edinenie-s-prirodoy` | One with Nature | 1 | Incidental | нет |
| `painful-blow` | Painful Blow | 1 | Incidental | нет |
| `parirovanie` | Parry | 1 | Out-of-turn Incidental | да |
| `precision` | Precision | 1 | Incidental | нет |
| `pristoynoe-povedenie` | Proper Upbringing | 1 | Incidental | да |
| `vyhvatyvanie` | Quick Draw | 1 | Incidental | нет |
| `bystryy-udar` | Quick Strike | 1 | Passive | да |
| `vtoroe-dyhanie` | Second Wind | 1 | Incidental | да |
| `shapeshifter` | Shapeshifter | 1 | Passive | нет |
| `shield-slam` | Shield Slam | 1 | Incidental | нет |
| `bystryy` | Swift | 1 | Passive | нет |
| `tavern-brawler` | Tavern Brawler | 1 | Passive | нет |
| `templar` | Templar | 1 | Passive | нет |
| `zakalennyy` | Toughened | 1 | Passive | да |
| `tumble` | Tumble | 1 | Incidental | нет |
| `neprimetnyy` | Unremarkable | 1 | Passive | нет |
| `adventurer` | Adventurer | 2 | Passive | нет |
| `bard` | Bard | 2 | Passive | нет |
| `berserk` | Berserk | 2 | Maneuver | нет |
| `block` | Block | 2 | Out-of-turn Incidental | нет |
| `blood-sacrifice` | Blood Sacrifice | 2 | Incidental | да |
| `bulwark` | Bulwark | 2 | Out-of-turn Incidental | нет |
| `chill-of-nordros` | Chill of Nordros | 2 | Incidental | нет |
| `skoordinirovannaya-ataka` | Coordinated Assault | 2 | Maneuver | да |
| `vstrechnoe-predlozhenie` | Counteroffer | 2 | Action | нет |
| `oboronitelnaya-taktika` | Defensive Stance | 2 | Maneuver | да |
| `dirty-tricks` | Dirty Tricks | 2 | Incidental | нет |
| `dominion-of-the-dimora` | Dominion of the Dimora | 2 | Incidental | нет |
| `oboerukiy` | Dual Wielder | 2 | Maneuver | нет |
| `encouraging-song` | Encouraging Song | 2 | Action | нет |
| `exploit` | Exploit | 2 | Incidental | да |
| `favor-of-the-fae` | Favor of the Fae | 2 | Incidental | нет |
| `flames-of-kellos` | Flames of Kellos | 2 | Incidental | нет |
| `flash-of-insight` | Flash of Insight | 2 | Passive | нет |
| `grapple` | Grapple | 2 | Incidental | нет |
| `povyshennoe-vnimanie` | Heightened Awareness | 2 | Passive | нет |
| `heroic-recovery` | Heroic Recovery | 2 | Incidental | нет |
| `hunter` | Hunter | 2 | Passive | нет |
| `impaling-strike` | Impaling Strike | 2 | Incidental | нет |
| `voodushevlyayuschaya-rech` | Inspiring Rhetoric | 2 | Action | нет |
| `izobretatel` | Inventor | 2 | Incidental | да |
| `schastlivoe-popadanie` | Lucky Strike | 2 | Incidental | нет |
| `natural-communion` | Natural Communion | 2 | Passive | нет |
| `reckless-charge` | Reckless Charge | 2 | Incidental | нет |
| `runic-lore` | Runic Lore | 2 | Passive | нет |
| `shapeshifter-improved` | Shapeshifter (Improved) | 2 | Triggered Incidental; OOT только по trigger | нет |
| `shag-v-storonu` | Side Step | 2 | Maneuver | да |
| `signature-spell` | Signature Spell | 2 | Passive | нет |
| `templar-improved` | Templar (Improved) | 2 | Passive | нет |
| `threaten` | Threaten | 2 | Out-of-turn Incidental | да |
| `well-traveled` | Well-Traveled | 2 | Passive | нет |
| `wraithbane` | Wraithbane | 2 | Passive | нет |
| `zhivotnoe-sputnik` | Animal Companion | 3 | Passive | да |
| `backstab` | Backstab | 3 | Action | нет |
| `battle-casting` | Battle Casting | 3 | Passive | нет |
| `body-guard` | Body Guard | 3 | Maneuver | да |
| `cavalier` | Cavalier | 3 | Maneuver | нет |
| `counterattack` | Counterattack | 3 | Out-of-turn Incidental | нет |
| `dissonance` | Dissonance | 3 | Action | нет |
| `uklonenie` | Dodge | 3 | Out-of-turn Incidental | да |
| `dual-strike` | Dual Strike | 3 | Incidental | нет |
| `orlinyy-glaz` | Eagle Eyes | 3 | Incidental | нет |
| `easy-prey` | Easy Prey | 3 | Maneuver | нет |
| `polevoy-komandir` | Field Commander | 3 | Action | нет |
| `geroicheskaya-volya` | Heroic Will | 3 | Out-of-turn Incidental | нет |
| `voodushevlyayuschaya-rech-uluchshennyyj` | Inspiring Rhetoric (Improved) | 3 | Passive | нет |
| `justice-of-the-citadel` | Justice of the Citadel | 3 | Incidental | нет |
| `odarennost` | Natural | 3 | Incidental | нет |
| `znatok-boleutolyayuschih` | Painkiller Specialization | 3 | Passive | да |
| `parirovanie-u-luchshennyy` | Parry (Improved) | 3 | Out-of-turn Incidental | нет |
| `potent-concoctions` | Potent Concoctions | 3 | Passive | нет |
| `precise-archery` | Precise Archery | 3 | Passive | нет |
| `pressure-point` | Pressure Point | 3 | Incidental | нет |
| `bystraya-strelba-iz-luka` | Rapid Archery | 3 | Maneuver | нет |
| `shockwave` | Shockwave | 3 | Passive | нет |
| `back-to-back` | Back-to-Back | 4 | Passive | нет |
| `mozhet-obsudim-eto` | Can’t We Talk About This? | 4 | Action | нет |
| `conduit` | Conduit | 4 | Incidental | нет |
| `metkiy-glaz` | Deadeye | 4 | Incidental | нет |
| `death-rage` | Death Rage | 4 | Passive | нет |
| `oborona` | Defensive | 4 | Passive | да |
| `vynoslivost` | Enduring | 4 | Passive | да |
| `polevoy-komandir-uluchshennyyj` | Field Commander (Improved) | 4 | Passive | нет |
| `voodushevlyayuschaya-rech-masterskiyj` | Inspiring Rhetoric (Supreme) | 4 | Incidental | нет |
| `signature-spell-improved` | Signature Spell (Improved) | 4 | Passive | нет |
| `unrelenting` | Unrelenting | 4 | Incidental | нет |
| `venom-soaked-blade` | Venom Soaked Blade | 4 | Passive | нет |
| `crushing-blow` | Crushing Blow | 5 | Incidental | нет |
| `povyshenie` | Dedication | 5 | Passive | да |
| `lets-talk-this-over` | Let’s Talk This Over | 5 | Out-of-turn Incidental | нет |
| `master` | Master | 5 | Incidental | нет |
| `retribution` | Retribution! | 5 | Out-of-turn Incidental | нет |
| `whirlwind` | Whirlwind | 5 | Action | нет |
| `zealous-fire` | Zealous Fire | 5 | Passive | нет |

Добавить отсутствующие:

- `Challenge!`: Tier 1, Combat, ranked, Active (Maneuver), once/encounter. Выбрать
  противников в Short в количестве не больше ranks; minion group считается одной
  целью. До конца encounter выбранная цель получает один Boost, атакуя владельца,
  и два Setback, атакуя кого-либо другого.
- `Let’s Talk This Over`: Tier 5, Social, unranked, Active
  (Out-of-turn Incidental), once/session. Перед началом боя с разумными существами
  выполнить Daunting (4) Charm; успех заменяет начинающийся бой social encounter.
- `Retribution!`: Tier 5, Combat, unranked, Active
  (Out-of-turn Incidental), once/round. Когда противник атакует союзника в Medium,
  потратить один player Story Point. Если противник находится в range готового
  оружия владельца, тот автоматически попадает: damage равен базовому damage
  оружия плюс применимые постоянные бонусы; успехи броска не добавляются, потому что
  броска нет.

Оставить пять errata-талантов:

| Talent | Tier | Activation | Ranked |
|---|---:|---|---|
| Second Wind | 1 | Active (Incidental) | да |
| Side Step | 2 | Active (Maneuver) | да |
| Swift | 1 | Passive | нет |
| Toughened | 1 | Passive | да |
| Unremarkable | 1 | Passive | нет |

Убрать из **новой покупки и RoT reference**, но не удалять исторические владения:
`Rapid Reaction`, `Surgeon`, `Scathing Tirade`, `Scathing Tirade (Improved)`,
`Scathing Tirade (Supreme)`, `Just in Time!`, `Indomitable`,
`Ruinous Repartee`, `Attuned`, `Counterspell`, `Empowered Casting`.
`Ruinous Repartee` остаётся NPC-only там, где оно есть у официального adversary.
Запись из Core/EPG не удаляется глобально: исключается только ошибочная
принадлежность RoT PC catalog.

#### Исправленная metadata

- `ranked = true`: Apothecary, Blood Sacrifice, Body Guard, Dungeoneer, Exploit,
  Threaten.
- `Signature Spell`: Tier 2, Magic/Fantasy, unranked, Passive.
- `Signature Spell (Improved)`: Tier 4.
- `Conduit`: Tier 4, Magic/Fantasy, unranked, Active (Incidental), once/encounter.
- Нужен отдельный `OutOfTurnIncidental`, либо комбинация
  `ActivationKind=Incidental + CanUseOutOfTurn=true`.
- Out-of-turn разрешён для Block, Bulwark, Clever Retort, Counterattack, Dodge,
  Heroic Will, Let’s Talk This Over, Parry, Parry (Improved), Retribution!,
  Threaten; у Shapeshifter (Improved) — только по его trigger.

Канонические display names при прежних stable codes:
`Can’t We Talk About This?`, `Eagle Eyes`, `Painkiller Specialization`,
`Back-to-Back`, `Chill of Nordros`, `Dominion of the Dimora`,
`Favor of the Fae`, `Flames of Kellos`, `Flash of Insight`,
`Justice of the Citadel`, `Let’s Ride`.

#### Реализация и приёмка

- `TalentDef` получает явные systems, `Retired`, category, tier, ranked,
  activation/timing/frequency. Seed — idempotent upsert по stable code.
- Retired скрыт из browser/buy, но видим на старом листе с пометкой; custom
  same-name не затрагивается.
- Reference/API/UI возвращают и показывают точные metadata, без определения системы
  по `Any`.
- Тесты фиксируют count 112, точные include/exclude/errata sets, всю таблицу
  исправленных metadata и имён, idempotent seed, сохранность custom и legacy.
- **Миграция:** безопасные catalog fields/backfill; не удалять строки, на которые
  ссылаются персонажи.

### ROT-TAL-02. Prerequisites, взаимоисключения, покупка и refund

Точные prerequisites:

| Покупаемый талант | Обязательный талант |
|---|---|
| Block | Parry |
| Blood Sacrifice | Dark Insight |
| Bulwark | Parry |
| Counterattack | Parry (Improved) |
| Shapeshifter (Improved) | Shapeshifter |
| Signature Spell (Improved) | Signature Spell |
| Templar (Improved) | Templar |
| Parry (Improved) | Parry |
| Inspiring Rhetoric (Improved) | Inspiring Rhetoric |
| Inspiring Rhetoric (Supreme) | Inspiring Rhetoric |
| Field Commander (Improved) | Field Commander |

`Inspiring Rhetoric (Supreme)` не требует промежуточный Improved, если базовый уже
есть. Взаимоисключающие пары симметричны:

- Chill of Nordros ↔ Flames of Kellos;
- Dominion of the Dimora ↔ Favor of the Fae.

Один `TalentPurchasePolicy` до mutation проверяет system/scope, retired, tier,
talent pyramid, XP, prerequisite, exclusion и choices. Refund запрещён, если
удаляемый последний ранг/талант нужен ещё имеющемуся dependent talent. Связи
сохраняются stable codes/ID; custom definition validation отклоняет missing,
cross-system link и cycle.

Illegal legacy-комбинацию не удалять: показать warning и запретить действие,
которое делает состояние хуже. API возвращает reason code и zero mutations.
UI показывает prerequisite/exclusion и disabled reason, но повторяет проверку
backend.

Тесты — table-driven для всей матрицы, оба направления exclusions, refund dependent,
XP/pyramid atomicity, custom cycle, retired/legacy. **Миграция:** link
tables/structured metadata и backfill.

### ROT-TAL-03. Обязательные сохраняемые параметры талантов

| Talent | Choice schema и правило |
|---|---|
| Dedication | На каждый ранг выбрать ровно одну characteristic, которую этот персонаж ещё не повышал посредством Dedication; +1, максимум до 5. Одну characteristic нельзя выбрать этим талантом второй раз. |
| Knack for It | Ранг 1: один skill; каждый следующий ранг: два новых skill. Только non-combat и non-magic, без повторов. Убирать до двух Setback из проверок каждого выбранного skill. |
| Lucky Strike | Одна characteristic. После успешной combat check за один Story Point добавить к одному hit damage, равный её текущему rating. |
| Heroic Recovery | Одна characteristic. Once/encounter за один Story Point heal strain, равный её текущему rating. |
| Heroic Will | Две разные characteristics. За Story Point до конца encounter игнорировать влияние Critical Injuries на checks этих характеристик; injuries не удалять. |
| Natural | Два разных skills. Once/session перебросить одну check любого выбранного skill. |
| Master | Один skill. Once/round suffer 2 strain и снизить difficulty следующей check этого skill на 2, минимум Easy (1). |
| Signature Spell | Одна magic action и точная непустая multiset-конфигурация additional effects. Порядок не важен, multiplicity важна. |
| Animal Companion | Ранг 1: один одобренный GM companion silhouette ≤ 0. Каждый новый ранг повышает допустимый silhouette на 1. Одновременно связан только один companion. |

Для `Animal Companion` в structured encounter владелец once/round тратит Maneuver:
companion, который видит и слышит владельца (обычно не дальше Medium), на ходе
владельца выполняет один Action и один Maneuver. Повышение ранга позволяет вырастить
того же companion или заменить его через отдельную GM-approved command.

#### Реализация

- Общая versioned `TalentChoiceSchema` в definition и
  `CharacterTalentChoice` с `rankIndex`, typed value, stable ID и display snapshot.
- Валидировать cardinality, тип, distinctness только там, где таблица прямо требует
  разные значения, skill kind, characteristic cap,
  silhouette и exact spell multiset до списания XP. Значения не хранить только как
  display names.
- Choice неизменяем после покупки; в creation меняется refund+repurchase.
  Ranked refund снимает последний rank choice и эффект в одной транзакции.
- Старый параметр characteristic временно принимается только как alias Dedication;
  существующие `GrantedCharacteristics` переносятся в общий формат.
- Legacy talent без обязательного choice получает `NeedsChoice`; его эффект
  блокируется до repair без повторной оплаты XP.
- Generic UI строится из schema: single/multi selectors, spell builder, companion
  form; sheet/print показывают выбор.

Тестировать всю таблицу, Knack 1/2/2, дубли и запрещённые kinds, spell multiset,
silhouette, invalid request с zero mutations, refund/backfill/round-trip.
**Миграция:** choices + backfill с временным сохранением legacy fields.

### ROT-TAL-04. Карьерные навыки, выдаваемые талантами

> **Финальное решение по объёму (2026-07-30): выполнено.** Текущая выдача карьерных навыков
> талантами, расчёт цены новых рангов и существующее поведение refund приняты владельцем как
> достаточные. Отдельный сохраняемый cost stack для каждого купленного ранга и repair старых
> покупок больше не считаются незавершённой работой `ROT-TAL-04`. Требования ниже сохраняются
> как архив аудита.

| Talent | Навыки, становящиеся карьерными |
|---|---|
| Adventurer | Athletics; Knowledge (Adventuring) |
| Bard | Knowledge (Lore); Verse |
| Hunter | Knowledge (Geography); Ranged; Survival |
| Runic Lore | Knowledge (Lore); Runes |
| Templar | Divine |
| Well-Traveled | Knowledge (Geography); Negotiation; Vigilance |

`Templar` также сохраняет своё отдельное ограничение: не более одного Divine spell
за encounter, полученного через этот талант.

Effective career set = career ∪ species grants ∪ owned talent grants. Новый skill
rank стоит `5 × newRank`, если skill сейчас карьерный, иначе
`5 × newRank + 5`. Позднее получение карьерного статуса не возвращает старую
надбавку. После refund таланта ранее купленные ranks остаются, но будущая цена
использует текущий resolver.

Чтобы refund был корректен при смене статуса, хранить фактическую цену каждой
покупки rank (`CharacterSkillRankPurchase` или эквивалентный cost stack); возвращать
последнюю фактически уплаченную сумму. Для legacy без доказуемой истории не
угадывать выгодный refund. Точная политика legacy:

- известные новые покупки записываются поверх старого unresolved prefix и могут
  возвращаться LIFO по своей фактической цене;
- когда последней становится legacy-покупка без доказанной цены, refund
  блокируется с `skill.refund.provenance_required`, но дальнейшая игра и новая
  покупка не блокируются;
- owner/GM repair показывает rank, creation grants, известные события и возможные
  цены; он требует вручную выбрать фактически уплаченную цену для **каждого**
  unresolved rank и записывает reason/audit;
- server не предлагает значение как подтверждённое и не выбирает более выгодную
  карьерную цену автоматически;
- после полного repair обычный LIFO refund работает только по сохранённому stack.

Reference DTO отдаёт grants; sheet — `effectiveIsCareer`, `careerSources`,
`nextRankCost`. Проверить каждый набор, union нескольких источников, цены
до/после buy/refund, отсутствие ретроактивного возврата, Templar/Verse eligibility.
**Миграция:** grant metadata и cost provenance. **Зависимости:** ROT-CRE-01 и
ограничение покупки magic skills.

> **Вне скопа: нужен рантайм столкновения.** Разбор сохранён в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md):
> - **ROT-TAL-05** — общий lifecycle активных талантов: session/encounter/round/turn и сбросы счётчиков.

### ROT-TAL-06. Исправить подтверждённо неверные исполняемые эффекты

Ниже приведена полная целевая механика затронутых записей. Она имеет приоритет над
текущими `Description`, `properties` и частичной автоматизацией.

1. **Challenge!, Let’s Talk This Over, Retribution!** — реализовать без отклонений
   от ROT-TAL-01.
2. **Conduit.** Once/encounter, Incidental, потратить один Story Point; следующую
   magic action персонаж выполняет как Maneuver. Сама активация не является
   Maneuver.
3. **Signature Spell.** Для сохранённой точной комбинации action + multiset effects
   снижать итоговую difficulty на 1. Improved заменяет величину на 2, а не
   складывается до 3. Талант не отменяет 2 strain за заклинание и не имеет use limit.
4. **Apothecary.** При natural rest под заботой владельца цель дополнительно лечит
   wounds в количестве `2 × ranks`.
5. **Bullrush.** После Maneuver, которым персонаж вошёл в Engaged, при Brawl,
   Melee (Light) или Melee (Heavy) check можно потратить 3 Advantage или Triumph:
   цель падает prone и отталкивается не более чем на один range band. Не добавлять
   условие успешного попадания, которого в правиле нет.
6. **Dark Insight.** Когда spell effect/quality обычно использует ranks
   Knowledge (Lore), разрешить использовать ranks Knowledge (Forbidden).
7. **Dungeoneer (errata).** После Perception, Vigilance или
   Knowledge (Adventuring), сделанной для обнаружения, распознавания или избегания
   угрозы в пещере, подземной руине или сходной среде, отменить uncanceled Threat не
   больше ranks. Это не reroll и не универсальное удаление символов.
8. **Painful Blow.** До combat check добровольно повысить difficulty на 1. Если
   цель получила хотя бы одну wound, до конца encounter она получает 2 strain
   каждый раз, когда выполняет Maneuver; Action сам по себе эффект не запускает.
9. **Blood Sacrifice.** Требует Dark Insight. До magic check персонаж получает от
   0 до ranks wounds и добавляет столько же automatic Success. Это не Boost dice.
10. **Bulwark.** Требует Parry и готовое оружие с Defensive. Когда Engaged ally
    получает melee hit, разрешает использовать Parry, чтобы уменьшить его damage;
    атака не перенаправляется на владельца.
11. **Can’t We Talk About This?** Action; одна non-Nemesis цель в Medium;
    opposed Charm или Deception против Discipline. При успехе цель не может
    атаковать владельца или совершать против него враждебные действия до конца
    следующего хода цели. За 2 Advantage продлить ещё на один ход; Triumph
    распространяет запрет на названных союзников владельца в Short. Эффект
    прекращается, если владелец или известный цели союзник атакует её; GM может
    пометить сюжетно невосприимчивую цель.
12. **Encouraging Song.** Требует инструмент; Average Charm **или** Verse. За
    каждый Success выбрать одного ally в Medium: один Boost к его следующей check.
    За каждый Advantage один уже затронутый ally лечит 1 strain. Не Setback, не
    Short и не перестановка Success/Advantage.
13. **Exploit.** Для Ranged или Melee (Light) check получить 2 strain; атака
    получает `Ensnare = ranks`.
14. **Shapeshifter и Shapeshifter (Improved).**
    - Base — passive и автоматический. Когда normal-form персонаж становится
      incapacitated именно из-за превышения ST, он out-of-turn incidental
      превращается: heal all strain; Brawn и Agility +1 (каждая максимум 5);
      Intellect и Willpower −1 (каждая минимум 1); Cunning/Presence не меняются.
      Его unarmed attack получает +1 damage и Critical 3. В форме нельзя
      использовать magic skills и делать ranged attacks. Видимая форма как минимум
      дважды upgrade difficulty социальных checks персонажа против реагирующих на
      неё NPC. Через 8 часов либо при новом incapacitation персонаж возвращается в
      normal form; один и тот же crossing threshold не запускает мгновенный цикл
      transform/revert.
    - Improved требует base. Once/session персонаж делает Hard (3) Discipline как
      Out-of-turn Incidental, чтобы либо вручную запустить весь base transform,
      либо попытаться не превратиться при обязательном trigger. Use тратится при
      броске. При успехе выбранный override срабатывает; при провале ручная
      трансформация не происходит, а неудачная попытка подавить trigger приводит к
      обычной base-трансформации. Это не отменяет attack/hit и не лечит wounds.
15. **Templar (Improved).** Требует base. Для одного Divine spell, разрешённого
    Templar, игнорировать casting Setback от тяжёлой брони (soak +2), щита и
    отсутствия свободной руки. Difficulty не снижается.
16. **Threaten.** Out-of-turn после combat check противника, которая нанесла damage
    союзнику. Владелец получает 3 strain; противник получает strain, равный ranks
    Coercion владельца. На rank 1 range Short, каждый последующий rank увеличивает
    максимум на один band.
17. **Body Guard.** Once/round Maneuver; получить от 0 до ranks strain; выбрать
    Engaged ally. До **конца** следующего хода владельца difficulty атак по ally
    upgrade столько раз.
18. **Cavalier.** Находясь верхом на battle-trained mount, once/round Maneuver
    приказывает mount выполнить Action; не дополнительный Maneuver.
19. **Counterattack.** Требует Parry (Improved). Когда его counter-hit
    срабатывает, активировать одно качество использованного оружия так, словно
    потрачены 2 Advantage. Талант не накладывает автоматический stagger.
20. **Dissonance.** Инструмент, Average Charm или Verse. Каждый Success наносит
    одной выбранной enemy в Medium 1 wound; каждый Advantage наносит одной уже
    затронутой enemy ещё 1 wound. Не strain, не Setback и не Short.
21. **Easy Prey.** Maneuver + 3 strain. До **начала** следующего хода владельца
    он и allies в Short добавляют два Boost к combat checks только против
    immobilized targets. Не один Boost и не против staggered.
22. **Justice of the Citadel.** Once/round в собственный ход получить 3 strain;
    один hit успешной melee attack получает damage + ranks Discipline. Ограничения
    «только undead/demon» нет.
23. **Crushing Blow.** Once/session после melee roll, до resolution, получить
    4 strain. Для этого resolution оружие имеет Breach 1 и Knockdown; кроме того,
    уничтожается один находящийся у цели non-Reinforced предмет. Это не Sunder и не
    простой бонус damage.
24. **Back-to-Back.** Пока персонаж Engaged хотя бы с одним ally, владелец и такие
    allies получают один Boost к combat checks. Если Engaged ally тоже владеет
    талантом, совокупный максимум — два Boost. Не Setback.
25. **Field Commander.** Action, Average Leadership. При успехе выбрать не больше
    Presence allies: каждый может получить 1 strain и немедленно сделать
    Out-of-turn Maneuver. Improved повышает предел до `2 × Presence`; за Triumph
    один выбранный ally вместо Maneuver может получить 1 strain и сделать Action.
    Тип активации остаётся Action.
26. **Painkiller Specialization.** Painkiller лечит цели на +1 wound за rank
    таланта; шестая и последующие дозы за день всё равно не лечат.
27. **Knack for It.** Использовать choice/effect из ROT-TAL-03: первый rank один
    skill, следующие по два, и до двух Setback с каждого выбранного skill.
28. **Side Step (errata).** Activation — Maneuver; эффект длится до **конца**
    следующего хода владельца.

Дополнительно проверить и сохранить следующие RoT-эффекты, чувствительные к
ошибочной замене терминов:

- `Berserk`: melee получает один automatic Success и два automatic Advantage;
  adversaries получают один automatic Success против владельца; ranged не
  затрагивается; при завершении владелец получает 6 strain.
- `Rapid Archery`: получить 2 strain; для следующей ranged check лук получает
  `Linked = ranks Ranged`. Дополнительное попадание не выдаётся автоматически.
- `Shapeshifter`: полностью моделировать автоматический trigger, профиль формы и
  пределы, а не оставлять необязательный vague text.
- `Shield Slam`: на shield attack против minion/rival потратить 4 Advantage или
  Triumph; не добавлять выдуманное условие hit.
- `Chill of Nordros`: Ice без повышения difficulty, Fire запрещён.
- `Dominion of the Dimora`: Impact бесплатен, Manipulative запрещён.
- `Favor of the Fae`: Manipulative бесплатен, Impact запрещён.
- `Flames of Kellos`: Fire бесплатен, Ice запрещён.
- `Flash of Insight`: при Triumph на knowledge check добавить результат отдельного
  броска двух Boost; исходный Triumph остаётся доступен для траты.
- `Natural Communion`: Summon Ally бесплатен, но только для местных естественных
  животных.
- `Battle Casting`: игнорирует casting Setback от тяжёлой брони, щита и занятой
  руки; наличие оружия само по себе не является условием.
- `Potent Concoctions`: отдельно при Triumph и при Despair бросить/добавить одну
  Proficiency; каждое из двух срабатываний максимум один раз за check.
- `Precise Archery`: downgrade difficulty один раз.
- `Shockwave`: rating Blast равен ranks Melee (Heavy); владелец невосприимчив к
  собственному Blast.
- `Death Rage`: +2 melee damage за каждую current Critical Injury, без
  придуманного cap.
- `Unrelenting`: вторая атака той же цели; +1 Difficulty при втором оружии,
  +2 при том же оружии.
- `Whirlwind`: attack против самой высокой difficulty среди Engaged enemies, затем
  +1 Difficulty; каждая Engaged enemy получает base damage + total Success.
- `Impaling Strike`: после того как melee weapon действительно нанесло Critical
  Injury, Incidental без дополнительной цены immobilizes ту же цель до конца её
  следующего хода **в дополнение** к самой Critical Injury. Без созданной Critical
  Injury, при ranged attack или против цели без применённого critical trigger
  недоступен.
- Не откатывать Dungeoneer, Conduit, Side Step и пять errata-добавлений.

#### Реализация и приёмка

Typed rule registry содержит каждый trigger, cost, rank formula, range, duration и
result allocation. Для каждого пункта нужен parameterized domain test точных
значений и хотя бы один invalid-trigger test; для временных эффектов —
GameTable expiry/use-counter test. API покрывает happy path, zero-mutation failure и
атомарную стоимость. Guided/GM effect создаёт pending decision, а не молча
считается выполненным.

**Миграция:** effect metadata/runtime state по необходимости. **Зависимости:**
ROT-TAL-05, combat/check/magic/social engines.

### ROT-TAL-07. Полный PrivateFull и согласованный RU/EN

- Для всех 112 активных RoT talents нужны разные, содержательные, оригинальные
  RU/EN-парафразы с одинаковыми числами, timing, range, cost, duration и
  limitations; `SafeDescription` остаётся коротким.
- Особо переписать обе локали для Conduit, Signature Spell, Field Commander,
  Encouraging Song, Easy Prey, Back-to-Back, Dungeoneer и
  Can’t We Talk About This?.
- Обязательно проверить английские парафразы всех талантов из ROT-TAL-06; текущий
  английский текст не считать автоматически верным.
- Источник/страница соответствуют реальному источнику таланта. NPC-вариант
  Encouraging Song не должен подменять PC-вариант с `Charm OR Verse`.
- Startup/CI coverage validator для `PrivateFull` падает, если отсутствует хотя бы
  один `rot.talent.<code>`, если full равен safe/placeholder или локали расходятся
  со структурной механикой. `PublicSafe` не отдаёт full ни через один endpoint.
- Seed обновляет built-in по stable code, не меняя ID и custom content.
- Тесты: 112/112 RU и EN, отсутствие placeholders/дубликатов, ключевые числовые
  значения из rule registry, content-mode leakage, reseed, cards/print/language
  switch.

Перед merge провести ручной copyright-review: парафразы должны полностью передавать
механику, но не воспроизводить книжные предложения.

---

## 4. Heroic Abilities: пункты 1, 2 и 5

### Общая база, которую нельзя сломать этими задачами

- Новый RoT-персонаж выбирает одну Heroic Ability, но получает **0 ability points**
  при создании. Starting XP вида не считается earned XP.
- За каждые полные 50 earned XP персонаж получает один ability point.
- Power: Improved стоит 1 AP, затем Supreme ещё 2 AP; порядок обязателен.
- Duration: 1 AP за ранг, repeatable; каждый ранг добавляет один собственный ход
  длительности.
- Frequency: 2 AP за ранг, repeatable; каждый ранг добавляет одно использование за
  сессию.
- Story: 1 AP, один раз; уменьшает стоимость activation с 2 до 1 Story Point.
- Secondary effect: 1 AP за различный effect; максимум два разных standard
  secondary effects.
- Покупка после creation необратима обычным игроком. Бюджет проверяет backend по
  earned XP и фактической сумме всех upgrades.

### ROT-HA-01 (heroic 1). Обязательные личное имя и происхождение

У способности есть три разных понятия: primary effect из каталога, личное название
игрока и origin. Личное название обязательно, trim, 1–120 символов; оно не является
глобальным DB-key и не обязано быть уникальным между персонажами, но не должно
молча подменяться display name primary effect.

Origin выбирается как собственный текст или как один/два результата d10:

| d10 | Структурная категория происхождения |
|---:|---|
| 1 | наследственная сила или особая кровь |
| 2 | избранность судьбой или пророчеством |
| 3 | сила, связанная с артефактом |
| 4 | покровительство невидимой сверхъестественной силы |
| 5 | исключительная внутренняя цель: долг, клятва или месть |
| 6 | единственный преобразивший жизнь опыт |
| 7 | благословение либо проклятие |
| 8 | уникальная многолетняя подготовка |
| 9 | воздействие неконтролируемой магии |
| 0 | бросить ещё два раза; каждый повторный 0 перебросить; сохранить оба результата |

Специальный результат 0 не хранится как финальный origin. Последовательность
`0,0,4,7` даёт origins 4 и 7; одинаковые обычные результаты допустимы только если
книга не требует их перебросить — хранить фактические rolls для аудита.

#### Реализация

- Value object `HeroicIdentity`: `CustomName`, `OriginMode`
  (`Standard|DoubleStandard|Custom`), один/два `HeroicOriginType`,
  `OriginNarrative` (обязателен для Custom, 1–2000), фактические rolls.
- Identity обязателен при завершении нового RoT-PC с primary effect и после
  completion неизменяем обычным игроком.
- Random resolver — чистая domain-функция с инъецируемым d10.
- Setup/API различает `primaryEffectName` и `customName`; duplicate/export/import
  сохраняют identity. UI после primary effect предлагает выбрать, описать или
  бросить, показывает оба результата special roll.
- Legacy completed character без identity остаётся доступен с
  `HeroicIdentityIncomplete`, но не может покупать/редактировать heroic upgrades,
  пока owner/GM один раз не заполнит данные с audit event. Не выдумывать origin.

Критерии: completion невозможен без всех частей; Core не требует поля; 0 всегда
разрешается в два non-zero; reload/duplicate/round-trip; immutability. Тесты
покрывают mapping 1–9, repeated 0, validation, ownership и UI. **Миграция:** nullable
columns/value object + legacy flag; invariant non-null для новых RoT.

### ROT-HA-02 (heroic 2). Параметры Paragon, Sixth Sense и Signature Weapon

Параметр выбирается вместе с primary effect, обязателен до completion и после него
не меняется. Смена primary effect во время creation атомарно удаляет параметры и
созданные экземпляры старого.

#### Paragon

Ровно один доступный персонажу skill (`SkillDefId` + display snapshot). Допустим
built-in или доступный owner custom skill той же системы. Все уровни Paragon
работают только с ним. Скрытый позднее custom skill сохраняет snapshot и даёт
repair warning; нельзя бесшумно выбрать другой.

#### Sixth Sense

Одна согласованная с GM категория воспринимаемых существ/сущностей, trim 1–300:
например, животные, мёртвые, духи или чужие мысли. Это отдельный typed parameter, не
общая заметка персонажа.

#### Signature Weapon

Способность материализует/связывает ровно одно именное оружие. Клиент выбирает
profile enum, craftsmanship (`Dwarven|Elven|Steel`), narrative form и один
совместимый base attachment; сервер сам создаёт следующий профиль:

| Profile | Skill | Damage | Crit | Range | Enc | HP | Качества |
|---|---|---:|---:|---|---:|---:|---|
| Brawl | Brawl | Brawn + 2 | 4 | Engaged | 1 | 2 | Disorient 3, Superior |
| One-handed | Melee (Light) | Brawn + 3 | 3 | Engaged | 1 | 2 | Superior |
| Two-handed | Melee (Heavy) | Brawn + 5 | 3 | Engaged | 3 | 2 | Knockdown, Superior |
| Ranged | Ranged | 8 | 3 | Long | 2 | 2 | Superior |

Форма не меняет цифры, но определяет совместимость attachment. Выбранный base
attachment не устанавливается навсегда: его эффект активен только вместе с Heroic
Ability, стоит 0 и занимает 0 HP. Если тот же attachment уже установлен обычным
способом, transient copy не складывается.

Совместимость не определяется свободным названием. Вместе с narrative form GM
подтверждает неизменяемые `WeaponFormTraits`:
`Brawl|OneHanded|TwoHanded|Ranged`, `Sword`, `BowOrCrossbow`, `Bladed`,
`BluntOrCrushing`, `HasCuttingEdge`, `WoodenWorkingEdge`. Profile задаёт первую
группу автоматически; остальные traits GM отмечает только если они физически
следуют из формы. Attachment resolver использует те же predicates, что
ROT-EQP-ATT-02:

- Balanced Hilt — one-handed close-combat hilt;
- Duelist Cross Guard — Sword;
- Razor/Serrated Edge и Rune of Blades/Severing — Bladed, с дополнительным
  запретом Rune of Blades для wooden working edge;
- Recurve Limbs — BowOrCrossbow;
- Weighted Head — BluntOrCrushing и **без** HasCuttingEdge;
- Explosive Missile — Ranged;
- Runic Flame/Frost — Melee/Brawl;
- Runic Thunder, Superior customization и Ynfernael Corruption — any weapon.

Base choice берётся только из weapon attachments, совместимых с подтверждёнными
traits, и не может быть качеством/attachment, которое weapon уже имеет в
effective profile. Rarity, price, HP, enchantment-install check и magic-skill rank
для этой **временной heroic copy** не применяются; GM всё равно подтверждает
narrative compatibility. Позже физически установленный экземпляр того же code
делает временный эффект redundant, а не даёт выбрать новый base attachment.

Не более одного signature weapon. Если оно потеряно/уничтожено, отдельная
GM/narrative replacement command возвращает или заменяет его не позднее начала
следующей сессии; старый и replacement не могут одновременно остаться активными.

#### Реализация и приёмка

- One-to-one typed `CharacterHeroicConfiguration` и отдельный
  `CharacterSignatureWeapon`/instance override; не создавать глобальный custom
  ItemDef.
- Сервер проверяет system/visibility, exact union, compatibility, single weapon и
  строит numbers из profile enum. Нерелевантные или подменённые client fields
  отклоняются.
- Conditional UI: skill picker, subject input или четыре profile cards +
  craftsmanship/form/filtered attachment; итоговый preview объясняет transient
  attachment.
- Tests: четыре точных профиля, missing/wrong union, foreign ID, compatibility,
  effect switch cleanup, tampered import, lost/replacement, round-trip.
- Legacy affected characters — `HeroicConfigurationIncomplete`, без угадывания.
- **Миграция:** configuration, signature instance и FK. **Зависимости:** item
  qualities/HP/attachments/craftsmanship engine.

### ROT-HA-05 (heroic 5). Power всех 11 primary effects и Duration lifecycle

#### Общий lifecycle

Power rank 0 — Base; rank 1 добавляет Improved; rank 2 добавляет Supreme. Эффекты
кумулятивны, кроме явно указанной замены action cost у Unleash.

Activation по умолчанию — Incidental, 2 Story Points, once/session. Default duration
заканчивается в конце первого собственного хода, который **начался после**
activation. Каждый Duration rank добавляет ещё один собственный ход. Текущий ход,
внутри которого была activation, счётчик не уменьшает. Даже мгновенный primary
effect оставляет active state для Duration и secondary effects.

Хранить session-scoped `HeroicActivationState`: participant, primary code, snapshot
Power/Duration, activation round/turn, `RemainingOwnerTurns = 1 + DurationRanks`,
active flag, counters и typed payload. `StartOwnerTurn` запускает periodic hooks,
`EndOwnerTurn` уменьшает счётчик и при нуле снимает только transient modifiers.
`EndSession` очищает state, uses и temporary Story Points.

Повторная допустимая activation во время активности заново оплачивается и
расходует use, повторяет activation-only effects и обновляет duration. Одинаковые
continuous modifiers не складываются. Все targets/choices проверяются до Story
spend; Story pool защищён concurrency token. Active instance использует snapshot:
купленный посреди него upgrade не действует ретроактивно.

#### Точная Power-матрица

1. **All the Facts**
   - Base: в каждый свой активный ход получить от GM один важный факт о наблюдаемом
     или прямо связанном с ситуацией человеке, существе, месте, предмете либо
     обстоятельстве.
   - Improved: один раз upgrade ability для каждой check, непосредственно
     использующей полученную информацию.
   - Supreme: каждый факт создаёт один temporary player Story Point до конца
     session. При трате такой point исчезает и не переходит GM; хранить связь
     `fact → point`.
2. **Connected**
   - Base: выбрать NPC и установить правдоподобный ранее существовавший долг/рычаг.
     Favor не обязан подвергать NPC смертельному риску, убийству или явному
     разрушению положения/богатства. Если GM признаёт связь невозможной,
     activation отменяется без Story/use.
   - Improved: пока active, downgrade difficulty каждой social check персонажа
     один раз.
   - Supreme: когда разумный adversary объявляет владельца целью атаки, владелец
     out-of-turn incidental заставляет его выбрать другую допустимую цель.
3. **Foretelling**
   - Base: once/round активности задать GM один yes/no вопрос о знании, недоступном
     обычной логикой; ответ правдив.
   - Improved: один раз reroll связанную с вопросом skill check; одну check нельзя
     перебрасывать этим эффектом повторно.
   - Supreme: once/activation при NPC skill check игрок бросает идентичный pool и
     после обоих бросков решает, заменить ли NPC result своим.
4. **Hard to Kill**
   - Base: +4 soak.
   - Improved: increase difficulty всех combat checks, targeting owner, на 1.
   - Supreme: весь damage, который owner должен получить, становится 0.
5. **Influential**
   - Base: в complex social encounter дополнительный social strain равен
     characteristic, связанной с использованным social skill. Если сцена решается
     одной check, вместо strain добавить столько automatic Success.
   - Improved: critical remark стоит 2 Advantage и наносит 5 strain; можно
     несколько раз на одной check при достаточных symbols.
   - Supreme: уменьшить получаемый в social encounter strain на
     `max(Presence, ranks Cool)`, минимум 0. Не уменьшать voluntarily suffered
     strain.
6. **Miraculous Recovery** — полное правило ROT-HA-08.
7. **Paragon**
   - Только выбранный ROT-HA-02 skill.
   - Base: после roll, до resolution, убрать одну выбранную Difficulty die и
     игнорировать её symbols.
   - Improved: дополнительно убрать одну Setback die.
   - Supreme: вместо Difficulty можно убрать одну Challenge die. Если нужной die
     нет, соответствующая часть ничего не делает.
8. **Sixth Sense**
   - Только выбранный subject.
   - Base: ограниченный обмен эмоциями/впечатлениями и один важный факт для
     текущего encounter.
   - Improved: обмен простыми идеями и ещё один факт для текущей session.
   - Supreme: сложный разговор и ещё один факт масштаба adventure/campaign.
     При Power 2 создаются все три information prompts.
9. **Signature Weapon**
   - Base: transient attachment из ROT-HA-02 на время активности, цена/HP 0.
   - Improved: навсегда выбрать ровно одно — `Reinforced` **или**
     `Ancient craftsmanship` со всеми его эффектами.
   - Supreme: навсегда +2 HP и одно бесплатное совместимое attachment
     Rarity ≤ 9, которое помещается в новый total HP.
   - Improved/Supreme choices фиксируются при покупке и неизменяемы.
10. **Unbowed**
    - Base: при activation выбрать одну current Critical Injury, кроме `Dead`; до
      expiry игнорировать её effects и её +10 к последующим critical rolls.
      Activation можно сделать out-of-turn сразу после получения Critical.
    - Improved: игнорировать все current Critical Injuries, кроме Dead.
    - Supreme: игнорировать также Dead, но при expiry немедленно умереть, если Dead
      к этому моменту не вылечена.
11. **Unleash** — полное правило ROT-HA-10.

#### Точная матрица восьми secondary effects

Каждый effect стоит 1 AP, unranked; купить можно максимум два **разных** code.
Выбранные effects входят в immutable snapshot каждой activation:

| Code | Полная механика |
|---|---|
| `devastating` | Пока ability active, к одному выбранному hit **каждой** attack владельца добавить +2 damage. При miss бонус не сохраняется; у Linked/Auto-fire/multi-target только один hit этой attack. |
| `diminish` | Пока active, каждый enemy, который в момент построения pool находится в Short от владельца, добавляет один Setback ко всем своим skill checks. Выход из Short немедленно снимает модификатор, вход добавляет. |
| `drain` | Сразу при activation и в начале каждого собственного хода владельца, пока active, каждый находящийся тогда в Short enemy получает 2 strain; routing Rival/Minion выполняет CMB-05/MIN-03. Это отдельные periodic events, soak не применяется. |
| `empowered` | Пока active, владелец добавляет один Boost ко всем своим skill checks. |
| `empower-allies` | Пока active, каждый ally, находящийся в Short от владельца при построении pool, добавляет один Boost к своим skill checks. Сам владелец не считается своим ally для этого effect. |
| `rejuvenation` | Сразу при activation и в начале каждого собственного хода, пока active, владелец heal 2 strain, не ниже 0. Rival/Minion-владелец без ST не превращает это в лечение wounds. |
| `rejuvenate-allies` | При activation и в начале каждого собственного хода владельца все allies, находящиеся тогда в Short, heal 2 strain. Ally без ST ничего не лечит; это не wound healing. |
| `renewal` | При activation в structured encounter владелец может бросить Cool **или** Vigilance и создать один новый PC Initiative slot с этим результатом. Slot остаётся до конца encounter. Он доступен сразу, но не позволяет персонажу, уже сделавшему turn в текущем round, сделать второй; server хранит acted participants, а не только число slots. Каждая последующая законная activation может создать ещё один slot по тем же правилам. |

Для `drain/rejuvenation/rejuvenate-allies` activation tick выполняется ровно один
раз и не дублируется первым `StartOwnerTurn`. Range и allegiance определяются
authoritative Game Table на каждый tick/check, а не snapshot списка целей.
`renewal` вне structured encounter создаёт pending choice без slot; подтвердить его
можно только до окончания той же activation в начавшемся encounter, иначе choice
истекает без эффекта. Secondary, купленный во время уже active ability, начинает
работать только со следующей activation.

#### Реализация и приёмка

- Один activation service обслуживает sheet и Game Table; standalone endpoint без
  session/story/turn не меняет wounds/soak и не обходит лимиты.
- DTO возвращает expiry, remaining turns, uses/cost, applied effects и pending
  decisions. UI показывает badge Active, counter и только допустимые context
  actions.
- Narrative primary создаёт typed GM prompt с сохранённым ответом, не no-op text.
- Table-driven xUnit: Base/Improved/Supreme каждого из 11, exact modifier/range/
  timing/frequency, invalid target/die/choice, cleanup. Lifecycle: activation в
  свой/чужой ход, Duration 0/1/3, reactivation, defeat, session end. API:
  concurrency/cost/idempotency. UI: хотя бы один интерактивный path на каждый
  primary. Отдельные table tests покрывают все восемь secondary effects, два
  разных/duplicate/третий effect, movement range, periodic ticks, multi-hit
  Devastating и отсутствие дополнительного turn от Renewal.
- **Миграция:** activation state/event payload. Ephemeral active state backfill не
  нужен. Зависимости перечислены в матрице.

> **Вне скопа: нужен рантайм столкновения.** Разбор сохранён в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md):
> - **ROT-HA-08** — Miraculous Recovery: лечение в начале каждого своего хода;
> - **ROT-HA-10** — Unleash: раз в раунд снять minion group.

### ROT-HA-CONTENT. PrivateFull для Heroic Abilities

Для всех 11 primary, их 22 Power upgrades, пяти universal upgrade categories,
secondary effects, origin table и parameter help должны существовать полные
оригинальные RU/EN-парафразы. Они обязаны совпадать со структурной матрицей выше и
не сводиться к названию. `PublicSafe` получает только safe summary. Coverage и
leakage тестируются так же, как в ROT-TAL-07.

---

## 5. Бой, здоровье и Defense: пункты 1–4

Эти Core-правила RoT не переопределяет; они обязательны для обеих систем.

### ROT-CMB-01. Обычный урон только при успешной атаке

1. После сокращения Success/Failure атака попадает только при
   `netSuccesses > 0`; ноль — промах.
2. Только при hit каждый оставшийся Success даёт +1 к base damage:
   `rawDamage = baseDamage + netSuccesses`.
3. Triumph содержит Success, который участвует в сокращении. Оставшийся Triumph при
   `netSuccesses <= 0` не превращает промах в hit.
4. Для каждого отдельного hit:
   `applied = max(0, rawDamage - targetSoak)`. Auto-fire/Linked и иные
   дополнительные hits проходят soak по отдельности, не после суммирования.
5. Обычная Critical Injury доступна, только если атака hit и после soak прошла хотя
   бы 1 wound/strain damage.
6. Активное качество обычно требует hit. Исключение существует только у качества,
   структурное правило которого прямо разрешает активацию на miss, например
   отдельные режимы Blast, Sunder или Guided. Advantage/Triumph промаха можно
   тратить на разрешённые narrative/quality effects, но base damage не появляется.

#### Реализация

- Backend `CombatResolver` — источник истины вместо frontend-only helper.
- Input различает net symbols, base profile, soak, дополнительные hits и выбранные
  symbol spends. Output:
  `IsHit`, `RawDamagePerHit?`, `DamageAfterSoakPerHit[]`, `TotalApplied`,
  `AllowedSymbolSpends`, `RejectedSymbolSpends`, log.
- На miss damage fields пусты/null, а не `baseDamage`.
- Game Table apply ссылается на roll ID и idempotency key; повторная доставка не
  наносит damage второй раз. Клиентский summary не является authoritative.
- UI пишет «Промах: обычный урон не применяется» и фильтрует spends по
  `MayActivateOnMiss`. При hit показывает base/successes/soak для каждого hit.

#### Критерии

- base 7, successes 2, soak 4 → hit, raw 9, applied 5;
- successes 0 или −2 → miss, applied 0;
- Triumph + Failure без иных Success → miss;
- hit, полностью поглощённый soak, не даёт обычный critical;
- multi-hit применяет soak к каждому;
- invalid spend и repeated apply дают zero mutations.

### ROT-CMB-02. Преимущества только одной активной брони

Персонаж может физически носить несколько Armor, но получает soak, defense,
уникальные свойства и attachment effects ровно одной выбранной брони. Щит,
талисман, cover, talent и иной non-Armor source обрабатываются отдельно.

- `Character.ActiveArmorCharacterItemId` — nullable FK на принадлежащий, надетый
  Armor instance с quantity ≥ 1.
- Derived calculator использует только active armor для защиты. Все реально
  надетые брони продолжают давать свой фактический worn load.
- Первая надетая броня при null может стать active. Надевание второй не переключает
  выбор молча; отдельная атомарная команда устанавливает ровно одну.
- Снятие/продажа/удаление/разрушение active armor очищает FK; следующая не
  выбирается автоматически.
- API возвращает active ID и `isActiveArmor`; UI использует radio/action и прямо
  сообщает, что неактивная надетая броня даёт load, но не защиту.
- Нельзя выбрать чужой, отсутствующий, не-Armor или не надетый instance.

Legacy migration выбирает среди надетых Armor максимальный soak, затем максимальную
применимую defense, затем стабильный ID; решение записывается в migration audit.
Предметы/states не менять.

Тесты: +1 и +2 не дают +3, переключение меняет stats без изменения load,
неактивные unique/attachments не работают, shield/talisman остаются, invalid ID
атомарно отклонён. **Миграция:** active armor FK + backfill.

### ROT-CMB-03. Defense: provider, increase, области и cap 4

Defense добавляет к направленной на цель combat check столько Setback dice, каков
effective rating. Это не Difficulty и не upgrade.

Области:

- `General` применима к melee и ranged;
- `Melee` — только к ближней атаке;
- `Ranged` — только к дальней.

Каждая атака имеет typed classification; не определять её по локализованному skill
name. Обычная ranged magic attack использует Ranged Defense; `Close Combat` —
Melee Defense.

Источники `Provides/Sets` не складываются: для канала берётся максимальный
применимый provider. К ним относятся броня, cover, Guarded Stance и формулировка
«получает/имеет Defense N». Источники `Increases` со знаком «+» складываются друг с
другом и с лучшим provider: Defensive, Deflection, явно additive
talent/ability/item.

```text
meleeProvider  = max(all applicable General/Melee Provides, 0)
rangedProvider = max(all applicable General/Ranged Provides, 0)
meleeRaw       = meleeProvider  + sum(General/Melee Increases)
rangedRaw      = rangedProvider + sum(General/Ranged Increases)
effective      = min(4, raw)
```

`DefenseContribution` хранит source ID/type, scope, mode, value и expiry.
Агрегатор возвращает effective и breakdown: победивший/проигнорированные providers,
каждый increase и cap. Временные cover/stance/ability contributions не меняют base
snapshot.

Dice builder автоматически добавляет effective Setback. UI показывает breakdown и
raw value сверх cap; contributions сверх cap сохраняются, чтобы после expiry
другого источника пересчёт был корректен.

Критические тесты:

- armor General provider 1 + cover Ranged provider 2 → melee 1, ranged 2;
- provider 2 + increases 1 + 1 → 4;
- provider 3 + increases 2 → raw 5/effective 4;
- General increase действует в обоих каналах;
- Defense 4 → четыре Setback;
- удаление лучшего provider открывает следующий.

### ROT-CMB-04. Cap Defense 4 для NPC и всех остальных

Значение 4 — универсальный максимум. Текущая политика «warning >4, error >6»
удаляется.

- `NpcValidator`: base melee/ranged defense только 0…4; 5 — блокирующая RuleError.
- То же для built-in seed, custom CRUD, import/content pack, encounter/manual
  participant и override.
- Runtime aggregator ROT-CMB-03 всегда ограничивает effective значением 4.
- UI fields `min=0,max=4`; убрать текст об «исключительном NPC до 6».
- Перед DB constraint сохранённые base/snapshot values >4 привести к 4 и
  зафиксировать audit; built-in seed fail-fast, а не silently clamp.

Тесты: 4 сохраняется без warning; 5/6 → 400 и zero mutations; все import/manual
пути; derived raw 6 → effective 4; migration сохраняет остальные поля.

> **Вне скопа: нужен рантайм столкновения.** Разбор сохранён в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md):
> - **ROT-CMB-05** — усталость Rival превращается в раны — маршрутизация автосписания;
> - **ROT-CMB-06** — MinionGroup только для Minion: валидатор состава группы.

---

## 6. Minion Groups

> **Вне скопа.** Механика групп целиком вынесена в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md): она существует только ради
> применения урона к участникам столкновения.

---

## 7. Социальные столкновения

> **Вне скопа.** Раздел целиком вынесен в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md): социальная сцена держится на
> списании усталости и тратах символов конкретной проверки.

---

## 8. Снаряжение, экономика, encumbrance, оружие, броня и attachments

### ROT-EQP-01. Нагрузка, перегруз и сокрытие

> **Финальное решение по объёму (2026-07-30): выполнено.** Текущая реализация перегруза,
> агрегации предметов с нулевым Encumbrance и лимитов используемого снаряжения принята владельцем
> как достаточная. Typed holder/placement для Container/Mount/Wagon, отдельная
> `zeroEncStorageMode`, автоматический concealment builder и связанная миграция не считаются
> незавершённой работой `ROT-EQP-01`. Требования ниже сохраняются как архив аудита. Отдельный
> раздел транспорта остаётся в `ROT-TRANSPORT-01`.

```text
threshold = 5 + current Brawn + applicable threshold bonuses
overload  = max(0, load - threshold)
```

- При `overload > 0` добавить ровно `overload` Setback ко всем checks, связанным с
  Brawn или Agility. Они складываются с другими Setback и имеют source tag
  `encumbrance`.
- Если `overload >= current Brawn`, персонаж не получает бесплатный Maneuver в свой
  ход. Максимум два сохраняется, но **каждый** сделанный Maneuver, включая первый,
  стоит 2 strain.
- Brawn 2, threshold 7: load 8 → один Setback и бесплатный Maneuver; load 9 → два
  Setback и 2 strain за каждый Maneuver.
- Изменение Brawn/load/container немедленно пересчитывает состояние.

#### Enc 0

Нулевой Enc не умножается на quantity:

```text
zeroEncLoad =
    floor(total loose Enc0 items / 10)
  + floor(total efficiently stored Enc0 items / 20)
```

Агрегировать между разными stacks одного holder/container, чтобы десять строк по
одному предмету не обошли правило. Остаток 1–9 loose или 1–19 stored даёт 0.
`zeroEncStorageMode=Loose|Efficient` — состояние stack/placement; GM может добавить
отдельный manual load adjustment для неудобного груза.

#### Сокрытие

- Enc ≤ 1 можно спрятать на персонаже без предварительной check.
- Активный обыск: opposed `searcher Perception` vs `holder Stealth`.
- Пассивное наблюдение: opposed `searcher Vigilance` vs `holder Stealth`.
- Ищущий получает один Boost за каждый пункт Enc **самого крупного** скрытого
  предмета сверх 1; несколько предметов не суммируются.
- При тайнике в окружении GM задаёт concealable Enc без бонуса; искать предмет
  крупнее — один Boost за каждый пункт превышения.

#### Реализация и тесты

Domain result: load/threshold/overload/Setback/free Maneuver/strain cost и breakdown.
Typed holder/placement: Character, Container, Mount, Wagon; один stack не считается
у двух holders. Game Table списывает maneuver/strain атомарно. UI показывает точный
штраф, а concealment builder сам вычисляет opposed pool/Boost.

Legacy Enc0 без доказуемого контейнера → Loose + migration warning. Проверить
9/10/19/20/21, split stacks, mixed modes, equality overload=Brawn, две maneuvers,
только Brawn/Agility checks, active/passive search, largest item, holder transfer.
**Миграция:** placement/storage mode.

### ROT-ECO-01. Доступность, покупка, продажа и услуги

> **Финальное решение по объёму (2026-07-30): выполнено.** Серверный расчёт цены покупки, три режима
> продажи, таблица редкости и модификаторы рынка сделаны. Признак `Price = —` выражается nullable
> ценой (`ItemDef.Price = null`) и принят владельцем как достаточный: отдельный булев `IsPriceless`
> в объём не входит. Услуги закрыты в `GEN-SHOP-01`. Требования ниже сохраняются как архив аудита и
> не считаются незавершённой работой `ROT-ECO-01`.

#### Поиск товара

| Effective rarity | Difficulty |
|---:|---|
| 0 | Simple (0) |
| 1–2 | Easy (1) |
| 3–4 | Average (2) |
| 5–6 | Hard (3) |
| 7–8 | Daunting (4) |
| 9–10 | Formidable (5) |

Модификаторы rarity:

- consumer economy −1;
- major metropolis −1;
- trading hub −1;
- средний город/обычная цивилизованная территория 0;
- rural/agrarian +1;
- regulated economy +1, если регулирование относится к товару;
- frontier +2;
- prohibited ownership +2;
- active war zone +3;
- post-disaster wasteland +4.

Итог не ниже 0. При rarity >10 difficulty остаётся Formidable, а GM может upgrade
check по одному разу за каждый пункт сверх 10. GM может объявить товар отсутствующим
без check. Legal search использует Negotiation, illegal/black market — Streetwise;
иной Knowledge — только явное contextual GM решение.

#### Деньги и продажа

- Найденный товар покупается по listed price. Сервер получает ItemDef+quantity,
  сам считает total и атомарно списывает баланс; произвольный client `Cost`
  запрещён.
- GM price override — отдельная авторизованная команда с reason/audit.
- Продажа требует успешной Negotiation check по rarity; illegal sale —
  Streetwise. Failure ничего не продаёт.
- Выручка: 1 net Success → 25% base cost; 2 → 50%; 3+ → 75%.
  Книга не задаёт дробные монеты, поэтому приложение принимает явно помеченное
  `ProductDecision`: для одинакового stack сначала
  `bookSubtotal = quantity × floor(unitListedPrice × fraction)`. Не округлять
  каждый процент вверх и не считать процент от подменённой клиентом цены.
- Damage/condition discount не является автоматическим Core/RoT процентом. Только
  GM может задать видимый `conditionMultiplier` с reason; тогда
  `finalProceeds = max(0, floor(bookSubtotal × conditionMultiplier))`. В audit
  отдельно сохраняются listed price, fraction, rounding, quantity и override.
- Для опциональной bulk trade между рынками сначала изменить base cost по росту
  rarity: +0/+1 ×1; +2 ×2; +3 ×3; +4 или больше ×4, затем доля продажи. Это
  guidance для торговли грузом, не скидка на обратную покупку единичного предмета.

`Price = —` хранится `price=null,isPriceless=true`, не 0; обычные buy/sell
недоступны, GM выдаёт/изымает с audit. Lodging/meal/travel/hire создают expense/
service record с периодом, не вечный inventory item. Direct money edit — только
manual ledger adjustment с reason.

Тесты: вся rarity/modifier table, >10 upgrades, legal/illegal skills, failure и
1/2/3 Success, rounding/quantity, funds/concurrency/idempotency, priceless/service,
GM auth и content visibility.

### ROT-EQP-02. Специальное снаряжение и транспорт

> **Финальное решение по объёму (2026-07-29): выполнено.** Текущие состояния специального
> снаряжения и их взаимодействие с Encumbrance приняты владельцем как достаточные. Исходные
> расширенные требования ниже сохраняются как архив аудита и не считаются незавершённой работой
> `ROT-EQP-02`. Скакуны, повозки, груз и транспортное снаряжение перенесены в отдельную будущую
> задачу `ROT-TRANSPORT-01`.

- `Barding`: Enc 5, price 900, rarity 4; armor mount: provider Defense 1, soak +2.
  Обычно совместим с War Mount; другой mount — GM override. Не даёт защиту rider;
  у mount тоже только одна active armor.
- `Saddlebags`: price 75, rarity 3; +4 encumbrance threshold конкретного mount.
  Содержимое — load mount, не PC.
- `Wagon`: price 200, rarity 2; container capacity 50 Enc для груза/пассажиров.
  Живое существо обычно занимает `5 + Brawn`. Для умеренного движения требуется
  связанный Beast of Burden. Без тяги wagon остаётся контейнером. Overcapacity
  блокирует движение либо получает явный GM override, но не переносится на PC.
- `Waterskin`: empty Enc 1, price 5, rarity 1; full Enc 2 и дневной запас воды для
  двух существ. Fill/empty меняет state одного instance; пока не пуст, Enc 2.
- `Winter Clothing`: base Enc 4, worn Enc 1; при ношении убрать до двух Setback с
  Survival/Resilience, только если source `cold weather`.
- `Fine Cloak`: base Enc 1, worn Enc 0; при ношении убрать один Setback с Charm,
  Deception или Leadership.
- `Elven Boots`: base Enc 1, worn 0; число movement maneuvers для смены bands −1,
  минимум один.
- `Winged Boots`: base Enc 1, worn 0; flight/hover без Maneuver для удержания
  высоты, максимум Medium над землёй.
- `Gauntlets of Power`: base Enc 1, worn 0; ко всем Brawn-based checks один
  automatic Success и один automatic Advantage.
- `Cloak of Mists`: base Enc 1, worn 0; полный активный эффект — в magic items.
- `Warding Talisman`: Enc 0, worn; увеличивает General Defense на 1 как additive
  contribution, затем cap 4. Это не provider 1.
- `Adventuring Pack` исключить из official RoT active catalog. Career `traveling
  gear` разворачивать в backpack, bedroll, rope, flint and steel, 3 torches,
  waterskin. Legacy instance → Retired; GM-assisted decomposition без изменения
  денег, не silent delete.
- Beast of Burden/Riding/Flying/War Mount при приобретении создают mount/adversary
  instance со statblock и owner, а не безликий gear. Flying Mount — без Dodge 2
  согласно errata.

Нужны typed states `worn/mounted/container/full`, target owner и effect definitions.
Продажа контейнера с содержимым блокируется до переноса либо выполняется audited
bundle command. Проверить unlink, quantity, export/import/duplicate, лист, стол и
обе content modes.

### ROT-TRANSPORT-01. Добавить отдельный раздел «Транспорт»

Транспорт не является состоянием обычного `CharacterItem`. На листе персонажа нужен отдельный
раздел «Транспорт», объединяющий принадлежащих персонажу скакунов и транспортные средства.

Минимальный принятый объём будущей задачи:

- отдельные owned instances для `Mount` и `Vehicle` с owner, source и стабильным definition code;
- покупка или выдача Beast of Burden, Riding Beast, Flying Mount и War Mount создаёт mount
  instance, а не безликую строку gear;
- Wagon создаёт vehicle/container instance, а не предмет с фиктивным Enc;
- карточка показывает название, тип, характеристики, soak, wound/system threshold, defense,
  silhouette, скорость/режим движения, текущие повреждения и заметки;
- отдельный список груза с текущей загрузкой и вместимостью; груз транспорта не прибавляется к
  Encumbrance персонажа;
- Barding и Saddlebags устанавливаются на конкретного скакуна; содержимое saddlebags принадлежит
  его грузу, а защитные значения barding не применяются к rider;
- wagon связывается с Beast of Burden для движения; отсутствие тяги не удаляет wagon и не
  переносит его груз персонажу;
- перемещение груза между персонажем и транспортом — явная атомарная команда с проверкой
  ownership/capacity и audit-записью;
- продажа, удаление, export/import и duplicate не оставляют потерянные ссылки или груз без owner;
- transport section доступен с листа персонажа; Game Table может читать выбранный instance,
  но полноценный runtime погонь и транспортного боя в эту задачу не входит.

Зависимости: `ROT-MOUNT-ITEM-01`; справочные vehicle/chase rules уже существуют отдельно.
Миграция и точный API-контракт проектируются при начале реализации задачи.

### ROT-EQP-GEAR-01. Полный обычный gear catalog и исполняемые эффекты

> **Финальное решение по объёму (2026-07-30): выполнено.** Все 20 записей уже присутствуют
> в активном RoT-каталоге, а текущие Enc/price/rarity и описательные эффекты приняты владельцем
> как достаточные. Исходные расширенные требования ниже сохраняются как архив аудита.
> Структурные `RemoveSetback`, дополнительные состояния расходников и автоматизация эффектов
> не считаются незавершённой работой `ROT-EQP-GEAR-01`. Wagon относится к будущему
> `ROT-TRANSPORT-01`.

Точная таблица `base Enc / price / rarity`:

| Item | Enc | Price | Rarity |
|---|---:|---:|---:|
| Alchemist’s Kit | 3 | 300 | 5 |
| Alchemist’s Lab (Supplies) | 8 | 600 | 6 |
| Apothecary’s Kit | 2 | 150 | 4 |
| Backpack | 0; threshold +4 | 50 | 3 |
| Bedroll | 1 | 15 | 1 |
| Climbing Gear | 1 | 20 | 2 |
| Extra Quiver | 2 | 25 | 2 |
| Fine Cloak | 1 | 90 | 4 |
| Flask (Empty) | 0 | 1 | 1 |
| Flint and Steel | 0 | 10 | 2 |
| Herbs of Healing | 0 | 50 | 6 |
| Lantern | 1 | 50 | 1 |
| Pole (30 hands long) | 2 | 10 | 1 |
| Rope | 1 | 5 | 1 |
| Thieves’ Tools | 1 | 75 | 5 |
| Torches (3) | 1 за pack | 1 | 0 |
| Trail Rations (1 day) | 0 | 2 | 0 |
| Wagon | null | 200 | 2 |
| Waterskin (Empty) | 1 | 5 | 1 |
| Winter Clothing | 4 | 100 | 3 |

Исполняемые правила:

1. Alchemist’s Kit — подходящий инструмент для Alchemy, без собственного Boost.
   GM может потребовать lab для особо сложной формулы.
2. Lab включает функцию kit и даёт один Boost к Alchemy; kit+lab не складываются.
   Lab требует помещения; перевозимая lab занимает всю wagon и одно тягловое
   животное, не оставляя capacity другим грузам/пассажирам.
3. Apothecary’s Kit разрешает Medicine для wounds/Critical Injuries без штрафа
   отсутствующих инструментов; собственного Boost нет.
4. Backpack даёт +4 threshold владельцу только в Worn state; несколько quantity
   одной записи не умножают бонус.
5. Climbing Gear при использовании снимает один Setback с source `climbing` на
   Athletics; это не общий Boost.
6. Extra Quiver: после narrative OutOfAmmo обычного ranged weapon потратить
   Maneuver и вернуть Ready. Не работает с Limited Ammo; автоматически не
   уничтожается, optional depleted state ставит GM.
7. Fine Cloak — как ROT-EQP-02; отсутствие Setback не превращает эффект в Boost.
8. Herbs of Healing: израсходовать одну единицу вместе с Medicine result и добавить
   один automatic Success и automatic Advantage.
9. Lit Lantern освещает Short и снимает один darkness Setback. Обязательного
   счётчика топлива книга не задаёт.
10. Rope стандартной длины достигает примерно Medium. Bedroll, Pole, Flask, Flint
    and Steel и rations дают narrative affordance без выдуманного постоянного die.
11. Thieves’ Tools: один automatic Advantage на Skulduggery для механического
    замка/защёлки; позволяют попытку сложного механического замка. Не дают Boost и
    не открывают автоматически magic/electronic barrier.
12. Pack Torches содержит три units, общий Enc 1. Lit torch горит около часа,
    освещает Short и снимает один darkness Setback; при завершении часа
    израсходовать одну. До нуля pack Enc остаётся 1.
13. Trail Ration — один person-day/unit и участвует в общей Enc0 aggregation.
14. Waterskin/Winter Clothing/Wagon — точные states из ROT-EQP-02. Special worn Enc
    одежды не получает ещё раз общий armor −3.
15. Armor −3 Enc применяется только `kind=Armor`, не любому equipped gear.

Модель различает Base/Worn Enc, capacity, threshold target, state и consumable
units. Effect types как минимум `RemoveSetback(source,count)` и
`AddAutomaticSymbol`; клиент не присылает готовый load/modifier.

Legacy неизменённый Adventuring Pack детерминированно разложить в backpack,
bedroll, rope, flint and steel, pack of 3 torches и empty waterskin с audit. Если
старый pack имел custom state/состав и безопасно распознать его нельзя — оставить
Retired read-only и потребовать GM conversion.

### ROT-EQP-SVC-01. Услуги не являются инвентарём

> **Решение по объёму (2026-07-30): перенесено.** Этот пункт больше не выполняется как
> самостоятельная задача. Вся таблица и правила услуг ниже входят в `GEN-SHOP-01`, потому что
> услуги должны быть перенесены из инвентаря только одновременно с появлением доступного игроку
> общего магазина. Закрытие `ROT-EQP-SVC-01` означает перенос объёма, а не готовность механики.

| Service | Price | Rarity |
|---|---:|---:|
| Ale, flagon | 1 | 0 |
| Lodging, common room, 1 night | 1 | 0 |
| Lodging, private room, 1 night | 5 | 1 |
| Meal, tavern | 2 | 0 |
| Porter, per day | 1 | 1 |
| Torchbearer, per day | 1 | 1 |
| Riverboat travel, 1 day | 5 | 2 |
| Wagon travel, 1 day | 2 | 1 |
| Wine, bottle | 2 | 1 |

`ServiceDef/ServicePurchase`: Enc null, списание денег и ledger/term, но не
CharacterItem/Equip/load. Porter/Torchbearer не создают combat NPC без отдельного
statblock. Lodging обычно включает стойло обычному riding beast; необычное животное
— GM surcharge/override. Повторная покупка — новая транзакция либо явное продление.

### GEN-SHOP-01. Общий магазин в левом меню и перенос услуг из инвентаря

> **Уточнение объёма (2026-07-30):** встроенные витрины предметов в `Инвентаре` и улучшений
> сохраняются наряду с общим магазином. Из встроенной витрины инвентаря исключаются только услуги.
> Требование ниже об устранении двух активных витрин для физических товаров считается отменённым
> прямым решением владельца продукта. Для услуг инвариант остаётся строгим: они доступны в общем
> магазине и никогда не создают `CharacterItem`.

#### Пользовательский маршрут

- В основном левом меню появляется самостоятельный пункт `Магазин`, доступный из любой
  авторизованной страницы. Он не должен быть вложен в карточку или инвентарь персонажа.
- Пункт открывает отдельный устойчивый маршрут общего магазина. Конкретный URL фиксируется при
  реализации в соответствии с действующим router, но deep link и перезагрузка страницы обязаны
  открывать тот же экран.
- Просматривать каталог можно без выбранного персонажа. Для покупки пользователь явно выбирает
  принадлежащего ему персонажа; если персонаж не выбран, кнопка покупки не выполняет запрос и
  показывает понятное требование выбора.
- Выбранный персонаж определяет игровую систему, доступный контент, баланс денег и результат
  покупки. Нельзя купить RoT-only запись персонажем Genesys Core или изменить инвентарь чужого
  персонажа.
- Магазин объединяет покупаемый справочный каталог в одной поверхности и предоставляет фильтры
  по типу. Как минимум отдельно различаются физические предметы и услуги; существующие категории
  оружия, брони, обычного снаряжения, магических инструментов и рун сохраняют свои фильтры.
- Пункт меню, заголовки, пустые состояния, ошибки и фильтры имеют RU/EN локализацию и остаются
  доступны в мобильной версии левого меню.

#### Физические предметы

- Покупка физической записи продолжает использовать серверную цену и атомарную проверку денег.
  Успешная покупка создаёт или увеличивает принадлежащий выбранному персонажу `CharacterItem`
  согласно существующим правилам stack/instance.
- Раздел `Инвентарь` после переноса остаётся списком уже принадлежащих персонажу физических
  предметов и местом управления ими. Удаление услуг не должно удалять оружие, броню, gear,
  implements, runes или историю денежных операций.
- Одна и та же встроенная запись не должна одновременно показываться как активный товар в двух
  независимых витринах после завершения переноса. Старые ссылки на каталог инвентаря должны либо
  безопасно вести в общий магазин, либо показывать явное перенаправление без дублирования покупки.

#### Услуги

- Общий магазин содержит все девять записей и точные `price/rarity` из таблицы
  `ROT-EQP-SVC-01`. У услуги нет Encumbrance, quantity предмета, equip/worn state, hard points,
  condition или действий ремонта/продажи как у имущества.
- Покупка услуги атомарно списывает с выбранного персонажа рассчитанную сервером сумму и создаёт
  отдельную запись покупки/расхода услуги с definition code, ценой на момент покупки, датой,
  покупателем и, где применимо, сроком (`1 night`, `1 day`). Она не создаёт `CharacterItem`.
- Повторная покупка создаёт новую транзакцию либо явное продление срока. Она не объединяется в
  бессрочный inventory stack.
- Porter и Torchbearer не создают combat NPC автоматически. Lodging не создаёт предмет и не
  изменяет Encumbrance; дополнительные игровые последствия остаются такими, как описано в
  `ROT-EQP-SVC-01`.
- История услуги остаётся доступна после завершения срока как денежная операция; истёкшая услуга
  не считается активной. Отмена, возврат и ручная коррекция возможны только через отдельный
  авторизованный и аудируемый денежный сценарий, если он предусмотрен реализацией.

#### Порядок выпуска без недоступного состояния

1. Добавить маршрут, пункт левого меню, общий каталог, выбор персонажа и покупку физических
   предметов.
2. Добавить определения и покупку услуг с отдельной записью расхода, проверить все девять строк.
3. Только после того как услуги реально доступны и покупаются в общем магазине, удалить их из
   каталога/вкладки раздела `Инвентарь`.
4. Не удалять существующие исторические данные. Если legacy-услуги уже хранятся как
   `CharacterItem`, нужна идемпотентная миграция в записи услуг либо явное read-only legacy
   состояние; silent delete запрещён.

#### API, безопасность и критерии приёмки

- Backend остаётся source of truth для definition, цены, rarity, количества и итоговой суммы.
  Клиент не может передать доверенную цену или owner id.
- Все операции проверяют authentication, ownership выбранного персонажа, content visibility,
  достаточность денег и idempotency; списание и создание результата происходят в одной
  транзакции.
- Ошибка покупки не списывает деньги и не создаёт частичную запись. Двойная отправка одного
  idempotency key не создаёт двойную покупку.
- API и UI различают `physical item` и `service`; сервисная запись никогда не попадает в расчёт
  Encumbrance, экипировки, Defense, Soak или доступных предметных действий.
- Проверить frontend router/sidebar/deep link/mobile menu, фильтры и выбор персонажа; backend
  ownership/funds/content-mode/idempotency; покупку каждой из девяти услуг; отсутствие услуг в
  инвентаре после переноса; отсутствие регрессий покупки и отображения физических предметов.
- `PrivateFull` и `PublicSafe` имеют одинаковую структурную доступность разрешённых записей, но
  показывают только допустимые для режима тексты. Новые формулировки должны быть парафразами, а
  не оригинальным текстом книг.
- Изменение persistent model требует отдельной EF Core migration и обновления
  `docs/database.md`. Точный DTO/endpoint и схема хранения выбираются при реализации, но не могут
  ослаблять перечисленные инварианты.

### ROT-MOUNT-ITEM-01. Четыре покупаемых профиля скакунов

В active RoT shop остаются ровно:

| Mount | Price | Rarity | Kind | B/A/I/C/W/P | Soak | WT | M/R Def | Skills | Capacity | Sil | Attack/Ability |
|---|---:|---:|---|---|---:|---:|---|---|---:|---:|---|
| Beast of Burden | 200 | 1 | Minion | 4/2/1/1/1/1 | 4 | 7 | 0/0 | group Athletics, Resilience | 18 | 2 | harness |
| Riding Beast | 400 | 2 | Minion | 4/3/1/1/1/1 | 4 | 5 | 0/0 | group Athletics, Resilience | 12 | 2 | riding tack |
| Flying Mount | 2000 | 8 | Rival | 3/4/1/2/2/2 | 3 | 12 | 1/2 | Athletics3, Coordination3, Discipline2, Resilience2, Survival2 | 12 | 2 | Flyer; Brawl dmg5 crit4 Engaged Knockdown |
| War Mount | 1500 | 6 | Rival | 4/3/1/2/3/1 | 4 | 14 | 0/0 | Athletics3, Brawl1, Discipline2, Resilience3, Survival2 | 13 | 2 | Brawl dmg6 crit4 Engaged Knockdown |

Flying Mount имеет `Talents: none`; печатный Dodge 2 удалён official errata.
Riding Beast в бою/стрессе требует Riding check, difficulty задаёт GM по ситуации.

Покупка атомарно создаёт `OwnedMount`/NPC instance с owner, source, wounds,
carried load, active state и mount gear; Enc самого mount null. Capacity профиля
имеет приоритет над `5+Brawn`.

Barding и Saddlebags используют mount ID и правила ROT-EQP-02. Другие book-external
generic mount/animal записи исключить из built-in RoT scope, сохранив custom и
legacy references.

Тесты всего gear/services/mount: каждое число/source page, state/modifier semantic,
consumption/load, service not inventory, atomic purchase, links, Flying errata,
barding/saddlebags only mount, migration/idempotency.

### ROT-WPN-01. Полные профили оружия и alternate attacks

`AttackProfile`: typed skill, `DamageKind=BrawnPlus|Fixed`, value, Crit, range,
qualities. HP RoT берётся из таблицы, а не вычисляется по Enc.

| code | Skill | Damage | Crit | Range | Enc | HP | Price | Rarity | Qualities |
|---|---|---:|---:|---|---:|---:|---:|---:|---|
| axe | Melee (Light) | Brawn+3 | 3 | Engaged | 2 | 1 | 150 | 1 | Vicious 1 |
| cestus | Brawl | Brawn+1 | 4 | Engaged | 1 | 0 | 40 | 1 | Disorient 3 |
| dagger | Melee (Light) | Brawn+2 | 3 | Engaged | 1 | 1 | 60 | 1 | Accurate 1 |
| flail | Melee (Heavy) | Brawn+4 | 3 | Engaged | 4 | 2 | 150 | 3 | Cumbersome 3, Linked 1, Unwieldy 3 |
| greataxe | Melee (Heavy) | Brawn+4 | 3 | Engaged | 4 | 2 | 300 | 4 | Cumbersome 3, Pierce 2, Vicious 1 |
| greatsword | Melee (Heavy) | Brawn+4 | 2 | Engaged | 3 | 2 | 300 | 4 | Defensive 1, Pierce 1, Unwieldy 3 |
| halberd | Melee (Heavy) | Brawn+3 | 3 | Engaged | 5 | 3 | 250 | 3 | Defensive 1, Pierce 3 |
| katar | Brawl | Brawn+1 | 2 | Engaged | 1 | 1 | 175 | 4 | Accurate 1 |
| mace | Melee (Light) | Brawn+3 | 4 | Engaged | 2 | 1 | 75 | 1 | — |
| military-pick | Melee (Light) | Brawn+1 | 2 | Engaged | 3 | 1 | 160 | 2 | Pierce 2 |
| pike | Melee (Heavy) | Brawn+4 | 4 | Short | 4 | 2 | 100 | 2 | Prepare 1 |
| shield | Melee (Light) | Brawn+0 | 6 | Engaged | 1 | 1 | 80 | 1 | Defensive 1, Deflection 1, Inaccurate 1, Knockdown |
| shield-large | Melee (Light) | Brawn+1 | 5 | Engaged | 2 | 2 | 160 | 2 | Defensive 2, Deflection 2, Inaccurate 2, Knockdown |
| shield-bulwark | Melee (Light) | Brawn+2 | 5 | Engaged | 3 | 2 | 280 | 3 | Cumbersome 4, Defensive 2, Deflection 3, Inaccurate 2, Knockdown, Reinforced |
| spear | Melee (Heavy) | Brawn+3 | 3 | Engaged | 3 | 1 | 110 | 2 | Accurate 1 |
| spear-light | Melee (Light) | Brawn+2 | 4 | Engaged | 2 | 1 | 90 | 1 | Accurate 1, Defensive 1 |
| staff | Melee (Heavy) | Brawn+2 | 4 | Engaged | 2 | 1 | 40 | 0 | Defensive 1 |
| sword | Melee (Light) | Brawn+3 | 2 | Engaged | 1 | 1 | 200 | 2 | Defensive 1 |
| war-hammer | Melee (Heavy) | Brawn+5 | 4 | Engaged | 4 | 2 | 600 | 3 | Concussive 1, Cumbersome 4, Inaccurate 1, Knockdown |
| bow | Ranged | 7 | 3 | Medium | 2 | 1 | 275 | 2 | Unwieldy 2 |
| crossbow | Ranged | 7 | 2 | Medium | 3 | 1 | 600 | 4 | Pierce 2, Prepare 1 |
| crossbow-hand | Ranged | 5 | 2 | Short | 2 | 0 | 750 | 5 | Pierce 1, Prepare 1 |
| crossbow-heavy | Ranged | 8 | 2 | Long | 4 | 2 | 1000 | 5 | Cumbersome 3, Pierce 3, Prepare 2 |
| crossbow-repeating | Ranged | 6 | 2 | Short | 3 | 2 | 800 | 7 | Linked 2, Prepare 2 |
| longbow | Ranged | 8 | 3 | Long | 3 | 2 | 450 | 4 | Unwieldy 3 |
| sling | Ranged | 4 | 4 | Medium | 0 | 0 | 20 | 0 | Disorient 2, Prepare 1; не получает Out of Ammo от Threat/Despair |
| throwing-axe | Ranged | Brawn+2 | 3 | Short | 1 | 1 | 50 | 1 | Inaccurate 1, Limited Ammo 1, Vicious 1 |

Один inventory instance поддерживает alternate profiles:

- dagger thrown: Ranged, Brawn+2, Crit 3, Short, Accurate 1, Limited Ammo 1;
- light spear thrown: Ranged, Brawn+2, Crit 4, Short, Accurate 1,
  Limited Ammo 1;
- throwing axe held: Melee (Light), Brawn+2, Crit 3, Engaged, Inaccurate 1,
  Vicious 1, без Limited Ammo.

Limited Ammo thrown-профиля означает недоступность экземпляра до подбора/возврата,
не автоматическое уничтожение. Pike атакует Short с Average (2) difficulty и не
атакует Engaged. Prepare 1 сохраняется.

Щит — weapon, не armor. Defensive/Deflection действуют, пока shield wielded, даже
при атаке другим оружием. Inaccurate применяется, только если shield profile
участвует в attack (primary либо secondary); Knockdown — только от его hit.

### ROT-ARM-01. Полная таблица брони

| code | Defense provider | Soak | Carried Enc | Worn Enc | HP | Price | Rarity | Special |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| brigandine | 1 | +1 | 2 | 0 | 1 | 400 | 5 | — |
| chainmail | 0 | +2 | 3 | 0 | 2 | 550 | 4 | +1 Setback к Stealth |
| heavy-robes | 1 | +0 | 1 | 0 | 1 | 45 | 0 | — |
| leather | 0 | +1 | 2 | 0 | 1 | 50 | 3 | — |
| padded | 0 | +1 | 2 | 0 | 0 | 35 | 2 | — |
| plate | 1 | +2 | 4 | 1 | 2 | 1000 | 6 | +2 Setback к Stealth |
| scale | 0 | +2 | 4 | 1 | 1 | 410 | 4 | +1 Setback к Stealth |

Worn Enc = `max(0, base Enc - 3)`. RoT-профиль имеет приоритет над одноимённым
generic fantasy Core item. Активная броня выбирается по ROT-CMB-02; неактивная
может иметь worn load, но не soak/defense/special/attachments.

### ROT-WPN-02. Craftsmanship

У weapon/armor instance ровно один immutable craftsmanship:
`Steel` по умолчанию или `Iron|Dwarven|Elven|Ancient`.

| Type | Armor | Weapon | Price | Rarity |
|---|---|---|---:|---:|
| Steel | без изменений | без изменений | ×1 | без изменений |
| Iron | Enc +2; worn +1 Setback к Athletics, Coordination, Riding, Stealth | Crit +1 | ×0.5 | −1 |
| Dwarven | Enc +1, HP +1 | Damage +1, Enc +1 | ×2 | +2 |
| Elven | Enc −2 (min 0); снять 1 Setback со Stealth при ношении | Damage −1 и Crit −1 (оба min 1) | ×2 | +3 |
| Ancient | Soak +1, provider Defense +1, Reinforced; HP −1 (min 0) | Damage +1, Crit −1 (min 1), Reinforced; HP −1 (min 0) | ×20 | ровно 10 |

Цена Iron = `floor(base/2)`. Rarity ограничить 0…10, кроме Ancient, которое
устанавливает 10. Reinforced у armor делает его защиту невосприимчивой к
Pierce/Breach, а сам предмет — к Sunder. Порядок:
base → craftsmanship → attachments → damage state → situational effects.

Craftsmanship не складывается. Если Signature Weapon Improved раскрывает Ancient,
Ancient заменяет предыдущий type. Если HP уменьшается и становится недостаточно,
владелец обязан немедленно выбрать установленный attachment для discard согласно
errata; операция upgrade не завершается с over-capacity.

Legacy без metadata → Steel; известный magic/signature type задаётся stable catalog
rule, не парсингом имени.

### Общая реализация WPN/ARM

- ItemDef: attack profiles, base HP, armor penalty, compatibility tags, page.
- CharacterItem: profile selection, craftsmanship, damage/ammo state, wielded/worn,
  active armor и attachments.
- Structured qualities authoritative; строка Properties — только display fallback.
- API возвращает base/effective profile и modifier breakdown; клиент не задаёт
  готовые damage/Defense/HP.
- Legacy over-capacity сохраняется с warning и запретом новых установок; custom
  content не удалять.
- Table-driven tests проверяют каждую клетку обеих таблиц, alternate/pike/sling/
  shield, one armor, worn load, Stealth и все craftsmanship combinations/floors/
  order/Ancient.

### ROT-EQP-ATT-01. Hard points, установка и enchantments

#### Hard points и совместимость

- RoT weapon/armor использует явный HP из таблиц выше. Для generic/custom без
  явного HP Core fallback:
  `ceil(baseEncumbrance / 2)`, по исходному Enc до modifiers; Enc0 → 0.
- `remainingHP = effectiveMaxHP - sum(HP enabled installed attachments)`.
  Установка требует достаточного HP и compatibility с конкретным host.
- HP0 attachment всё равно устанавливается и имеет host/state; это не пассивный
  gear в кармане.
- Один физический attachment instance не может быть на двух hosts.
- Core/RoT не устанавливает универсальный автоматический запрет двух экземпляров
  одного code. Поэтому `MultiplicityPolicy` должен быть явным. Для записей ниже
  обычный UI предупреждает и требует GM approval для дубля; повторный эффект **не
  складывается**, если его правило прямо не говорит increase/stack. Нельзя выдавать
  это application-решение за книжный hard rule.

#### Enchantments

`IsEnchantment=true` требует installer с хотя бы rank 1 в одном magic skill
активной системы. Один только карьерный статус rank0 недостаточен. GM override
разрешён с reason, так как конкретная форма magic ability остаётся решением GM.

#### Установка

Около часа и Average (2) Mechanics. Server принимает authoritative result check:

| Result | Последствие |
|---|---|
| Failure без Despair | не установлено; instance сохранён; можно повторить |
| Failure + Despair | не установлено; attachment instance уничтожен |
| Success без Despair | Installed |
| Success + Despair | Installed+Unstable; работает, пока GM не disable/detach, но может отказать в неудобный момент |

Ownership, HP, compatibility, instance availability и enchantment ability
проверяются до результата. Cost/item/effect state меняются атомарно.

Книга не задаёт Star-Wars-подобных покупаемых mods внутри attachment: поле
`Modifiers` — base effect, не upgrade slots. Detach — только GM-assisted audited
workflow с явным outcome `returned|destroyed|unusable`; normal safe detach возвращает
тот же instance и освобождает HP, не создавая копию/деньги.

Installed attachment не добавляет второй раз собственный inventory Enc, если его
эффект прямо не даёт +Enc. Его нельзя отдельно продать/передать/удалить; host sale
требует detach либо bundle transaction.

#### Model/API/UI

- `AttachmentDef`: HP, price nullable, rarity, enchantment, compatibility,
  multiplicity, typed effects, source/full/safe content.
- `CharacterItemAttachment`: host, instance/def, state, installer/check facts,
  unstable note.
- Минимальные effect operations:
  Add/Increase/SetAtLeast quality; Damage/Soak/Defense; automatic symbol;
  Crit reduction/forced Critical result; check dice; strain-event increase;
  Reinforced; Enc.
- Read DTO: base/effective HP/profile и breakdown. Commands: compatible list,
  install preview/resolve, GM detach/disable/enable, buy.
- Import/duplicate сохраняет host/state; невозможная комбинация rejected либо
  explicit legacy-review, но не silently dropped.
- UI: HP used/total, profile diff, compatible inventory, install check, magic
  requirement, unstable/priceless states.

Migration переносит built-in attachment definitions и существующие gear instances
в unattached inventory. Stack `quantity=N` не превращается заранее в N строк:
при начале install transaction сервер атомарно отделяет ровно одну единицу в
уникальный instance. Success связывает её с host; Failure без Despair возвращает
единицу в исходный stack; Failure+Despair удаляет только отделённый instance.
Concurrent install одной последней единицы допускает ровно одного победителя.
Ничего не устанавливать по догадке и не менять custom.

### ROT-EQP-ATT-02. Полный fantasy/RoT weapon attachment catalog

1. `Balanced Hilt` — HP1, 1000, R6; Melee (Light)/одноручное melee. Нет Accurate →
   Accurate1; есть Accurate N → N+1; есть Inaccurate → уменьшить его на1 до0,
   вместо выдачи Accurate.
2. `Duelist Cross Guard` — HP1, 800, R5; любой sword. После melee combat check
   против владельца с uncanceled Threat владелец once/check out-of-turn может
   получить 1 strain и добавить ещё 2 Threat в result.
3. `Explosive Missile` — HP1, 1250, R7; ranged weapon; Blast5. Постоянный
   attachment, не одноразовый боеприпас.
4. `Razor Edge` — HP1, 1250, R6; bladed close combat. Нет Pierce → Pierce2, иначе
   +1; Crit −1, min1.
5. `Recurve Limbs` — HP1, 300, R4; bow/crossbow. Нет Pierce →2, иначе +1; нет
   Unwieldy →2, иначе +1.
6. `Rune of Blades` — HP1, price null, R10, enchantment; bladed weapon, но не с
   деревянной рабочей кромкой. Critical Injury получает фиксированный результат
   `Bleeding Out` вместо d100. Для цели без Critical table — pending GM resolution,
   не автоматические wounds.
7. `Runic Flame` — HP1, 2000, R8, enchantment; melee;
   `Burn = max(existing Burn, 1)` (`SetAtLeast`, не +1).
8. `Runic Frost` — HP1, 1750, R8, enchantment; melee;
   `Ensnare = max(existing,1)` и `Stun = max(existing,4)`.
9. `Runic Thunder` — HP2, 2000, R8, enchantment; любое weapon;
   `Concussive = max(existing,1)`.
10. `Rune of Severing` — HP2, price null, R10, enchantment; bladed melee;
    effective Vicious = `max(existing,5)`, не +5.
11. `Serrated Edge` — HP1, 75, R2; bladed close combat; нет Vicious →1, иначе +1.
12. `Superior Weapon Customization` — HP1, 750, R7; любое weapon; Superior.
13. `Weighted Head` — HP1, 250, R2; blunt/crushing close combat без cutting edge;
    Damage +2; нет Cumbersome →2, иначе +1.
14. `Ynfernael Corruption` — HP1, price null, R8, enchantment; любое weapon;
    base damage +2. Пока его wield/wear, каждое отдельное положительное событие
    strain увеличивается на 1. Не срабатывает на 0, wounds/damage или просто carried.

Core-compatible fantasy attachments остаются в RoT, потому что RoT их не отменяет;
неподходящие sci-fi attachments не включать автоматически.

### ROT-EQP-ATT-03. Armor attachments и errata

1. `Deflective Plating` — HP1, 450, R4; любая armor; ranged Defense +1, только у
   active worn armor.
2. `Gilded` — HP0, 1500, R5; любая armor; пока active+worn, один Boost к Charm,
   Negotiation, Leadership.
3. `Intimidating Visage` — HP0, 125, R3; любая armor; active+worn: один automatic
   Success к Coercion и один automatic Failure к Charm. Не Advantage/Threat.
4. `Ironbound Rune` — HP2, price null, R10, enchantment; metal armor; soak +1 и
   General Defense +1, затем cap4.
5. `Reinforced Plating` — HP2, 8000, R7; hardened plate armor; Reinforced и Enc +1.
6. `Spikes` — **HP1** по official errata, 600, R4; plate. Когда wearer является
   целью melee combat check, после roll можно потратить из attacker result
   3 Threat или 1 Despair: attacker получает 3 wounds без soak. Once/check,
   независимо от hit, если check действительно targeting wearer.
7. `Twilight Rune` — HP1, price null, R10, enchantment; любая armor; active+worn:
   два Boost к Stealth и ranged Defense +2; melee Defense не повышать.

Price null — обычная продажа/покупка недоступна. Source page/authority хранить
печатными: Core attachments pp.207–209, RoT weapon attachments pp.106–107,
armor pp.107–108; Spikes помечен `OfficialErrataV1.1`. Не сохранять текущий
сдвиг страниц +1.

#### Acceptance

- Seed tests всех 21 записей: code/HP/price/rarity/enchantment/compatibility/page.
- Positive/negative compatibility; HP boundaries, HP0, duplicate с GM policy,
  explicit RoT HP.
- Четыре installation outcomes; magic rank0/rank1/override; unstable lifecycle.
- Каждый effective effect: Accurate/Inaccurate, set-at-least Vicious, cap4,
  Ynfernael events, forced Bleeding Out, Spikes miss/hit/soak bypass.
- Installed sell/detach/bundle, priceless, migration quantity; auth/audit/
  import/export/duplicate/UI/content modes.

### GEN-EQP-DMG-01. Состояние, Sunder и ремонт

Состояние принадлежит item instance:

| State | Использование | Repair |
|---|---|---|
| Undamaged | без штрафа | не нужен |
| Minor | один Setback ко всем checks, непосредственно использующим item | Easy (1) |
| Moderate | increase difficulty один раз | Average (2) |
| Major | unusable; никаких attack/armor/attachment/tool benefits | Hard (3) |
| Destroyed | unusable, обычный repair недоступен | только GM disposal/особое правило |

Minor и Moderate не складываются. Major armor сохраняет физический load, но не
soak/Defense/effects. Major container сохраняет ссылки на содержимое, но его
capacity/threshold bonus отключён; содержимое не теряется.

`Sunder`: active, 1 Advantage, доступен даже при miss, repeatable в одной check,
но все активации направлены в один открыто wielded/используемый target item.
Каждая переводит на одну ступень до Destroyed. Reinforced полностью immune; preview
не списывает symbols. Скрытый inventory item требует GM override.

#### Repair

- Mechanics по умолчанию; иной skill — GM override с reason.
- Adequate time обычно 1–2 часа на каждый уровень base difficulty: Minor 1–2,
  Moderate 2–4, Major 3–6. Меньше выбранного adequate time → +1 Difficulty; нет
  proper tools → ещё +1; cumulative.
- Base material cost: 25%/50%/100% current base instance price для
  Minor/Moderate/Major. Craftsmanship multiplier учитывается, региональная
  торговая наценка и attachment prices — нет. `price=null` требует GM quote.
- Self-repair уменьшает стоимость на 10% за каждый net Advantage, минимум 0.
  Округлять вверх до целой silver — явная `ProductDecision`, потому что дробные
  монеты/округление книга не определяет.
- Момент списания материалов книга не фиксирует. Для однозначности продукта:
  резервировать/списывать при начале попытки независимо от success и явно
  пометить это `ProductDecision`; если команда выберет иной вариант, он должен быть
  global config с теми же тестами, а не различаться по endpoint.
- Success → Undamaged; failure оставляет state. Не автоматизировать Threat/Despair
  сверх выбранного GM outcome.

Attachment может иметь собственное damage state; Major/Destroyed не даёт effect,
но HP освобождается только после detach/disposal. Все transitions/costs
idempotent/audited. Legacy → Undamaged.

Тесты: все transitions, multi-Sunder same target/miss/Reinforced; penalty на
weapon/armor/tool/container; time/tools; 25/50/100, discount/round/null; atomic
money/result, ownership и round-trip.

> **Вне скопа: нужен рантайм столкновения.** Разбор сохранён в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md):
> - **ROT-EQP-AMMO-01** — narrative ammo и OutOfAmmo за Despair конкретной проверки.

### GEN-EQP-QUAL-01. Структурные качества, нужные RoT

Metadata: Active/Passive, rated, cost, TriumphAllowed, hit requirement,
CanActivateOnMiss, repeatability/limit, target scope, effect type. По умолчанию
Active стоит 2 Advantage и требует successful hit; Triumph может оплатить
activation. Исключения ниже authoritative.

- `Accurate N`: passive, N Boost к attack этим weapon.
- `Blast N`: active. На hit за 2 Advantage каждый персонаж Engaged с original
  target получает отдельный hit `N + netSuccesses`; original target не получает
  второй hit. На miss за 3 Advantage original target и все Engaged получают N без
  Success. Soak отдельно каждому; тесное пространство — GM area override.
- `Breach N`: ignore N vehicle armor или `10×N` personal soak. Reinforced armor
  immune.
- `Burn N`: 2 Advantage на hit; target в начале каждого своего хода N rounds
  получает base weapon damage без первоначальных net Success, soak каждый раз.
  Разные hit targets активируются отдельно. Обычное пламя: Action, Average
  Coordination на твёрдой/ Easy на мягкой поверхности; вода прекращает сразу.
  Chemical/magic tag может запретить это по GM.
- `Concussive N`: hit target Staggered N rounds; повтор — для разных hit targets.
- `Cumbersome N`: если Brawn<N, increase difficulty checks с item
  `N-Brawn` раз.
- `Defensive N`/`Deflection N`: while wielded +N melee/ranged Defense как increase,
  общий cap4.
- `Disorient N`: hit target Disoriented N rounds; один Setback ко всем checks.
- `Ensnare N`: hit target Immobilized N rounds; Action Hard Athletics освобождает.
- `Inaccurate N`: N Setback к attacks этим weapon.
- `Knockdown`: hit target Prone; 2 Advantage +1 за каждый silhouette target сверх1.
- `Limited Ammo N`: ROT-EQP-AMMO-01.
- `Linked N`: на successful attack каждые 2 Advantage дают ещё один hit, максимум
  N дополнительных; одна original target; каждый hit base+те же net Success;
  critical/quality разрешаются отдельно по hit.
- `Pierce N`: ignore до N soak, min0; Reinforced armor immune.
- `Prepare N`: N Maneuvers перед использованием.
- `Reinforced`: item immune Sunder; armor soak immune Pierce/Breach; не immunity ко
  всем narrative hazards.
- `Stun N`: active hit; target получает N strain без soak.
- `Sunder`: GEN-EQP-DMG-01.
- `Superior`: automatic Advantage на каждую check, связанную с использованием.
- `Unwieldy N`: если Agility<N, increase difficulty на `N-Agility`.
- `Vicious N`: +`10×N` к Critical Injury/Hit roll; не создаёт critical и не
  снижает его цену.

Seed validation запрещает broken quote artifacts и несогласованные active/rating/
cost fields; current Blast/Burn/Sunder строки исправить. PrivateFull даёт полный
RU/EN парафраз, PublicSafe — short text, но structural timing/cost виден.

Тесты: metadata/defaults; Blast hit/miss/soak; Burn duration/extinguish; deficits;
Defense cap; Ensnare; Knockdown silhouette; Linked; Pierce/Breach/Reinforced;
Stun/Sunder/Superior/Vicious и отсутствие мусорных кавычек.

### ROT-MAG-IMP-01. Magic implements как настроенные instances

На одной magic check используется максимум один Wielded/Used implement. Carried не
работает; Staff+Wand/два материала не складываются. Material — immutable instance
property, не attachment/craftsmanship и не HP. Runebound shards не получают
Bone/Oak/Hazel/Willow/Yew.

Difficulty сначала строится из action/effects, затем implement делает конкретный
effect бесплатным; итог не опускается ниже base difficulty action.

| Implement | Attack damage | Enc | Price | Rarity | Effect |
|---|---:|---:|---:|---:|---|
| Holy Icon | +0 | 0 | 250 | 4 | Каждый Divine-only effect стоит на 1 difficulty меньше; successful Heal дополнительно heal 2 wounds once/spell |
| Magic Scepter | +2 | 1 | 350 | 5 | один Boost к magic check; Close Combat бесплатно |
| Magic Staff | +4 | 2 | 400 | 6 | первый Range бесплатно |
| Magic Tome | +0 | 1 | 750 | 7 | до двух immutable effect codes бесплатно; сумма их обычных increases ≤3 |
| Magic Wand | +3 | 1 | 400 | 7 | один immutable effect с обычной ценой ровно +1 бесплатно |
| Musical Instrument | +0 | 1 | 200 | 4 | только Verse; Additional Target бесплатно |

Attack damage bonus не является melee damage и не влияет на Heal/Curse. Tome/Wand
choices определяет **GM**, когда конкретный instance изготовлен или впервые
получен, после чего они неизменяемы:

- Tome: 0–2 разных effect codes. Обычно сумма их printed difficulty increases не
  выше 3; превышение — не silent hard ban, а явный GM override с reason, потому что
  формулировка книги является рекомендацией GM. На cast бесплатно применяются
  только выбранные effects, уместные для action и magic skill.
- Wand: ровно один effect code, printed increase которого равен +1. Иное значение
  отклоняется; GM не может этим выбором обойти skill availability/exclusion.

Shop/career package не может создать не настроенный Tome/Wand как полностью
рабочий. Выдача создаёт pending `ImplementConfiguration`; до GM-confirm его
обычные stats/материал существуют, но free-effect не применяется. Legacy instance
без choices получает тот же pending state, если snapshot не позволяет однозначно
восстановить выбор.

### ROT-MAG-MAT-01. Материалы implements и errata

| Material | Price multiplier | Rarity | Trigger |
|---|---:|---:|---|
| Bone | **×1.5** | +2 | после successful Attack/Curse heal caster 1 wound, once/check |
| Oak | ×1 | +0 | нет |
| Hazel | **×1.5** | +1 | если есть Triumph, once/check бросить один Boost и добавить symbols; Triumph остаётся |
| Willow | ×2 | +2 | после successful spell добавить automatic Advantage, once/check |
| Yew | **×1.5** | +1 | после successful Augment/Barrier/Heal heal caster 1 strain, once/check |

Bone/Hazel/Yew ×1.5 — обязательная official errata; печатное ×0.5 не использовать.
Effective rarity clamp 0…10; дробную custom price округлять вверх как видимую
ProductDecision.

Полная контрольная матрица `(Bone, Oak, Hazel, Willow, Yew)`:

| Implement | Цена/Rarity по пяти материалам |
|---|---|
| Holy Icon | 375/R6; 250/R4; 375/R5; 500/R6; 375/R5 |
| Magic Scepter | 525/R7; 350/R5; 525/R6; 700/R7; 525/R6 |
| Magic Staff | 600/R8; 400/R6; 600/R7; 800/R8; 600/R7 |
| Magic Tome | 1125/R9; 750/R7; 1125/R8; 1500/R9; 1125/R8 |
| Magic Wand | 600/R9; 400/R7; 600/R8; 800/R9; 600/R8 |
| Musical Instrument | 300/R6; 200/R4; 300/R5; 400/R6; 300/R5 |

`ImplementDef/MaterialDef`, CharacterItem material/choices и shop/spell previews —
typed. Сервер считает price/rarity/difficulty/damage/trigger; покупка атомарна.
Legacy implement → Oak с audit warning. Tests: 6×5 matrix, one implement,
каждый free effect/choice budget, every trigger success/failure/once, no shard
material, round-trip/content modes. Source: RoT printed pp.98–99; errata authority.

### ROT-EQP-SRC-01. Источники и приоритет

> **Финальное решение по объёму (2026-07-30): выполнено.** Текущие источники записей и
> действующий приоритет совместимых Core rules, RoT overrides и official errata приняты
> владельцем как достаточные. Расширение единственной строки источника до отдельных полей
> `BookCode`, edition/language, printed page range, section, authority и errata version не
> считается незавершённой работой этого пункта. Требования ниже сохраняются как архив аудита.

Вместо единственной строки хранить BookCode, edition/language, printed page range,
section, authority (`Core|RoTOverride|OfficialErrata`) и errata version. Display
генерируется; printed page не вычисляется PDF-offset.

Контрольные страницы:

- RoT melee weapons 92–94 (table 94), ranged 95, armor 96, craftsmanship 97;
- gear 100–101, animals/gear 104–105, services 105;
- weapon attachments 106–107, armor attachments 107–108;
- Core qualities/maintenance 86–89, install 206–209.

Spikes/Flying Mount → official errata. RoT profile/HP побеждает одноимённый Core;
совместимые Core rules остаются. Seed tests запрещают blanket +1 offset; migration
обновляет built-in only.

---

## 9. Магия: пункты 1–4, 6, 10–12

> Нумерация сохраняет номера выбранных пунктов аудита. Самостоятельная задача
> аудита 9 сюда намеренно не добавлена; её правило нельзя незаметно подменить
> побочным изменением.

### ROT-MAG-01. Структурная доступность actions/effects для магических навыков

Базовая RoT-матрица:

| Magic action | Arcana | Divine | Primal | Runes | Verse |
|---|:---:|:---:|:---:|:---:|:---:|
| Attack | да | да | да | да | нет |
| Augment | нет | да | да | да | да |
| Barrier | да | да | нет | да | нет |
| Conjure | да | нет | да | нет | нет |
| Curse | да | да | нет | да | да |
| Dispel | да | нет | нет | нет | да |
| Heal | нет | да | да | нет | да |
| Utility | да | да | да | да | да |

EPG actions `Mask`, `Predict`, `Transform` могут оставаться как явно
`Optional/EPG`, но не входят в RoT-native matrix и не должны менять таблицу выше.

Additional effect наследует доступность parent action, кроме явных ограничений:

- Attack `Manipulative` — только Arcana;
- Attack `Non-Lethal` — только Primal;
- Attack `Holy/Unholy` — только Divine;
- Barrier `Reflective` — только Arcana;
- Barrier `Sanctuary` — только Divine;
- Augment `Divine Health` — только Divine;
- Augment `Primal Fury` — только Primal;
- Curse `Despair` — только Divine;
- Curse `Doom` — только Arcana.

Каждая запись эффекта должна содержать `AllowedSkills`, `ParentAction`,
`DifficultyIncrease`, `Repeatability`, `Exclusions`, `ResolutionKind`, а не
ограничение внутри Description. Builder фильтрует и валидирует одну и ту же
матрицу; server отклоняет вручную подставленный effect. Reference/PrivateFull
объясняет недоступность.

Тесты: вся таблица 8×5, все девять исключений, Core без Runes/Verse, EPG opt-in,
tampered API request и parity UI/backend. **Миграция:** structured spell metadata.

### ROT-MAG-02. Повторяемость дополнительных эффектов

По умолчанию один named additional effect выбирается не более одного раза. Повтор
разрешён только при явном `Repeatable=true`.

- `Range` можно выбирать несколько раз; каждый выбор повышает дальность на один
  band и добавляет свою difficulty.
- Если EPG включён, повторяемы только прямо отмеченные его эффекты, например
  увеличения `Size/Silhouette`; EPG-правило не переносится на одноимённый RoT
  effect автоматически.
- `Additional Target`, `Additional Summon` и сходные записи выбираются в builder
  один раз. Их post-roll трата Advantage может повторяться столько раз, сколько
  разрешает собственное описание; это не повторный выбор difficulty effect.
- Blast, Burn, Deadly, Destructive, Empowered, Haste, Swift, Paralyzed и прочие
  неотмеченные эффекты второй раз выбрать нельзя.

Builder хранит multiset только для действительно repeatable effects; для остальных
duplicate request возвращает error, не silently dedupe. `Signature Spell` сравнивает
нормализованный multiset с учётом допустимой multiplicity.

Тестировать Range 0/1/3, duplicate non-repeatable, post-roll repeat отдельно,
EPG-disabled, order-independent Signature Spell. **Миграция:** repeatability flag;
legacy duplicate spell preset пометить invalid и предложить repair.

### ROT-MAG-03. Несовместимые эффекты и таланты

Builder и server обязаны отклонять весь набор, если:

- Curse `Despair` совмещён с `Additional Target`;
- Curse `Paralyzed` совмещён с `Additional Target`;
- owned/selected `Chill of Nordros` и `Flames of Kellos` присутствуют вместе;
- owned/selected `Dominion of the Dimora` и `Favor of the Fae` присутствуют вместе.

Первые две связи относятся к конкретному cast; вторые две — к покупке талантов и
эффектам spell construction. Exclusion двусторонний, основан на stable codes.
Нельзя скрыть кнопку только на frontend и принять invalid payload напрямую.

Legacy illegal spell preset остаётся читаемым с warning, но не кастуется до repair.
Legacy illegal talents обрабатываются по ROT-TAL-02. Тесты проверяют оба порядка,
atomic rejection, unrelated effects и UI disabled reason.

### ROT-MAG-04. Не перепутывать Haste и Swift

В Augment:

- `Haste`, difficulty +1: цель может сделать второй Maneuver в свой ход без
  обычных 2 strain; общий максимум два maneuvers сохраняется.
- `Swift`, difficulty +1: цель игнорирует difficult terrain и не может быть
  immobilized на время эффекта.

Исправить stable-code mapping, RU/EN names, full/safe descriptions и typed effects.
Миграция должна сохранять **фактический semantic effect**, а не ошибочную подпись:

- если legacy snapshot/версия однозначно доказывает механику «второй Maneuver без
  2 strain», перенести в canonical `haste`;
- если доказывает «ignore difficult terrain и immunity immobilized», перенести в
  canonical `swift`;
- если хранится только code/name из версии, где mapping был перепутан и смысл
  восстановить нельзя, оставить raw snapshot, поставить `SpellPresetNeedsReview`
  и предложить две явно описанные кнопки; preset до выбора не castable;
- repair меняет один code, пересчитывает signature multiset/difficulty и пишет
  audit; он никогда не включает оба эффекта как «безопасный» вариант.

Тесты: второй maneuver, третий запрещён, terrain/immobilized, отсутствие
перекрёстного эффекта, обе локали и старый preset.

> **Вне скопа: нужен рантайм столкновения.** Разбор сохранён в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md):
> - **ROT-MAG-05** — дополнительный призыв за преимущества после броска;
> - **ROT-MAG-07** — Misfortune: поворот брошенной Setback-кости.

### ROT-MAG-06. Удалить дубликат Attack «Move»

В официальной RoT-модели существует одна запись этого эффекта —
`Manipulative`, difficulty +1, только Arcana. После успешного hit заклинатель может
потратить 1 Advantage и переместить цель на один range band в любом направлении.

Удалить дублирующую built-in запись `Move` из active catalog/builder. Если legacy
preset ссылается на старый code, мигрировать ссылку на `Manipulative` один к одному
и записать warning; если в одном preset были оба, оставить один и пересчитать
difficulty с явным repair report, а не сохранить двойной бонус. Custom same-name
не трогать.

Тесты: exact active count, Arcana-only, hit/1 Advantage/range, miss/no symbols,
idempotent seed и legacy mapping.

> **Вне скопа: нужен рантайм столкновения.** Разбор сохранён в
> [rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md):
> - **ROT-MAG-08** — двухфазный протокол каста, серверный бросок и стоимость заклинания.

### ROT-MAG-10. Только Knowledge (Lore) для рейтингов RoT

В RoT ссылка magic effect на «Knowledge» означает ranks
`Knowledge (Lore)`, если конкретный talent/ability прямо не заменяет навык.
Рейтинг Blast, Burn, Ensnare, Stun, Vicious, Disorient, Pierce и иных **числовых**
effects, прямо зависящих от Knowledge, берётся из Lore. `Sunder` не имеет rating:
это boolean quality, и Knowledge не превращает его в `Sunder N`. Generic
`Knowledge`, наибольший
knowledge skill и клиентский rating запрещены.

Явное исключение `Dark Insight` позволяет для описанных им spell ratings
использовать Knowledge (Forbidden). Resolver возвращает источник (`Lore` или
`DarkInsightForbidden`) и значение. При rank 0 рейтинг 0 обрабатывается по общему
правилу качества; не выдумывать minimum 1.

Тесты: Lore/Forbidden с разными ranks, без Dark Insight, с ним, custom same-name,
PC/NPC и каждый затронутый quality.

### ROT-MAG-11. Runebound shard как обязательный implement Runes

Для Runes cast персонаж должен держать/использовать **ровно один** runebound shard,
связанный с выбранной rune/item instance:

- без shard Runes action отклоняется до roll/strain;
- shard является implement для cast; второй shard или иной magic implement в том
  же cast не добавляет свой эффект;
- для Arcana/Divine/Primal/Verse implement остаётся необязательным, если другое
  правило не требует его;
- activation effect самого shard/rune, не являющийся Runes magic action, не требует
  отдельной Runes check.

Request передаёт instance ID, server проверяет ownership, equipped/held state,
runebound type и single-implement invariant. UI Runes builder требует picker и
скрывает несовместимые implements. Продажа/снятие выбранного shard инвалидирует
preset с понятным warning.

#### Полный каталог 17 runebound shards

Все shards имеют Enc 0, `Price=null`, `Rarity=null`, `Purchasable=false` и
`Sellable=false`: отсутствие цены/rarity в книге нельзя превращать в `0/1`.
Обычно Activation стоит Maneuver; если строка ниже задаёт Action, passive condition
или собственную частоту, она заменяет default. Любой персонаж может использовать
Activation. Implement-часть доступна только при `Runes` как career skill **и**
`Runes rank >=1`, применяется только к Runes cast и может быть единственным
implement.

1. `arcane-bolt-rune`.
   - Activation: до конца текущего хода shard является оружием `Ranged`,
     Fixed Damage 8, Crit 3, Medium, Auto-fire.
   - Implement Attack: первый `Range` бесплатен; `Impact` обязателен и бесплатен;
     base damage spell +4.
2. `blasting-rune`.
   - Activation: до конца хода оружие `Discipline`, Fixed Damage 9, Crit 3,
     Medium, Blast 7, Knockdown.
   - Implement Attack: `Blast` обязателен, `Impact` опционален; оба не повышают
     difficulty; base damage +5.
3. `ice-storm-rune`.
   - Activation: до конца хода оружие `Discipline`, Fixed Damage 7, Crit 2,
     Medium, Blast 4, Ensnare 3.
   - Implement Attack: `Ice` и `Blast` обязательны и бесплатны; base damage +4.
4. `immolation-rune`.
   - Activation: до конца хода оружие `Discipline`, Fixed Damage 8, Crit 3,
     Short, Burn 2.
   - Implement Attack: `Fire` и `Deadly` обязательны и бесплатны. После roll, до
     damage allocation caster может добровольно получить `N` wounds без soak,
     `0 <= N <= floor(WT/2)`, и добавить `N` damage ко **всем** hits этого spell.
5. `lesser-rune`.
   - При получении GM один раз фиксирует небольшой полезный activation effect:
     например свет уровня torch, маленькую иллюзию, перенос голоса или поджигание
     трута. Activation — Maneuver и разрешает только сохранённый effect.
   - GM также фиксирует один тематически связанный additional effect с printed
     difficulty +1. Implement добавляет его бесплатно в любой подходящий spell;
     Attack base damage +3. Выборы immutable и не обходят skill matrix.
6. `lightning-strike-rune`.
   - Activation: до конца хода оружие `Discipline`, Fixed Damage 8, Crit 3, Long,
     Auto-fire, Disorient 3.
   - Implement Attack: первый `Range` бесплатен; `Lightning` обязателен и
     бесплатен; base damage +5.
7. `rune-of-collection`.
   - Activation: Maneuver, только видимое свечение без дополнительного rule effect.
   - Implement любого Runes spell: final difficulty −1 и casting cost strain −1,
     оба с floor 0; прочие reductions применяются общим pipeline.
8. `rune-of-fate`.
   - Activation: Action, перевернуть один Story Point `player → GM`; к следующей
     check, которая targeting bearer, добавить один automatic Despair. Pending
     effect живёт до такой check или конца session и расходуется ровно один раз.
   - Implement Augment/Curse: `Additional Target` можно добавить бесплатно; Curse
     обязан также получить бесплатный `Doom`.
9. `rune-of-misery`.
   - Activation: Action, bearer получает 3 strain и выбирает цель в Short; target
     disoriented 3 rounds. Пока именно этот condition действует, bearer может
     потратить 2 Threat из любой check target, чтобы target получил 1 wound.
   - Implement Curse: final difficulty −2. GM может потратить 2 Threat из этой cast
     check, чтобы caster получил 1 wound.
10. `soulstone-rune`.
    - Activation: Maneuver; все **другие** characters в Short, включая allies,
      делают Average (2) Discipline OOT. Провал: 3 strain и staggered 1 round.
      Bearer heal 1 wound за каждую провалившую цель. Для Minion GM может вместо
      routing strain выбрать немедленный defeat; выбор фиксируется по каждой
      группе до mutation.
    - Implement Curse: каждая затронутая spell target дополнительно получает wounds
      в количестве ranks Knowledge (Forbidden). Пока Curse active, caster heal
      1 wound при каждом отдельном wound event каждой target, включая initial
      application; величина event не умножает healing.
11. `stasis-rune`.
    - Activation: Action, bearer получает 2 strain, одна цель в Short становится
      staggered и immobilized до конца её следующего хода.
    - Implement Curse: `Paralyzed` обязателен и бесплатен.
12. `sunburst-rune`.
    - Activation: до конца хода оружие `Ranged`, Fixed Damage 4, Crit 1, Medium,
      Breach 1.
    - Implement Attack: `Holy` обязателен и бесплатен, spell получает Breach 1.
      Это явное исключение, позволяющее Holy при Runes.
13. `teleportation-rune`.
    - Activation: Action. Телепортировать bearer либо один принадлежащий ему
      silhouette 0 item в видимую точку в пределах Extreme без check. Для точки,
      которую bearer ранее лично посещал, но сейчас не видит, нужна Average (2)
      Vigilance; GM до roll может добавить обстоятельства. Despair или несколько
      Threat GM может потратить на серьёзное отклонение места назначения.
    - Implement любого Runes spell: первые три `Range` additions бесплатны.
14. `terror-rune`.
    - Activation является passive: пока shard физически у персонажа, он игнорирует
      effects fear; Maneuver не нужен.
    - Implement: friendly targets этого spell невосприимчивы к fear на его
      duration; каждая enemy target немедленно делает Daunting (4) fear check OOT.
15. `vision-rune`.
    - Activation: Action + Average (2) Perception. Выбрать ровно одно: наблюдать
      местность в пределах трёх дней пути как с места присутствия либо видеть
      сквозь один твёрдый объект в Medium как сквозь прозрачный.
    - Implement: можно выбрать любую target в spell range без line of sight, в том
      числе за стеной/в darkness; range и иные target restrictions сохраняются.
16. `wanderers-stone`.
    - Activation: once/encounter Action, heal bearer 5 strain; не лечит wounds
      Rival/Minion.
    - Implement Augment: `Haste` и `Swift` опциональны и бесплатны по отдельности
      либо вместе.
17. `ynfernael-rune`.
    - Activation: Action; выбрать `N`, `0 <= N <= WT`. Bearer сначала получает N
      wounds без soak, затем **каждый character в Short, включая bearer и allies**,
      получает ещё N wounds без soak. Target set и preview фиксируются до первого
      wound; вся операция атомарна.
    - Implement Attack: `Empowered` и `Deadly` обязательны и бесплатны. После
      resolution caster получает 1 wound; если magic check failed, ещё 4 wounds.
      Base damage +3.

Mandatory/free effects всё равно проходят exclusions. Если обязательная комбинация
невозможна из-за другого выбранного effect/talent, cast отклоняется в preview; shard
не даёт права убрать mandatory effect. Прямое Activation weapon использует обычный
combat resolver и не требует Runes rank.

Migration меняет старые shard `price=0,rarity=1` на nullable только у exact
built-in codes. Unconfigured Lesser Rune получает `ShardConfigurationRequired`;
его не настраивать по display description. Existing inventory IDs сохраняются.

Тесты: exact 17-code fixture; 0/1/2 shards, shard+staff, чужой/не held instance;
каждая Activation/Implement строка, mandatory/free/exclusion, direct weapon
profiles, all timing/cost/range/periodic events, priceless economy, Lesser
configuration, atomic strain/wounds, export/import и content modes.
**Миграция:** implement type/instance linkage, nullable economy и typed shard
effects.

### ROT-MAG-12. Полный Conjure и запрет stacking Augment

#### Base Conjure

- Easy (1), только Arcana или Primal.
- Создаёт один простой предмет без движущихся частей, одно одноручное melee weapon
  без движущихся частей либо одного Minion silhouette ≤ 1.
- Созданное появляется Engaged с caster и существует до конца следующего хода
  caster. Concentrate maneuver в последующие ходы продлевает существование.
- Призванное существо по умолчанию руководствуется природными инстинктами, не
  контролируется caster и может быть враждебным. В structured encounter действует
  сразу после caster.

Additional effects:

- `Summon Ally` (+1): существо дружелюбно и следует приказам; одним Maneuver caster
  отдаёт команды всем своим активным summons.
- `Medium Summon` (+1): разрешает сложный инструмент с движущимися частями,
  Rival silhouette ≤ 1 или двуручное melee weapon.
- `Grand Summon` (+2): разрешает Rival silhouette ≤ 3.
- `Range` и `Additional Summon` работают по ROT-MAG-02/05.

Conjured item — временный instance с expiry, не бесплатный permanent inventory и не
доступен продаже/crafting. Creature profile, disposition, owner turn hook,
Concentrate и cleanup хранятся структурно. Если caster не концентрируется после
первого срока, все соответствующие summons исчезают.

#### Augment stacking

Один персонаж не может одновременно находиться под действием более одного Augment
spell. Базовая реализация выбирает однозначную политику `Reject`: новый cast,
который должен затронуть уже Augmented target, отклоняется **до**
`StartMagicCheck`, пока старый effect не истёк/не был законно завершён. Не
добавлять `replace`, так как книга его прямо не предоставляет. Если среди
Additional Targets хотя бы одна цель уже имеет Augment, отклоняется весь cast;
частичного применения нет. Эффекты двух casts никогда не складываются. Несколько
свободных targets одного cast допустимы.

Тесты Conjure покрывают каждую категорию base/Medium/Grand, skill matrix, range,
disposition, initiative hook, Concentrate/expiry, temp inventory и Additional
Summon. Тесты Augment — same/different caster, multi-target, expiry/replacement.

### ROT-MAG-CONTENT. Полнота PrivateFull магии

Все восемь Core actions, их RoT skill variants, все их additional effects,
RoT-only Runes/Verse, implements, runebound shards и symbol-spend guidance получают
полный оригинальный RU/EN-парафраз. EPG-entries хранятся отдельно и помечаются
источником. Coverage validator сверяет structured catalog с private overlay;
`PublicSafe` остаётся структурно полным, но без full text.

---

## 10. Magic items, crafting и alchemy

### ROT-MITEM-01. Полный исполняемый каталог 17 магических предметов

#### Общие правила

- Каталожный magic item — уникальная реликвия. У него нет обычной цены:
  `Price = null`, `Purchasable = false`, `Sellable = false`. Значение `null` нельзя
  сериализовать как `0` и тем самым сделать предмет бесплатным.
- Такие предметы почти никогда не продаются и по умолчанию выдаются только GM.
  Обычный crafting не создаёт запись из этого каталога. Редкое зачарование —
  отдельный GM-guided workflow ROT-CRAFT-MAGIC-01, а не способ заказать копию
  именованной реликвии.
- У каждого owned экземпляра есть `CharacterItemId`, definition/version snapshot,
  состояния `carried|worn|wielded`, structured attack profiles, qualities и typed
  effects. Эффект работает только в указанном состоянии; снятие предмета немедленно
  завершает временный modifier.
- Временное снижение threshold не удаляет accumulated wounds/strain и не
  пересчитывает creation snapshot. Если после снятия предмета накопленное значение
  превышает threshold, применяются обычные правила превышения порога.

#### Точная таблица и полная механика

1. **Bloodscript Ring** (`bloodscript-ring`): Enc 0, rarity 9.
   Пока кольцо надето, каждый раз, когда владелец завершает cast spell, все
   **остальные** персонажи в Short немедленно выполняют fear check как
   out-of-turn incidental. Difficulty равна ranks владельца в
   `Knowledge (Forbidden)`; при rank 0 это Simple (0), minimum 1 не добавляется.
   Союзники не исключаются, сам владелец не является целью. Пока кольцо надето,
   его Strain Threshold уменьшается на 1.
2. **Cloak of Mists** (`cloak-of-mists`): Enc 1, rarity 10; Enc 0, когда надет.
   После определения входящего damage, пока пакет ещё можно уменьшить до
   превращения результата в wounds, владелец может как out-of-turn incidental
   перевернуть один player Story Point в GM и добровольно получить `N` strain.
   `N` — целое от 1 до размера damage включительно и не больше текущего Strain
   Threshold. Damage total уменьшается на `N`, затем обычный damage pipeline
   применяет soak/исключения. Story Point, strain и reduction атомарны; уже
   записанные wounds задним числом не лечатся.
3. **Dead Man’s Compass** (`dead-mans-compass`): Enc 0, rarity 10.
   Только после броска, но до разрешения результата Knowledge (Geography),
   Perception или Vigilance check, сделанной для прокладки маршрута, поиска места,
   предотвращения потери пути или иной навигации, владелец может incidental
   выбрать целое `N ≥ 1`, получить `N` wounds и добавить `N` automatic Success.
   Эти wounds не являются damage и не уменьшаются soak. Верхнего лимита по
   threshold нет. Если выбранные wounds приводят к превышению WT, к немедленному
   Critical Injury roll за превышение добавить +50.
4. **Deepwood Longbow** (`deepwood-longbow`): Ranged, damage 8, critical 2,
   Extreme, Enc 4, rarity 6; Accurate 1, Cumbersome 3, Superior, Unwieldy 3.
   В естественной природной среде, под открытым небом и над землёй effective
   Accurate становится 3. Это `SetAtLeast(Accurate, 3)`, а не Accurate 4.
5. **Elven Boots** (`elven-boots`): Enc 1, rarity 6; Enc 0, когда надеты.
   Число movement maneuvers для смены range bands уменьшается на 1, минимум 1.
   Это не даёт бесплатного движения и не отменяет отдельные требования
   disengage/terrain, если они не являются именно дополнительным maneuver смены
   диапазона.
6. **Gauntlets of Power** (`gauntlets-of-power`): Enc 1, rarity 8; Enc 0, когда
   надеты. Каждая Brawn-based skill check получает один automatic Success и один
   automatic Advantage. Brawn, soak, WT и encumbrance threshold не меняются.
7. **Horn of Courage** (`horn-of-courage`): Enc 1, rarity 7. Как out-of-turn
   incidental владелец трубит в рог. До конца следующего round он и его allies в
   Medium уменьшают difficulty fear checks на 1, минимум Simple (0). Звук обычно
   слышен до Extreme и, по обстоятельствам, дальше, но механический fear effect
   остаётся Medium.
8. **Mace of Kellos** (`mace-of-kellos`): Melee (Light), damage `Brawn + 4`,
   critical 3, Engaged, Enc 2, rarity 10; Burn 2, Reinforced, Superior.
   Это Divine implement: к каждой spell check с ним добавить 2 Boost. В Attack
   spell эффекты `Fire`, `Close Combat` и `Holy` каждый можно выбрать с нулевым
   increase difficulty; остальные ограничения эффекта сохраняются.
9. **Prismatic Staff** (`prismatic-staff`): Melee (Heavy),
   damage `Brawn + 2`, critical 4, Engaged, Enc 2, rarity 8; Defensive 1.
   Как implement делает первый выбранный `Range` effect бесплатным. Attack spell
   увеличивает свой обычный base damage на 3 и получает Concussive 2, а также
   Disorient с rating, который обычный Disorient spell effect получает от
   `Knowledge (Lore)` по ROT-MAG-10 (или от Forbidden при законном Dark Insight).
   Нельзя трактовать отсутствующую цифру в prose как Disorient 1.
10. **Serpent Dagger** (`serpent-dagger`): Melee (Light),
    damage `Brawn + 2`, critical 2, Engaged, Enc 1, rarity 8; Pierce 1,
    Reinforced, Superior, Unwieldy 4. После успешного hit владелец может потратить
    1 Advantage. Цель немедленно делает Daunting (4) Resilience как out-of-turn
    incidental; при провале получает 8 wounds и 8 strain, без soak. Rival
    преобразует strain в wounds по ROT-CMB-05. Если resistance roll содержит
    3 Threat или 1 Despair, тот же check ставится на начало следующего хода цели.
11. **Shadow Bracers** (`shadow-bracers`): Enc 0, rarity 10. Пока надеты,
    владелец имеет concealment 2: checks, для которых это сокрытие применимо,
    получают 2 Setback. Все melee attacks владельца получают Disorient 3.
    Concealment — не Defense и не входит в cap 4.
12. **Shield of Light** (`shield-of-light`): Melee (Light),
    damage `Brawn + 0`, critical 6, Engaged, Enc 2, rarity 9; Defensive 3,
    Deflection 3, Inaccurate 2, Reinforced, Knockdown. Если wielding-владелец
    получает hit от melee combat check, после полного разрешения атаки защитник/GM
    может потратить из roll атакующего 2 Threat или 1 Despair и назначить
    атакующему Critical Injury `Blinded` без случайного critical roll.
13. **Soulbound Sword** (`soulbound-sword`): Melee (Heavy),
    damage `Brawn + 6`, critical 2, Engaged, Enc 3, rarity 10; Defensive 1,
    Pierce 1, Reinforced, Superior, Unwieldy 3. Пока меч worn или wielded, ST
    владельца уменьшается на 2. Как incidental при wielded-мече владелец может
    принять помощь духа: он и все остальные Engaged персонажи немедленно делают
    Hard (3) fear check, остальные — out-of-turn. Если владелец не роняет меч от
    результата своей проверки, до конца encounter его ranks Melee (Heavy) для
    атак **этим экземпляром** считаются 4. Если собственный rank уже 4+, вместо
    замены rank эти атаки получают 2 Boost. Despair на fear check владельца
    позволяет духу полностью завладеть им до конца encounter или дольше по решению
    GM; действия одержимого определяет GM совместно с игроком.
14. **Staff of Light** (`staff-of-light`): Melee (Heavy),
    damage `Brawn + 2`, critical 4, Engaged, Enc 0, rarity 9; Defensive 2.
    Освещает Short. Как implement позволяет применять Arcana к Heal action,
    делает `Blast` бесплатным и увеличивает обычный base damage Attack spell на 5.
15. **Truelight Lantern** (`truelight-lantern`): Enc 1, rarity 8. Освещает Short.
    В освещённой области Perception checks для обнаружения скрытых дверей,
    иллюзий, спрятанных предметов и сходных деталей upgrade дважды. Это не 2 Boost
    и не decrease difficulty.
16. **Warding Talisman** (`warding-talisman`): Enc 0, rarity 6. Пока надет,
    увеличивает general Defense на 1. Это `Increase`, а не provider: прибавить к
    лучшему применимому provider и другим increases, затем применить cap 4.
17. **Winged Boots** (`winged-boots`): Enc 1, rarity 9; Enc 0, когда надеты.
    Владелец летает и может hover без отдельного maneuver для поддержания высоты,
    но не может подняться выше Medium от земли. Перемещение между диапазонами
    по-прежнему требует maneuvers.

#### Runtime, API, legacy и тесты

- Ввести `MagicItemEffectDef` и `ItemEffectState`, всегда связанные с owned
  instance. Authoritative trigger context содержит roll/damage ID, actor, target,
  range, session/encounter/turn и оставшиеся symbols.
- Команда реакции проверяет ownership, worn/wielded state и timing window.
  Story Points, damage/strain/wounds, symbol allocation, scheduled repeat и event
  log изменяются одной idempotent транзакцией.
- Reference/shop DTO возвращает `price=null`, `purchasable=false`,
  `sellable=false`, а UI — `GM award only`. Shop/sell endpoint отклоняет code с
  `MAGIC_ITEM_NOT_FOR_SALE`.
- Legacy `price=0` заменить на null только у 17 built-in codes. Custom content и
  ownership не менять. Export/import/duplicate сохраняют instance definition
  version; ephemeral encounter effects экспортируются только в versioned encounter
  snapshot, не как постоянный bonus.
- Table-driven seed test фиксирует ровно 17 codes и все stats. По каждому пункту
  нужны positive/negative trigger, range/target, equip cleanup, concurrent/retry и
  exact expiry tests. Отдельно: Forbidden 0; Compass с превышением WT; Deepwood вне
  природы; movement minimum 1; implement exclusivity; poison repeat; Blinded без
  random roll; Soulbound rank 3/4/5 и Despair; Defense cap; flight height.

### ROT-CRAFT-01. Обычное изготовление

#### Базовый процесс

1. До check выбрать конечный `ItemDef`, quantity 1, подходящие многоразовые tools
   и материалы. Обычный skill — Mechanics.
2. Материалы стоят половину обычной цены предмета. Для целочисленной экономики
   применять `ceil(price / 2)`; это фиксированное ProductDecision округление.
   Материалы потребляются независимо от успеха.
3. Базовое время: `1 + rarity` дней. GM может явно заменить его из-за устройства
   предмета/условий; override хранит reason.
4. Difficulty Mechanics check:
   `ceil(rarity / 2)`, где 0 означает Simple, 1 Easy, …, 5 Formidable.
   Ситуативные Boost/Setback/upgrade применяются после base.
5. Success создаёт один item instance; failure не создаёт предмет. Roll и
   распределение symbols authoritative. Повторный resolve не списывает материалы и
   не создаёт второй instance.

`CraftingProject` хранит result/version snapshot, crafter, tools, material cost,
start/end time, difficulty, authoritative roll и выбранные spends. Workflow:
`validate → reserve and consume → roll → allocate → create/close`; после roll
cancel не возвращает материалы. Предмет без цены и 17 именованных magic items
обычным workflow не создаются.

GM может разрешить Survival вместо Mechanics для грубого простого предмета вроде
примитивного копья или ловушки. Такой item получает provenance `RoughSurvival`;
при Despair на любой последующей check, использующей этот предмет, GM может сломать
его и сделать непригодным. Это разрешение GM, не общий alternate skill для любого
рецепта.

#### Полная таблица symbol spends crafting

В пределах одной строки игрок/GM выбирает любой подходящий effect. Повторять можно
только effect, где это прямо указано.

| Cost | Effect |
|---|---|
| 1 Advantage **или** 1 Triumph | сократить время на 1 день, минимум 1; можно повторять **либо** добавить 1 Boost к следующей check тем же skill |
| 2 Advantage **или** 1 Triumph | сохранить материалы, чтобы стоимость следующего сходного craft уменьшилась вдвое **либо** Enc результата −1, минимум 0 **либо** для Limited Ammo 1/одноразового результата создать ещё один идентичный экземпляр; последний вариант можно повторять |
| 3 Advantage **или** 1 Triumph | HP результата +1 **либо** difficulty будущих check создания такого предмета −1, минимум Simple |
| 1 Triumph | добавить Superior **либо** увеличить на 1 один числовой benefit/quality rating, кроме damage, critical, soak и Defense **либо** усилить narrative benefit/добавить GM-approved narrative effect |
| 2 Triumph | добавить одну иную GM-approved item quality; не более одного раза |
| 1 Threat **или** 1 Despair | время +1 день; можно повторять **либо** 1 Setback к следующей crafting check персонажа |
| 2 Threat **или** 1 Despair | Enc результата +1 **либо** докупить материалы ещё на половину первоначальной component cost |
| 3 Threat **или** 1 Despair | для weapon добавить Inaccurate 1 **либо** HP −1, минимум 0 **либо** уничтожить использованные tools |
| 1 Despair | добавить Inferior **либо** при каждом повреждении предмет теряет на одну damage-step больше |
| 2 Despair | немедленная Critical Injury crafter либо равнозначная по тяжести связанная авария по решению GM |

Effects применяются к конкретному crafted instance/snapshot, не мутируют ItemDef.
Скидка следующего сходного craft и future difficulty reduction имеют owner,
`similarItemKey`, charges и expiry по решению GM; свободное клиентское сходство
запрещено. Narrative/quality choices требуют GM confirmation перед финальным
созданием.

Tests: rarity 0/1/2/5/10, нечётные цены, отсутствие tools/funds, failure,
concurrent resolve, каждый spend/cost/repeatability/exclusion, GM confirmation,
RoughSurvival break, inventory/economy/audit и round-trip.

### ROT-CRAFT-MAGIC-01. Редкое GM-guided зачарование

Это guidance, а не обычный recipe catalog:

- только GM решает, возможно ли конкретное зачарование;
- основа должна уже иметь Superior и быть подходящей по форме;
- GM до начала задаёт дополнительные tools/components, время и подходящий magic
  skill;
- даже для незначительного эффекта recommended minimum Hard (3), для большинства
  magic items — Formidable (5);
- success добавляет заранее согласованную magical ability; symbols могут получить
  дополнительные эффекты по согласованию GM;
- этот workflow не клонирует одну из 17 именованных реликвий и не снимает их
  `notForSale`.

API доступен только GM в campaign context: сначала immutable proposal и preview,
затем roll/resolve. UI маркирует результат `GM-created enchantment`, source не
`Official RoT item`. Custom full text принадлежит создателю и не подменяет private
overlay официального контента.

### ROT-ALCH-01. Полный каталог 12 алхимических расходников

Если не сказано иначе, выпить potion или дать его Engaged персонажу — один
Maneuver; расходуется одна dose. Эффекты нескольких доз **одного и того же**
potion не складываются. Разные potion effects сосуществуют, если конкретное
правило не запрещает. Доза и её изготовленные modifiers — instance data.

| Code | Enc | Price | Rarity | Полный эффект |
|---|---:|---:|---:|---|
| `acid-flask` | 0 | 200 | 6 | Action: бросить в точку Short; corrosive atmosphere rating 4 охватывает одну выбранную цель и всех Engaged с ней. Сохраняется до конца encounter, если ветер/открытая среда или иные обстоятельства по решению GM не рассеют раньше. |
| `bottled-courage` | 1 | 25 | 5 | До конца текущей scene/encounter один раз upgrade каждую Discipline check против fear или Coercion. |
| `health-elixir` | 0 | 25 | 3 | Это painkiller: первая доза за сутки лечит 5 wounds, следующие 4, 3, 2, 1; шестая и дальнейшие не лечат. Через одни сутки шкала сбрасывается. Painkiller Specialization прибавляется по собственному правилу. |
| `immunity-elixir` | 1 | 100 | 4 | Немедленно прекращает все действующие mundane poison/toxin effects; GM может исключить magical/extraordinary toxin. До конца scene/encounter Resilience checks против poison/toxin upgrade дважды. |
| `invisibility-potion` | 1 | 1000 | 9 | На 3 rounds персонаж невидим невооружённому глазу, не имеет видимой тени/отражения и получает concealment 4. Шум, запах и физическое тело остаются; магическое обнаружение возможно. |
| `poison` | 0 | 200 | 5 | Доза действует при проглатывании; GM может добавить её в smokebomb. Maneuver наносит её на острое/режущее оружие: первый successful hit, причинивший ≥1 wound, тратит dose и запускает Hard (3) Resilience OOT. При провале цель получает 4 wounds без soak и 1 strain за каждый uncanceled Threat. За 1 Despair GM/владелец poison выбирает Critical Injury **или** повтор этой проверки в начале следующего хода цели. |
| `power-potion` | 1 | 250 | 6 | До конца scene/encounter Brawn +1. Если до применения Brawn уже 5+, вместо роста добавить 2 Boost ко всем Brawn-based checks. После окончания получить 6 strain. Временный Brawn меняет текущие derived soak/load, но не creation WT snapshot. |
| `protective-tonic` | 1 | 125 | 6 | +1 soak на следующие 3 хода пользователя; считать именно его ходы, не rounds. |
| `regeneration-elixir` | 1 | 50 | 4 | Немедленная Simple Resilience: heal 1 wound за Success и 1 strain за Advantage. За Triumph запланировать повтор той же check в начале следующего хода; новый Triumph может снова запланировать только один следующий repeat. |
| `smokebomb-vial` | 0 | 25 | 4 | Maneuver: бросить в точку Short. Smoke screen охватывает одну цель и всех Engaged с ней и даёт concealment 2. Он существует, пока GM не отметит рассеивание; приложение не выдумывает фиксированную длительность. Poison может быть добавлен только отдельной GM-approved командой и расходует обе doses. |
| `speed-potion` | 1 | 200 | 7 | На следующие 3 хода персонаж получает один дополнительный Maneuver в ход и может выполнить максимум 3 вместо 2. После третьего хода получить 6 strain. |
| `stamina-elixir` | 0 | 50 | 3 | Первая доза за сутки heal 5 strain; каждая следующая лечит на 1 меньше, шестая и далее — 0. Через сутки шкала сбрасывается. |

Каждый эффект имеет action cost, target, timing, duration hook, stacking key,
consumption rule и cleanup. Acid/smoke area хранит центр/targets и GM expiry;
poison coating — weapon instance и remaining dose; последствия на окончании
power/speed выполняются даже при incapacitation.

### ROT-ALCH-02. Изготовление, ingredients и symbol spends

Alchemy использует общий project lifecycle ROT-CRAFT-01 со следующими заменами:

- skill `Alchemy`, difficulty `ceil(potion rarity / 2)`;
- базовое время одной batch: `1 + rarity` часов;
- success по умолчанию создаёт одну dose/application;
- обычно нужен Alchemist’s Kit или Lab. Без него GM может повысить difficulty для
  допустимого concoction; приложение не объявляет универсальный hard ban и не
  выбирает величину повышения само;
- цена ingredients `ceil(final price / 2)`, rarity ingredients
  `ceil(potion rarity / 2)`;
- ingredients расходуются при success и failure;
- купить ingredients можно по economy/rarity. Чтобы собрать их, нужен подходящий
  регион, значительная часть дня и Survival difficulty
  `ceil(potion rarity / 2)`; success даёт одну batch. Редкий/опасный компонент
  может потребовать несколько checks/encounter по решению GM.

Полная typed таблица Table 2-17:

| Cost | Effect |
|---|---|
| 1 Advantage **или** 1 Triumph | пользователь дополнительно heal 1 wound или 1 strain, выбор crafter фиксируется на dose **либо** crafter получает 1 Boost к следующей Alchemy check |
| 2 Advantage **или** 1 Triumph | создать ещё одну dose; можно повторять **либо** уменьшить время приготовления вдвое |
| 3 Advantage **или** 1 Triumph | сохранить ingredients на ещё одну batch **либо** увеличить применимую duration на 1 round |
| 1 Triumph | для poison один раз upgrade difficulty checks сопротивления **либо** GM заранее фиксирует усиленный эффект |
| 2 Triumph | добавить эффект одного другого potion строго меньшей rarity |
| 1 Threat **или** 1 Despair | пользователь beneficial potion после получения пользы получает 2 strain **либо** сильный запах добавляет 1 Boost к checks обнаружения potion/poison, включая еду/напиток |
| 2 Threat **или** 1 Despair | onset задержан на 1 минуту или 1 round structured time **либо** докупить ingredients ещё на половину исходной component cost |
| 3 Threat **или** 1 Despair | пользователь beneficial potion после пользы получает 1 wound **либо** duration −1 round, минимум 1; duration «до конца encounter» заменяется на 2 rounds |
| 1 Despair | пользователь disoriented на 2 rounds **либо** должен пройти Average (2) Resilience; при провале организм отвергает potion и основной эффект не возникает |
| 2 Despair | после normal beneficial effect пользователь также получает полный эффект стандартного `poison` |

Только `additional dose` явно repeatable. Комбинированная dose хранит оба effect
snapshots, но остаётся одной расходуемой dose; lower-rarity constraint проверяется
по server catalog. `GM усиленный эффект` и custom potion сначала требуют
GM-confirmed structured proposal, а не free-text client automation.

Модели: `AlchemyRecipeDef`, `IngredientBatch`, `CraftingProject`,
`ConsumableInstanceModifier`. UI разделяет `Gather`, `Buy ingredients`, `Brew`,
`Allocate symbols`, `Consume`; до подтверждения показывает exact price/rarity,
difficulty, time, tool state и конечный эффект.

Acceptance: exact 12-row seed fixture; все effect durations/counters; same-potion
nonstacking; tool override; gather environment; success/failure consumes inputs;
каждая строка symbol table и budget; last-dose concurrency/idempotency;
money/inventory/audit atomicity; instance version round-trip.

### ROT-MITEM-CONTENT. PrivateFull и surface coverage

Для 17 magic items, 12 potions, crafting/alchemy process, обеих symbol-spend tables
и GM enchant guidance private overlay содержит полный собственный RU/EN-парафраз:
условия, costs, timing, targets, ranges, durations, exceptions и consequences.
Coverage validator сравнивает ожидаемые codes/числа/choice count с overlay и
обходит reference, search, shop, character sheet, print, export и Game Table DTO.
`PublicSafe` сохраняет необходимые structured stats, но не получает full prose.

---

## 11. Очистка каталогов, бестиарий, скакуны и конструктор NPC

### Общая политика удаления и совместимости

Во всех задачах этого раздела слово «убрать» означает следующее:

1. запись больше не предлагается при новом создании, покупке, клонировании из
   активного RoT-каталога, поиске с фильтром `Official RoT` и генерации черновика;
2. встроенная строка получает `Retired=true` и правильный `SourceScope`, если на неё
   уже могут ссылаться персонажи, NPC, encounters, exports или audit log;
3. историческая ссылка продолжает открываться по прежнему stable ID/code и
   отображает сохранённый snapshot с пометкой об источнике;
4. seed не удаляет пользовательский контент и не затрагивает custom-запись только
   потому, что у неё совпало имя;
5. физически удалить встроенную строку допустимо лишь в новой чистой базе, если
   доказано отсутствие ссылок; миграция существующей базы всё равно должна быть
   неразрушающей.

`Retired` не является отдельной игровой системой. Core-запись остаётся доступной в
`GenesysCore`, даже если ошибочная копия удалена из `RealmsOfTerrinoth`. Аналогично,
материал иного официального дополнения можно хранить в своём scope, но нельзя
выдавать за содержимое RoT.

### ROT-CLEAN-3.1. Точный список карьер RoT

В активном встроенном RoT-каталоге должно быть ровно восемь карьер:

| Stable code | Canonical name |
|---|---|
| `disciple` | Disciple |
| `envoy` | Envoy |
| `mage` | Mage |
| `primalist` | Primalist |
| `scholar` | Scholar |
| `scoundrel` | Scoundrel |
| `scout` | Scout |
| `warrior` | Warrior |

`Knight` и `Runemaster` карьерами RoT не являются: Runemaster — класс мага из Descent,
а не карьера книги, и навык `Runes` уже есть у Scholar. Исключить только встроенную запись
`System=RealmsOfTerrinoth`; одноимённый custom content и запись из другого
правильного source scope не трогать. Старый RoT-персонаж с `Knight` сохраняет
career snapshot, career-skill flags, стартовые предметы и XP. Он может продолжать
использоваться, но UI показывает `LegacyCareerSourceMismatch`; смена карьеры после
creation по-прежнему запрещрещена общим правилом.

Seed-тест должен сравнивать **полное множество** восьми codes, а не только count.
Reference, creation, import resolver и search используют один фильтр
`ActiveFor(GameSystem.RealmsOfTerrinoth)`.

### ROT-CLEAN-3.2. Gunnery не входит в RoT skill list

`Gunnery` остаётся корректным Core skill, но не входит в active RoT skill catalog.
Для RoT:

- не показывать его при выборе навыков, custom-career base skills, talent choices,
  NPC quick draft и фильтре официальных навыков;
- ни один встроенный RoT career/species/talent/weapon не должен ссылаться на него;
- не преобразовывать его в `Ranged`, `Ranged (Light)` или иной навык: это разные
  значения и автоматическое сопоставление меняет данные;
- старый RoT-персонаж/NPC с Gunnery сохраняет rank и snapshot как
  `LegacySkillSourceMismatch`, но новый rank купить нельзя;
- custom skill с другим stable ID не затрагивать по совпадению display name.

Core creation и Core NPC builder продолжают предлагать Gunnery. Интеграционные
тесты должны проверять обе системы в одном процессе, чтобы фильтр RoT не удалил
Core-контент глобально.

### ROT-CLEAN-3.5. Лишние таланты в RoT PC catalog

Следующие одиннадцать записей исключить из **новой покупки персонажем RoT**:

1. `Rapid Reaction`;
2. `Surgeon`;
3. `Scathing Tirade`;
4. `Scathing Tirade (Improved)`;
5. `Scathing Tirade (Supreme)`;
6. `Just in Time!`;
7. `Indomitable`;
8. `Ruinous Repartee`;
9. `Attuned`;
10. `Counterspell`;
11. `Empowered Casting`.

Это не глобальный blacklist. Если талант принадлежит Core или EPG, оставить его в
соответствующем scope. `Ruinous Repartee` может оставаться в snapshot официального
NPC, но не становится из-за этого покупаемым RoT PC talent. Старые покупки
сохраняются без возврата XP и без автоматической замены. Новая покупка и
повторный rank отклоняются `talent.not_available_in_system`; refund разрешается,
если не нарушает зависимостей, и возвращает фактически записанную цену покупки.

Expected active RoT set после очистки — точный manifest из ROT-TAL-01. Нужны тесты
на каждый из одиннадцати codes во всех scopes, NPC-only исключение, legacy sheet и
отсутствие утечки через global search/import.

### ROT-CLEAN-3.6. Удалить девять adversaries не из RoT

Из active RoT bestiary исключить ровно следующие встроенные записи:

| Canonical name | Новый source scope |
|---|---|
| Farrow’s Guard | Haunted City / legacy supplement |
| City Guard | Haunted City / legacy supplement |
| Coachman | Haunted City / legacy supplement |
| Mavaris Skain, Necromancer | Haunted City / legacy supplement |
| Magistrate Edmin Cawl | Haunted City / legacy supplement |
| Brigand | Haunted City / legacy supplement |
| Brigand Leader | Haunted City / legacy supplement |
| Eliza Farrow | Haunted City / legacy supplement |
| Danne Bulvert | Haunted City / legacy supplement |

Нельзя удалить всех NPC с общими словами `Guard`, `Brigand` или совпадающим
переводом: операция идёт по девяти подтверждённым stable codes/IDs. Их поля и
вложенные attacks/abilities сохраняются для существующих encounters и duplicates,
но `SourceScope != RealmsOfTerrinoth`, `RetiredFromRoT=true`.

### ROT-CLEAN-3.7. Удалить выдуманный Adventuring Pack

`Adventuring Pack` не является отдельным приобретаемым RoT-предметом и не должен
оставаться container/bundle в inventory. Его место в карьерном комплекте занимает
`Traveling Gear`, которое всегда раскладывается на:

- Backpack ×1;
- Bedroll ×1;
- Rope ×1;
- Flint and Steel ×1;
- Torch ×3;
- пустой Waterskin ×1.

Это те же stable ItemDefs, что используются магазином; у каждой строки собственные
quantity, Encumbrance, state и provenance. Bundle не добавляет собственный Enc,
цену или слот сверх дочерних предметов.

Автоматически разложить legacy `Adventuring Pack` можно только если одновременно
доказаны:

- provenance `CareerStartingGear`;
- built-in code и версия ошибочного pack;
- quantity 1;
- стандартное неизменённое состояние;
- отсутствие пользовательских notes/attachments/custom overrides.

В таком случае одна транзакция создаёт точный набор выше с тем же character/audit
origin, удаляет только bundle-instance и не меняет деньги. Если хотя бы одно
условие не доказано, оставить retired read-only instance и предложить GM/player
repair preview. Ручная конверсия должна явно перечислить создаваемые и исчезающие
строки. Custom same-name bundle никогда не раскладывать автоматически.

### ROT-BEST-01. Канонический состав RoT-бестиария

После ROT-CLEAN-3.6 и добавления четырёх скакунов из ROT-MOUNT-NPC-01 active
built-in bestiary должен содержать **ровно 81 профиль**:

```text
86 текущих встроенных профилей
− 9 профилей Haunted City
+ 4 отсутствующих профиля скакунов
= 81 active RoT profiles
```

Expected fixture обязан содержать полный список 81 stable codes. Один count
недостаточен: тест также запрещает неожиданный code и проверяет уникальность.
`Goblin (Official)` переименовать в `Goblin` / `Гоблин`, сохранив прежний stable
code, database ID и alias старого имени для импорта/поиска. Суффикс `(Official)` не
должен попадать в новые snapshots.

Для каждой записи сохранять:

- `System=RealmsOfTerrinoth`, `SourceBook=RealmsOfTerrinoth`, page и stable code;
- kind, шесть characteristics, WT, nullable ST, soak, melee/ranged Defense,
  silhouette;
- group-skill membership для Minion либо ranks для Rival/Nemesis;
- talents, abilities, attacks, qualities, equipment и tags как вложенные typed
  записи со своими стабильными codes;
- final printed soak/Defense без повторного добавления armor;
- safe/full RU/EN presentation согласно §0.3.

Import/seed должен fail-fast, если Minion имеет ST/ranks, Rival имеет ST,
Nemesis не имеет ST, Defense выходит за 0…4, attack неразрешим или вложенный code
дублируется. Исключение допускается только как явно зафиксированная official
special ability, а не silent coercion.

### ROT-MOUNT-NPC-01. Четыре отсутствующих профиля скакунов

Добавить следующие четыре профиля одновременно в bestiary и в связь покупаемого
mount item с NPC profile. Число в `Damage` — уже итоговое значение stat block,
поэтому `Fixed`, а не ещё одно прибавление Brawn.

| Code | Kind | B/A/I/C/W/P | Soak | WT | ST | M/R Def | Sil | Skills | Attacks / abilities |
|---|---|---|---:|---:|---:|---|---:|---|---|
| `beast-of-burden` | Minion | 4/2/1/1/1/1 | 4 | 7 | — | 0/0 | 2 | group Athletics, Resilience | Carrying Capacity 18; Harness; без боевой атаки |
| `riding-beast` | Minion | 4/3/1/1/1/1 | 4 | 5 | — | 0/0 | 2 | group Athletics, Resilience | Carrying Capacity 12; Riding Tack; в стрессовой ситуации GM требует Riding check |
| `flying-mount` | Rival | 3/4/1/2/2/2 | 3 | 12 | — | 1/2 | 2 | Athletics 3, Coordination 3, Discipline 2, Resilience 2, Survival 2 | Carrying Capacity 12; Flyer; Hooves/Talons: Brawl, Fixed 5, Crit 4, Engaged, Knockdown |
| `war-mount` | Rival | 4/3/1/2/3/1 | 4 | 14 | — | 0/0 | 2 | Athletics 3, Brawl 1, Discipline 2, Resilience 3, Survival 2 | Carrying Capacity 13; Riding Tack; Hooves/Claws: Brawl, Fixed 6, Crit 4, Engaged, Knockdown |

Errata обязательна: у `Flying Mount` удалить `Dodge 2`; у `War Mount` не добавлять
придуманный talent. Пустой список talents является значимым и тестируется.

Связанные shop definitions сохраняют:

| Mount item | Price | Rarity |
|---|---:|---:|
| Beast of Burden | 200 | 1 |
| Riding Beast | 400 | 2 |
| Flying Mount | 2000 | 8 |
| War Mount | 1500 | 6 |

Покупка создаёт `MountInstance`, связанный со snapshot profile, а не обычный
безликий gear stack. Один и тот же instance нельзя одновременно назначить двум
персонажам/participants. Saddlebags дают выбранному mount +4 capacity. Barding
даёт mount +2 soak и provider General Defense 1; по умолчанию доступен War Mount,
а для другого mount требуется явный GM override. Снятие equipment пересчитывает
derived values, но не меняет исходный statblock.

### ROT-BEST-CONTENT. Полные тексты встроенных adversaries

Для всех 81 active profiles `PrivateFull` должен содержать полный собственный
RU/EN-парафраз каждой именованной ability, talent fragment, attack, quality,
equipment rule и особого поведения. Непустое имя при пустом description считается
ошибкой coverage. Числа в statblock остаются структурными и одинаковыми в обоих
режимах.

`PublicSafe` отдаёт statblock и короткие безопасные summaries, но не full overlay.
Проверить list/detail/search/duplicate preview, encounter picker, Game Table
snapshot, print cards, JSON export и nested DTO. Duplicate built-in NPC получает
user-owned snapshot с тем контентом, который режим имеет право выдать; переключение
режима не должно раскрыть скрытый private text из ранее прогретого кэша.

---

## 11.1. Полная переработка системы создания NPC по Core/RoT

### ROT-NPC-01. Источник правил и режимы конструктора

Core и RoT не задают XP-бюджет или единственную формулу создания adversary,
аналогичную PC creation. GM напрямую назначает нужный statblock; книга даёт
обязательные правила трёх kinds и рекомендации по уровню опасности. Поэтому
конструктор должен явно разделять три режима:

1. `ManualCoreRoT` — основной и выбранный по умолчанию. GM вводит значения, сервер
   применяет обязательные правила и выдаёт неблокирующие book-guidance warnings.
2. `EpgPackages` — optional режим Expanded Player’s Guide. Его можно показывать
   только после отдельной реализации **полного** EPG package algorithm со всеми
   таблицами и source label `Optional/EPG`. Частично восстановленные формулы не
   допускаются.
3. `AppPreset` — удобный неофициальный генератор Genesys Forge. Он помечен
   `Unofficial`, сначала возвращает полностью редактируемый preview и ничего не
   сохраняет. Только отдельное `Confirm` создаёт NPC.

Текущие `Weak|Standard|Strong|Elite`, role/template/combat-style presets и
`quick-draft` не являются Core/RoT rules. Их разрешено сохранить только как
`AppPreset`; UI/API/docs не должны называть результат «по книге», «валидированным
балансом» или «официальным уровнем силы».

Из rule-backed path удалить выдуманные автоматические формулы и выдачи:

- `3 + power level`, `8 + Brawn`, `12 + Brawn`, `10 + Willpower`;
- `WoundThreshold >= Silhouette × 10`;
- автоматические characteristics по role/power;
- автоматические natural attacks, signature action или spells;
- автоматический `Adversary` по kind/power;
- жёсткое ограничение soak 7 или characteristics 6.

Если эти значения остаются внутри `AppPreset`, preview обязан показать
`authority=AppPreset`, каждую применённую эвристику и дать отредактировать все поля
до сохранения.

### ROT-NPC-02. Структурные результаты валидации

Каждое правило возвращает:

```text
NpcRuleFinding {
  ruleId,
  authority: CoreMandatory | RoTOverride | CoreGuidance | EpgOptional | AppPreset,
  severity: Error | Warning | Info,
  fieldPath,
  messageKey,
  parameters,
  sourceRef
}
```

Только `CoreMandatory` и `RoTOverride` с severity `Error` блокируют save/send/import.
`CoreGuidance`, `EpgOptional` и `AppPreset` не могут превратиться в hard error.
Тексты UI локализуются по `messageKey`; серверный `ruleId` стабилен.

`POST /api/npcs/validate` принимает тот же versioned draft, что save, ничего не
сохраняет и возвращает normalized preview, errors, warnings и derived dice pools.
`POST /api/npcs/` и `PUT /api/npcs/{id}` повторяют ту же validation внутри
транзакции; успешный preview не является разрешением обойти повторную проверку.

### ROT-NPC-03. Обязательная общая модель statblock

Для нового NPC обязательны:

- непустое имя и `System=GenesysCore|RealmsOfTerrinoth`;
- `Kind=Minion|Rival|Nemesis`;
- шесть целых characteristics, каждая `>=1`;
- `WoundThreshold` — целое `>0`;
- `Soak`, `MeleeDefense`, `RangedDefense`, `Silhouette` — целые `>=0`;
- оба Defense — `<=4` по общей errata;
- skills, talents, abilities, attacks и equipment в формах ниже.

Для NPC нет общего PC cap характеристик 5. Значение 6 и выше разрешается, но
`characteristic.unusually_high` является guidance warning. Не обрезать его и не
менять dice pool: для characteristic `C` и skill rank `R` pool строится общим
Core resolver из `max(C,R)` Ability dice и `min(C,R)` upgrades, даже если одно
значение выше 5.

`Role`, `PowerLevel`, `CombatStyle`, `Tactics`, tags и visibility — свойства
приложения, не правила книги. Они могут помогать фильтрации, но не меняют statblock
после сохранения и маркируются `AppMetadata`.

### ROT-NPC-04. Различия Minion, Rival и Nemesis

#### Minion

- `StrainThreshold=null`; отдельный current strain отсутствует.
- Incoming strain становится равными wounds. Добровольно suffer strain нельзя:
  spell, второй maneuver за strain или talent-cost отклоняется до эффекта.
- В definition хранится только множество group skills без ranks.
- Minion разрешено использовать одному либо в MinionGroup. Group rules полностью
  определены в §6.
- Critical Injury не создаёт d100 injury: одиночный Minion выбывает, группа
  применяет правило ROT-MIN-05.

#### Rival

- `StrainThreshold=null`; отдельный current strain отсутствует.
- Incoming strain становится равными wounds. Rival **может** активировать ability,
  cast или второй maneuver, добровольно причиняющий strain; та же величина
  записывается как wounds.
- Skills имеют собственные ranks. Critical Injuries разрешаются обычно.
- Rival всегда отдельный participant; bulk add создаёт независимые instances.

#### Nemesis

- `StrainThreshold` — обязательное целое `>0`; strain и wounds считаются раздельно.
- Skills/ranks, Critical Injuries и voluntary strain работают как у PC, если
  конкретная ability не меняет правило.
- Nemesis всегда отдельный participant.

Смена kind в editor сначала строит preview потерь данных:

- в Minion очистятся ST и individual ranks, но GM должен подтвердить новое
  membership group skills;
- в Rival очистится ST, current strain станет wounds только для active snapshot,
  skills сохранят ranks;
- в Nemesis требуется вручную указать положительный ST;
- Rival/Nemesis → Minion никогда не угадывает group skills из списка рангов.

Без явного подтверждения kind change не сохраняется.

### ROT-NPC-05. Guidance, которое не блокирует сохранение

Показать следующие рекомендации с authority `CoreGuidance`:

| Область | Guidance |
|---|---|
| Minion characteristics | обычно большинство значений 2, слабые 1; одна-две сильные характеристики могут быть 3 |
| Rival characteristics | ключевая характеристика обычно 3, остальные чаще 1–2 |
| Skill ranks Rival | обычно не более 2 в одном skill, но исключения разрешены |
| Characteristics >5 | редкое исключительное значение; warning, не error |
| Minion WT | обычно 3–5 и почти никогда выше 7 |
| Rival WT | обычно 10–15; крупное чудовище может иметь 20–25 |
| Nemesis WT | обычно 10–20; особо крупное существо может иметь больше |
| Nemesis ST | обычно 10–15 и часто ниже WT |
| Defense 2 либо soak 4–5 | заметно повышает опасность |
| Defense 3 либо soak 6–7 | делает NPC очень стойким |
| Soak >7 | warning о крайне высокой стойкости; значение остаётся законным |
| Minion group skills | обычно 2–3 наиболее характерных |
| Слишком много skills | больше 8 — warning о неудобном statblock |
| Talents | Minion обычно 0; Rival обычно 0–1, редко 2; Nemesis примерно 3 |
| Adversary | Rival обычно не выше 1, Nemesis обычно не выше 3; это warning, не auto-grant/cap |
| Attacks/equipment | обычно 1–2 оружия и один значимый armor profile |

Не создавать warning `WT < Silhouette × 10`: официальные profiles нарушают такую
формулу, и Core её не устанавливает. Не создавать hard maximum soak 7, Defense 6
или characteristics 6. Defense имеет только общий errata cap 4.

### ROT-NPC-06. Skills и magic skills

`NpcSkillEntry` хранит `SkillDefId` или owner-visible `CustomSkillId`, stable code,
name snapshot и:

- `rank=null` для Minion group-skill membership;
- `rank` от 1 до 5 для Rival/Nemesis.

Пустое имя, duplicate skill ID и rank неправильного kind — blocking error.
Неизвестный legacy name хранится как unresolved snapshot и блокирует только
механический roll этого skill до repair; весь NPC не удаляется.

RoT builder предлагает RoT skill list без Gunnery. Core builder — Core list.
Official NPC может иметь структурную атаку с printed skill, даже если такого
weapon profile нет в PC shop: statblock является самостоятельным источником.

Magic NPC использует ту же матрицу actions/effects, implement requirements,
difficulty, Lore resolver и strain-cost, что §9. Для Rival cast наносит 2 wounds,
для Nemesis — 2 strain; Minion не может cast из-за voluntary strain, если его
**явная typed special ability** не заменяет эту цену. Чтобы выполнить magic check,
effective rank skill должен быть хотя бы 1. Одно наличие слова «магия» в свободном
description не создаёт spell.

### ROT-NPC-07. Таланты и abilities

NPC не покупает таланты за XP, не строит пирамиду и не обязан выполнять PC
prerequisites. GM напрямую добавляет нужный официальный или custom talent.

`NpcTalentEntry` содержит definition link либо custom snapshot, rank, typed
parameters и полное описание. Ranked talent требует rank `>=1`; unranked не
принимает rank. `Adversary N` хранится как talent code + integer `N`, а не строка,
которую надо парсить. Сервер не выдаёт его автоматически.

`NpcAbilityEntry` содержит stable local code, RU/EN name snapshots, full/safe
description, activation/timing/cost/target/duration и optional executable strategy.
Free-text ability разрешена, но отмечается `ManualResolution`; UI не изображает её
автоматизированной. Два вложенных entry не могут иметь один local code.

Для built-in bestiary все abilities/talents должны быть typed или явно
`ManualResolution` с полным `PrivateFull`-парафразом. `Ruinous Repartee` и иные
NPC-only entries не становятся PC-purchasable.

### ROT-NPC-08. Атаки и качества без парсинга строк

Заменить строковые `Damage`, `Critical`, `RangeBand`, `SkillName` на:

```text
NpcAttackEntry {
  id,
  localCode,
  nameRu,
  nameEn,
  skillRef,
  damageMode: BrawnPlus | Fixed,
  damageValue: integer,
  critical: positive integer | null,
  range: Engaged | Short | Medium | Long | Extreme,
  qualities: [{ qualityDefId, codeSnapshot, rating }],
  sourceWeaponRef?,
  notes,
  presentationSnapshot
}
```

`BrawnPlus` означает `baseDamage = current Brawn + damageValue`; `Fixed` означает
ровно `damageValue`. Клиент никогда не присылает готовый итог вместо выбранного
mode. `critical=null` означает, что обычная трата Advantage на Critical Injury
недоступна. `damageValue` может быть 0; итоговый damage не может быть отрицательным.

Quality resolver проверяет наличие/отсутствие rating, положительный рейтинг,
совместимость и повторяемость по GEN-EQP-QUAL-01. Неизвестное custom quality
разрешается только как owner-defined reference или `ManualResolution`; нельзя
молча принять строку за встроенное качество.

Одна attack обязана иметь непустое имя/code, разрешимый skill, damage mode/value и
range. Names не служат ключами; одинаковые display names возможны у разных attack
modes. Dice preview строится из authoritative NPC characteristic + skill rank
(для Minion — current group rank), а damage/critical разрешаются общей боевой
системой.

Legacy parser выполняется один раз в миграционном repair:

- однозначное `+3` → `BrawnPlus(3)`;
- однозначное целое `8` → `Fixed(8)`;
- `—`/пустой critical → `null`, положительное целое → значение;
- известный range/skill alias → stable reference;
- любое неоднозначное значение сохраняется в `presentationSnapshot`, получает
  `RulesReviewRequired` и не используется для автоматической атаки.

Нельзя выбирать наиболее похожий skill/quality по тексту.

### ROT-NPC-09. Equipment и финальные derived значения

Числа soak/Defense в готовом NPC statblock являются **финальными** и уже учитывают
обычный armor/equipment. Поэтому простой список equipment не прибавляет их второй
раз.

Поддержать два явных режима:

- `FinalStatblock`: GM вводит финальные soak/Defense; equipment — presentation и
  typed abilities без автоматического повторного armor bonus;
- `BuildFromComponents`: GM вводит base soak/Defense providers и выбирает предметы,
  preview показывает вклад каждого, а save материализует один финальный breakdown.

Переключение режима требует preview, чтобы не удвоить armor. Нагрузка для NPC —
необязательный GM tracker и не блокирует statblock validation. Если tracking
включён, применяются общие Enc/overload rules; если выключен, backend не выдумывает
capacity по kind.

### ROT-NPC-10. Minion groups, initiative и optional lone Nemesis

Кнопка `Create group` доступна только Minion и требует source profile, count ≥1,
per-member WT и group skills. Rival/Nemesis count всегда 1. Кнопка `Add N copies`
для них создаёт N отдельных preview rows и после подтверждения — N независимых
participants.

Существующая эвристика `count > 1 => ParticipantType.MinionGroup` удаляется из
Encounter/Game Table factories. Kind берётся из immutable snapshot и проверяется
по CMB-06/MIN-01.

Опциональное правило «одинокий Nemesis действует дважды» допускается только как
GM toggle `LoneNemesisExtraTurn`, по умолчанию `false`:

- при включении Nemesis бросает initiative два раза и получает два NPC slots;
- каждый slot даёт обычный один turn;
- once/round остаётся once на общий round, once/turn — отдельно на каждый turn;
- эффект «до конца следующего хода» заканчивается в конце **первого** его хода
  следующего round;
- toggle не включается автоматически по отсутствию других NPC и не называется
  базовым Core-правилом.

### ROT-NPC-11. UI и безопасный workflow

Новый editor состоит из последовательных секций:

1. system, kind и имя;
2. characteristics;
3. thresholds, soak, Defense, silhouette;
4. skills;
5. talents/abilities;
6. attacks/qualities;
7. equipment;
8. source, visibility, tags и notes;
9. validation summary и итоговый preview.

Ошибки показываются у конкретного `fieldPath`; warnings не блокируют кнопку, но
остаются в preview. При выборе `AppPreset` над всей формой виден постоянный label
`Неофициальная заготовка Genesys Forge`; `Generate` вызывает только preview.
`Confirm and save` отправляет весь отредактированный draft и его version.

Built-in NPC read-only; `Duplicate` создаёт custom copy со snapshot/provenance.
Campaign visibility проверяет ownership/GM membership. Игроку не выдаются hidden
GM notes, unrevealed motivations и private content overlay. Print card и Game
Table используют те же typed values, а не повторный parser presentation-строк.

### ROT-NPC-12. API, persistence и миграция

Сохранить существующие ресурсы `/api/npcs`, но выпустить новую версию DTO до
удаления старых строковых полей. Минимальный набор операций:

- `POST /api/npcs/validate` — manual/preset preview без записи;
- `POST /api/npcs/presets/preview` — только `AppPreset`, всегда
  `isOfficial=false`;
- `POST /api/npcs` — создать подтверждённый versioned draft;
- `PUT /api/npcs/{id}` — полная замена с optimistic concurrency;
- `POST /api/npcs/{id}/duplicate`;
- `POST /api/encounters/{id}/participants/preview` — проверить kind/count/snapshot;
- общие Game Table commands §5–7 для damage/strain/critical/cast.

Каждый response возвращает `validationFindings`, `rulesProfile`,
`contentVersion`, `rowVersion` и normalized typed collections. Старые
`/quick-draft` и `/apply-template` либо становятся явными aliases `AppPreset`, либо
возвращают deprecation metadata; они не должны сразу сохранять результат без
подтверждения.

Неразрушающая миграция выполняется этапами:

1. добавить rule profile/source, validation state, row version и новые typed child
   tables/columns рядом со старыми полями;
2. перенести однозначные skills, talents, abilities, attacks и qualities с
   provenance report;
3. встроенный каталог пересоздать из exact fixture и проверить fail-fast;
4. пользовательские неоднозначные строки сохранить как raw snapshot +
   `RulesReviewRequired`;
5. читать новый формат с fallback на legacy presentation;
6. после отдельного релиза и метрик перестать писать legacy fields; удаление
   колонок в эту задачу не входит.

Legacy kind не выводить из ST, count, имени или talents. Если kind отсутствует либо
противоречит данным, сохранить original snapshot, поставить `NpcKindReviewRequired`
и запретить mechanical send/cast/attack до выбора GM.

Illegal legacy Rival/Nemesis group **не раскладывать автоматически**, потому что
общие wounds, conditions, criticals и уже потраченные turns нельзя канонично
распределить. Сохранить группу как locked snapshot; repair wizard просит GM:

- выбрать количество отдельных NPC;
- распределить current wounds, criticals, conditions и turn state по каждому;
- просмотреть создаваемые participant IDs;
- подтвердить одну audited atomic conversion.

До подтверждения можно читать/печатать старую запись, но нельзя применять к ней
новые механические команды.

### ROT-NPC-13. Полная приёмочная матрица

Domain table tests:

- каждая характеристика 1, 5, 6 и >6; Defense 0/4/5; WT/ST/soak/silhouette bounds;
- точные различия Minion/Rival/Nemesis, voluntary/incoming strain и critical;
- group skill membership и динамический rank для counts 1/2/6/7;
- Rival/Nemesis skills 1/5, duplicate/0/6;
- typed BrawnPlus/Fixed attacks, nullable crit, все ranges и quality ratings;
- no pyramid/XP/prerequisite для NPC talents, structured Adversary;
- Core/RoT skill scopes и magic costs для всех kinds;
- все guidance thresholds по обе стороны границы; warning никогда не блокирует.

API/integration:

- validate не пишет БД; preset preview не пишет БД; confirm пишет один раз;
- owner/campaign visibility, built-in read-only/duplicate;
- optimistic concurrency и idempotency;
- tampered normalized values пересчитываются сервером;
- encounter/Game Table отвергает Rival/Nemesis group и принимает Minion;
- export/import/duplicate/print сохраняют typed data и raw legacy snapshot;
- миграция однозначных/неоднозначных атак и locked illegal group;
- `PublicSafe`/`PrivateFull` projection и отсутствие cache leakage.

Frontend:

- переключение kind с loss preview;
- error/warning/authority labels;
- Minion membership вместо rank fields;
- attack editor с явным damage mode;
- unofficial preset banner и отдельный confirm;
- no group control для Rival/Nemesis;
- repair wizard legacy attack/kind/group;
- keyboard-accessible validation summary и локализованные reason codes.

Seed/content:

- точные множества девяти careers, RoT skills без Gunnery, 112 talents и 81
  adversaries;
- девять Haunted City rows не входят в active RoT;
- Goblin rename сохраняет ID/alias;
- четыре mount profiles и errata Flying Mount;
- повторный seed не создаёт дубли и не меняет custom rows.

**Документация:** обновить `docs/domain-model.md`, `docs/database.md`,
`docs/api.md`, `docs/current-state.md`, user guide и reference source registry.
**Миграция обязательна**, потому что текущие NPC attacks/skills/talents/abilities и
validation state недостаточно типизированы.
