# Структурные свойства предметов и эффекты заклинаний (u10-item-qualities)

- **Roadmap:** U-10 — Структурные свойства предметов (GF-005) (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u10-item-qualities`
- **Базовая ветка:** `master`
- **PR:** [#43](https://github.com/Giviruk/GenesysForgeClaude/pull/43) (base — master)
- **Статус:** 🚧 In progress

## Контекст

Свойства предметов хранятся строкой `ItemDef.Properties` («Точное 1, Оборонительное 2»). Фронт уже
парсит её для тултипов/фильтра (`data/itemQualities.ts`, `PropertyTags`, мерж `feature/quality-tooltips`).
GF-005: добавить backend-справочник `QualityDef` (94 качества из CSV) + связь `ItemQualityValue`
(предмет↔качество+рейтинг), миграцию-бэкфилл строк `Properties` встроенных предметов, отдачу через
reference API и селектор свойств в форме кастом-предмета.

**Решения (по ответам пользователя):** полный GF-005; seed-описания берём из CSV как есть.
- Источник: `_books/_qualities/genesys_rot_item_and_spell_qualities.csv` (94) → embedded
  `SeedContent/qualities.catalog.json` (как items/talents), скрипт `_books/_qualities/gen-qualities-catalog.mjs`.
- `QualityDef : IContentDef` → dual-mode (PublicSafe чистит Description, остаётся SafeDescription).
- `Properties` строкой сохраняем (fallback); бэкфилл `ItemQualityValue` идемпотентный.
- `Assumption`: CSV не делит item/spell — `Kind` по умолчанию ItemQuality; `IsActive`/`Category` из CSV.

## План выполнения

- [x] Скрипт CSV→`qualities.catalog.json` (80 качеств); csproj embed
- [x] `QualityDef` (+ `QualityKind`), `ItemQualityValue`, `ItemDef.Qualities` nav; `ItemPropertyParser` (Domain)
- [x] DbSets + EF config + миграция `AddItemQualities`
- [x] `QualityCatalog.Load`; SeedData: seed качеств (ProjectContent, dual-mode) + бэкфилл `ItemQualityValue` из `Properties` (идемпотентно, алиасы)
- [x] Reference API: `QualityDto`, `ReferenceResponse.Qualities`, `ItemDefDto.Qualities` (+ Include)
- [x] F: типы (`Quality`/`ItemQualityRef`) + reference; селектор свойств+рейтинг в форме кастом-предмета (`CustomTab`)
- [x] Тесты: xUnit `ItemPropertyParserTests` (3) + `QualityReferenceTests` (4); `dotnet test` 65 dom + 151 api; frontend lint/build + 67 vitest
- [x] Миграция + `docs/database.md`; статус roadmap
- [x] PR открыт — #43 (base — master)

## Что осталось / блокеры

- Кастом-предметы: селектор в форме собирает строку `Properties` (фронт парсит её одинаково со
  встроенными). Структурные `ItemQualityValue` создаются бэкфиллом только для встроенных предметов;
  расширить структурное хранение на кастом — возможный follow-up (не входит в DoD).
- Тултипы/фильтр свойств на фронте уже работали (мерж `feature/quality-tooltips`) на локальном
  `data/itemQualities.ts`; переключение их на backend-каталог `reference.qualities` — опц. follow-up.
- Браузерная проверка селектора — опционально (нужен auth + создание кастом-оружия).

## Заметки / решения

- CSV «item_and_spell_qualities» содержит и свойства предметов, и доп.эффекты заклинаний (повторы
  Range/Additional Target/Empowered). Дедуп по `Code` → 80 каноничных качеств; `Kind`=ItemQuality.
- Бэкфилл сопоставляет по нормализованному имени (RU/EN, ё→е, без рейтинга) + 3 алиаса написания
  («Оглушающее»→«Оглушение» и т.п.). Несопоставленные токены остаются в строке `Properties`.
- `appendProperty` (CustomTab) — мелкая UI-склейка строки, отдельным Vitest не покрывал.
