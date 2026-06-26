# Справочные таблицы правил + глобальный поиск (u11-rules-tables-search)

- **Roadmap:** U-11 — Справочные таблицы правил + глобальный поиск (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u11-rules-tables-search`
- **Базовая ветка:** `master` (стек пуст — все нижестоящие U-01…U-10 слиты)
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

Сейчас `/api/reference/{system}` отдаёт только контент (навыки/таланты/предметы/...), а правила-таблицы
(сложности, траты символов, дистанции, критические ранения) нигде не структурированы. Мастеру за столом
нужен быстрый доступ к этим таблицам + глобальный поиск.

Решения (с пользователем): **(1)** таблицы хранятся в БД (seed-сущность, как QualityDef);
**(2)** поиск глобальный — по справочнику правил И по контенту/сущностям, не только по таблицам.

Источники данных (RU-парафразы, не текст книг — copyright-safe):
- `_books/genesys_critical_injuries_for_claude.csv` (крит-ранения d100, 29 строк)
- `_books/genesys_range_bands_for_claude.csv` (range bands, 16 строк)
- `_books/genesys_symbol_spends_for_claude.csv` (траты Advantage/Threat/Triumph/Despair, 68 строк)
- ладдер сложностей (Simple…Formidable, 0–5 кубов) — захардкожен в генераторе (стандартная механика).

Паттерн повторяет U-10 (QualityDef): генератор `_books/gen-rules-catalog.mjs` → embedded
`SeedContent/rules.catalog.json` → `RuleCatalog.Load()` → идемпотентный `SeedMissing`.

## План выполнения

- [x] Domain: `RuleTableEntry` + enum `RuleTableKind` (CriticalInjury/RangeBand/SymbolSpend/Difficulty)
- [x] Infra: `RuleCatalog.Load()` из embedded JSON; регистрация DbSet в AppDbContext + IAppDbContext; конфиг сущности
- [x] Генератор `_books/gen-rules-catalog.mjs` → `SeedContent/rules.catalog.json` (embedded resource в .csproj) — 119 строк
- [x] SeedData: добавить rules в `Apply()` (SeedMissing по Code) — системо-независимо
- [x] Миграция `20260626091551_AddRuleTables` + обновлён `docs/database.md`
- [x] Application: `GetRulesHandler` (`/api/reference/rules`) и `GlobalSearchHandler` (rules + контент системы + NPC/персонажи)
- [x] API: `/api/reference/rules` (в ReferenceEndpoints), `SearchEndpoints` (`GET /api/search?q=&system=`)
- [x] Frontend: типы, клиент (`api.rules`/`api.search`), область `reference` в роутере/навигации, `ReferencePage` (таблицы) + глобальный поиск
- [x] Тесты: xUnit (RuleReferenceTests: 5) + Vitest (ReferencePage + router) — Api 157 / Domain 65 / front 69 зелёные
- [x] Статус в `unified-roadmap.md` обновлён → 🚧 (PR #N)
- [ ] PR открыт

## Что осталось / блокеры

(заполняется по ходу)

## Заметки / решения

- Таблицы системо-независимы (как QualityDef): dedup по `Code`, без `System`.
- Поиск — серверный `Contains` по денормализованному `SearchText` (lowercased), без FTS (SQLite/PG обе ок).
- `Difficulty` ладдер — хардкод в генераторе: это базовая механика (число кубов), не текст книги.
