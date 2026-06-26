# Обновление ростера архетипов/карьер + RU-имена при выборе (archetype-roster-refresh)

- **Roadmap:** подготовка к U-12 (структурные модели видов) / U-13 (карьеры) — частичный шаг (данные + отображение), без структурных ArchetypeAbilityDef/CareerStartingGear.
- **Ветка:** `feature/archetype-roster-refresh`
- **Базовая ветка:** `feature/u11-rules-tables-search` (стек поверх PR #46 — общие правки SeedData/AppDbContext/миграций)
- **PR:** [#49](https://github.com/Giviruk/GenesysForgeClaude/pull/49) (re-land в master — [#51](https://github.com/Giviruk/GenesysForgeClaude/pull/51))
- **Статус:** ✅ Done

## Контекст

Пользователь обновил `_books/genesys_rot_core_archetypes_ru.csv` (4 Core + 14 RoT видов, детальные виды Терринота)
и `genesys_rot_core_careers_ru.csv`. Нужно: отразить новый список видов, показывать RU-названия архетипов/карьер
при создании персонажа, актуализировать карьеры.

Решения (с пользователем):
- **Виды:** заменить — показывать только новый каталог; старые built-in виды (Эльф Латари, Дворф, Орк, Гном,
  Котолюд, The Laborer/Intellectual/Aristocrat) скрываются из выбора (Retired), но остаются в БД ради FK.
- **Карьеры:** добавить только фэнтези/магию: Рыцарь (RoT), Волшебник/Друид/Жрец (Core). 8 ролевых Core и 9 RoT
  уже актуальны. Sci-fi/модерн (Капитан/Пилот/Хакер/Безумный учёный) пропущены.

Стартовый инвентарь — это **U-13** (CareerStartingGear), здесь не реализуется (в CSV колонка «Стартовое снаряжение RU» есть как данные).

## План выполнения

- [x] `ArchetypeDef.Retired` + миграция `20260626134851_AddArchetypeRetired` (AddColumn, non-destructive)
- [x] Генератор `_books/gen-archetypes-catalog.mjs` → embedded `SeedContent/archetypes.catalog.json` (18 видов)
- [x] `ArchetypeCatalog.Load()`; csproj embed
- [x] SeedData: архетипы из каталога, upsert `SeedOrUpdateArchetypes` (виды вне каталога → Retired); удалён инлайн CoreArchetypes/TerrinothSpecies/ArchetypeRu
- [x] 4 карьеры: Рыцарь (RoT), Волшебник/Друид/Жрец (Core) — навыки сверены с засиженными скиллами системы
- [x] `GetReferenceHandler`: фильтр `!Retired`, сортировка по NameRu; карьеры по NameRu
- [x] RU-имена в UI: дропдауны создания (CharactersPage), описание архетипа, RU-чипы карьерных навыков; список персонажей и кампаний (NameRu). Экспорт оставлен на EN (контракт импорта)
- [x] Тесты: SeedDataTests (каталог/retire/резолв навыков), Api 162 / Domain 65 зелёные; front 72
- [x] docs/database.md обновлён
- [x] PR открыт — #49

## Что осталось / блокеры

- Требуется применить миграцию + перезапуск backend (upsert архетипов отрабатывает на старте).

## Заметки / решения

- Способности/стартовые навыки видов (CSV «Способности RU», «Стартовые навыки RU») сложены в `SafeDescription`
  (структурно — это U-12). Видно при выборе вида.
- Knight → RoT (нужны Melee Light/Heavy, Riding); Mage/Druid/Priest → Core (Arcana/Primal/Divine есть в Core).
