# GenesysForge — справочные материалы (`_books/`)

Структурированный индекс исходников Genesys/RoT, лежащих в `_books/`, чтобы при реализации
задач из [unified-roadmap.md](unified-roadmap.md) **не искать заново**, а сразу брать готовый источник.

> ⚖️ Напоминание (см. todo-plan §2.1): в **public-safe** часть нельзя класть дословный текст книг.
> Из CSV/PDF берём структуру, числа, EN/RU-названия, короткий парафраз и ссылку на источник.
> Полные описания — только в `private-content/*.ru.json`.

---

## 1. Что уже есть в `_books/`

### Книги (PDF — источники истины, не для public-данных)
| Файл | Размер | Содержит | Используется для |
|---|---|---|---|
| `Genesys Corebook.pdf` | 63 MB | Genesys CRB (полный) | таблицы правил (U-11), крит-таблица (U-23), range bands |
| `Genesys дополнительные главы.pdf` | 7 MB | EPG / доп. главы | таланты EPG, Power Level, magic Mask/Predict/Transform |
| `Королевство  Терринот.pdf` | 130 MB | Realms of Terrinoth (RU) | **бестиарий RoT (U-19)**, виды/карьеры, героики |
| `talants.pdf` | 3.4 MB | таблицы талантов (скан) | сверка талантов; см. `_img/talants_p*.png` (45 страниц) |

### Готовые структурированные данные (CSV/JSON — основной рабочий материал)
| Файл | Строк | Заголовки/ключи | Питает задачу |
|---|---|---|---|
| `_qualities/genesys_rot_item_and_spell_qualities.csv` | 94 | Название RU/EN; Тип; Есть рейтинг; Базовая трата; Категория; Описание RU; Ограничения; Источник; Страница | **U-10** (QualityDef seed), крафт-справка |
| `_magic/genesys_magic_full_effects_corrected.csv` | 88 | Группа; Действие RU/EN; Тип записи; Название RU/EN; Базовая/итоговая сложность; Навыки Core/Terrinoth; Расход успехов/преимуществ/угроз/триумфа/очка сюжета; Описание; Источник; Страница | сверка магии (уже в seed); **U-11** (таблицы трат символов) |
| `_heroic_abilities/rot_heroic_abilities_with_upgrades.csv` | 33 | Тип записи; Название RU/EN; Уровень эффекта; Стоимость/требование; Активация; Длительность; Частота; Эффект; Источник; Страница | героики (уже в seed `heroics.catalog.json`); **U-18** (automation metadata) |
| `_inventory/genesys_inventory_filtered_any_fantasy.csv` | 170 | Источник; Страница; Категория; Тип; Название RU/EN; Навык RU/EN; Урон; Крит; Дистанция RU/EN; Защита; Поглощение; Нагрузка; Точки крепления; Цена; Редкость; Свойства RU/EN; Описание | предметы (уже в `items.catalog.json`); **U-10** (привязка свойств) |
| `genesys_core_talents_all_fantasy_with_desc.csv` | 57 | таланты Core+Fantasy с описанием | таланты (в `talents.catalog.json`) |
| `terrinoth_talents.csv` | 60 | таланты RoT | таланты RoT |
| `expanded.csv` | 4 | таланты EPG | маркировка EPG (U-аудит §3.3) |
| `_catalog.json` | — | нормализованный каталог талантов (code/name/tier/ranked/setting/wt/st/.../desc) | промежуточный формат талантов |
| `_talents_norm.json` / `_parsed.json` / `_talent_summary.txt` | — | нормализация/парсинг талантов | отладка пайплайна талантов |
| `_to_translate.json` / `_translations.json` | — | словарь переводов | RU-локализация контента |

### Скрипты-генераторы (как CSV → catalog.json)
| Файл | Делает |
|---|---|
| `_inventory/gen-items-catalog.mjs` | CSV снаряжения → `SeedContent/items.catalog.json` |
| `_heroic_abilities/gen-heroics-catalog.mjs` | CSV героик → `SeedContent/heroics.catalog.json` |

### Изображения
- `_img/talants_p1.png … talants_p45.png` — постранично таблицы талантов (для ручной сверки).

### Добавлено по запросу §3 (2026-06-25) — закрывает все недостающие источники
| Файл | Строк | Формат / ключевые колонки | Питает задачу |
|---|---|---|---|
| `genesys_rot_core_careers_ru.csv` | 25 | `;` · Источник;Страница;Сеттинг;Тип;EN;RU;Карьерные навыки EN/RU;**Стартовые ранги**;**Стартовое снаряжение RU**;Описание;Заметки | **U-13** (карьеры + стартовое снаряжение RoT заполнено) |
| `genesys_rot_core_careers_ru_comma.csv` | 25 | то же, разделитель `,` | альтернатива для comma-парсеров |
| `genesys_rot_core_archetypes_ru.csv` / `.json` | 18 | EN/RU;6 характеристик;**Порог ран/усталости (формула)**;Стартовый XP;Стартовые навыки;Способности RU | **U-12** (архетипы/виды) |
| `genesys_archetype_abilities_for_claude.csv` | 99 | `;` · полностью разложенные способности: `ability_kind;trigger;activation;frequency;cost;mechanical_effect_json;automation_kind;applies_during_creation;skill_*;choice_group` | **U-12** (видовые способности как данные) — основной источник |
| `genesys_critical_injuries_for_claude.csv` | 29 | `roll_min/max;severity;difficulty;name;effect;duration;persistent_injury;healable;mechanical_tags;source_page` | **U-23** (d100 крит-таблица) |
| `genesys_range_bands_for_claude.csv` | 16 | `code;name;order;maneuvers_required;can_melee;default_ranged_attack_difficulty;movement_rule;automation_hint` | **U-11** (range bands) |
| `genesys_symbol_spends_for_claude.csv` | 68 | `situation;polarity;advantage_min;threat_min;triumph_min;despair_min;actor;target;effect;mechanical_tags;automation_hint` | **U-11** (траты Advantage/Threat/Triumph/Despair) |
| `genesys_crafting_modification_rules_for_claude.csv` | 105 | `entry_type;category;applies_to;check_skill;difficulty_rule;time_rule;material_cost_rule;effect;*_cost;automation_hint` | **U-11** (крафт/модификации) |
| `_adversaries/genesys_fantasy_adversaries_ru.json` | 206 существ | nested: characteristics, skills_en/ru, **weapons** (damage/crit/range/qualities), abilities, `official`/`homebrew`, `source_url` | **U-19** (бестиарий) — предпочтительный источник (богаче CSV) |
| `_adversaries/genesys_fantasy_adversaries_ru.csv` / `_comma.csv` | 206 | плоская версия того же | альтернатива |
| `_adversaries/export_report.json` | — | отчёт извлечения (445 всего → 206 fantasy, 7 файлов-источников) | трассировка происхождения |
| `_npc/genesys_adversary_creation_rules_for_claude.md` / `.json` | — | LLM-ready спецификация Minion/Rival/Nemesis (профиль, пороги, правила баланса) | **U-15/U-16** (валидация + генератор) |

---

## 2. Карта «материал → задача»

| Задача | Готовый материал | Действие |
|---|---|---|
| **U-10** Структурные свойства | `_qualities/*.csv` (94, с рейтингом/тратой/категорией) | написать `gen-qualities-catalog.mjs` → seed `QualityDef`; распарсить строки `ItemDef.Properties` |
| **U-11** Таблицы правил | `_magic/*.csv` (колонки «Расход …»), `Genesys Corebook.pdf` | вынести траты символов из CSV; крит-таблицу/сложности/range bands — парафраз из CRB |
| **U-12** Архетипы/виды | `genesys_archetype_abilities_for_claude.csv` + `genesys_rot_core_archetypes_ru.json` | seed `ArchetypeAbilityDef`/`ArchetypeStartingSkill` из готового JSON |
| **U-13** Карьеры (стартовое снаряжение) | ✅ `genesys_rot_core_careers_ru.csv` (RoT-снаряжение заполнено) | seed `CareerStartingGear` из колонки «Стартовое снаряжение RU» |
| **U-15/U-16** NPC валидация/генератор | `_npc/genesys_adversary_creation_rules_for_claude.json` | правила баланса Minion/Rival/Nemesis как данные |
| **U-18** Автоматизация героик | `_heroic_abilities/*.csv` (Активация/Длительность/Частота) | добавить automation-метаданные к `HeroicAbilityDef` |
| **U-19** Бестиарий RoT | ✅ `_adversaries/genesys_fantasy_adversaries_ru.json` (206 существ) | `gen-adversaries-catalog.mjs` → seed-NPC; см. copyright-заметку §3 |
| **U-23** Криты | ✅ `genesys_critical_injuries_for_claude.csv` (d100) | seed таблицы крит-ранений |

---

## 3. Статус материалов — всё закрыто ✅ (2026-06-25)

Все 7 пунктов запроса добавлены пользователем в `_books/`:

| # | Запрос | Статус |
|---|---|---|
| 1 | Карьеры + стартовое снаряжение | ✅ `genesys_rot_core_careers_ru.csv` (RoT-снаряжение заполнено) |
| 2 | Бестиарий RoT (структурно) | ✅ `_adversaries/*.json` — 206 fantasy-существ |
| 3 | Таблица крит-ранений (d100) | ✅ `genesys_critical_injuries_for_claude.csv` |
| 4 | Траты символов | ✅ `genesys_symbol_spends_for_claude.csv` (68 строк по ситуациям) |
| 5 | Range bands | ✅ `genesys_range_bands_for_claude.csv` |
| 6 | Крафт/модификации | ✅ `genesys_crafting_modification_rules_for_claude.csv` |
| 7 | Видовые способности | ✅ `genesys_archetype_abilities_for_claude.csv` (99 строк, разложены) |

Бонусом: `_npc/genesys_adversary_creation_rules_for_claude.{md,json}` (правила баланса NPC).

### ⚠️ Copyright-заметка по бестиарию (важно для PublicSafe)
`_adversaries/` содержит **89 официальных** stat-блоков (86 из Realms of Terrinoth + 3 GCRB)
и **117 homebrew** (community «Creature Catalogue», автор Direach, по `source_url`).
В JSON хранятся **полные описания способностей (RU и EN-оригинал)**.
При seeding соблюдать существующий private/public pipeline:
- public-safe: только `name_ru`/`name_en`, числа, короткий парафраз, `Source`;
- полные описания (особенно EN-оригинал из книг) — в `private-content`, не в public-образ;
- homebrew-существа — сохранить атрибуцию (`source_owner`, `source_url`).

---

## 4. Рекомендации по организации (на будущее)

- **Единый формат пайплайна** уже сложился: `_books/<тема>/<csv>` + `gen-*-catalog.mjs` → `backend/.../SeedContent/*.catalog.json` (embedded resource). Держать новые источники в том же виде.
- Завести `_books/_index.md` (или ссылаться на этот файл) — единая точка «что где лежит».
- Для каждой новой CSV-выгрузки сохранять колонку **Источник + Страница** — это и copyright-safe ссылка, и поле `Source` в seed.
- Крупные PDF (`Королевство Терринот.pdf` 130 MB, `Genesys Corebook.pdf` 63 MB) — кандидаты на `.gitignore`/Git LFS, если ещё не вынесены; рабочие данные держать в CSV/JSON.
