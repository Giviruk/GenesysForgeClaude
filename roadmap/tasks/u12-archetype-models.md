# Структурные модели архетипов/видов (u12-archetype-models)

- **Roadmap:** U-12 — структурные модели видов (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-006 / Аудит §3.
- **Ветка:** `feature/u12-archetype-models`
- **Базовая ветка:** `master` (стек пуст — U-01…U-11 + archetype-roster-refresh слиты).
- **PR:** [#52](https://github.com/Giviruk/GenesysForgeClaude/pull/52)
- **Статус:** 🚧 In progress

## Контекст

После `archetype-roster-refresh` (PR #49/#51) видовые способности и стартовые навыки лежали свободным текстом
в `ArchetypeDef.SafeDescription`. U-12 выносит их в данные:
- `ArchetypeAbilityDef` — способности вида (отображение + тег `AutomationKind`; исполнение эффектов = U-18).
- `ArchetypeStartingSkill` — стартовые навыки; фиксированные применяются авто при создании, выборы
  («1 ранг в двух разных некарьерных навыках») закрываются пикером в форме создания.

Решения с пользователем: (1) способности структурно + AutomationKind сейчас; (2) полный пикер выборов при создании.
Источник — `genesys_rot_core_archetypes_ru.csv` (колонки «Способности RU», «Стартовые навыки RU»);
паттерн как U-10/U-11: генератор `.mjs` → embedded JSON → каталог-loader → идемпотентный upsert.

## План выполнения

- [x] Domain: `ArchetypeAbilityDef`, `ArchetypeStartingSkill`, enum `ArchetypeAbilityAutomationKind`; nav-коллекции в `ArchetypeDef`.
- [x] Генератор `gen-archetypes-catalog.mjs`: парсинг способностей (несколько в ячейке) и стартовых навыков
      (фикс/`;`/выбор), нормализация RU→EN-канон навыков (алиасы Выдержка→Discipline, Вера→Divine и т.п.); перегенерация JSON.
- [x] `ArchetypeCatalog.Load()` маппит вложенные abilities/startingSkills.
- [x] `SeedData.SeedOrUpdateArchetypes`: полная замена дочерних коллекций на upsert (идемпотентно).
- [x] `AppDbContext` + `IAppDbContext`: DbSet'ы + конфиг (FK cascade, индекс по ArchetypeId); миграция `AddArchetypeAbilitiesAndStartingSkills`.
- [x] `ArchetypeDto` (+ `ArchetypeAbilityDto`/`ArchetypeStartingSkillDto`) и `Mappers.ToDto`; `GetReferenceHandler` материализует с Include.
- [x] `CreateCharacterRequest.ArchetypeSkillChoices`; `CreateCharacterHandler`: авто-применение фиксированных навыков (слияние рангов), валидация и применение выборов.
- [x] Frontend: типы, `createCharacter` клиент, `CharactersPage` — показ способностей/навыков, пикер выбора, блокировка submit.
- [x] Тесты: SeedData (структура/идемпотентность/восстановление), CreateCharacter (фикс/выбор/валидация); Vitest CharactersPage. Api 170 / Domain 65 / front 74 зелёные.
- [x] docs/database.md, docs/domain-model.md обновлены.
- [x] PR открыт — #52.

## Что осталось / блокеры

- Требуется применить миграцию + перезапуск backend (upsert архетипов отрабатывает на старте).

## Заметки / решения

- `SkillName` хранится EN-каноном (как `CareerSkillNames`), чтобы матчиться к `SkillDef.Name` при создании.
- Несколько способностей в одной CSV-ячейке режутся по границе «… . Название:».
- `AutomationKind` сейчас всегда `Manual` (исполнение эффектов — U-18); поле заведено под будущий движок.
- Гранты карьерного статуса навыка видом (напр. «получает Знание (запретное) как карьерный») остаются в тексте
  способности — это не моделируется здесь (за рамками U-12).
- Стартовое снаряжение карьер — U-13 (в CSV есть как данные, здесь не реализуется).
