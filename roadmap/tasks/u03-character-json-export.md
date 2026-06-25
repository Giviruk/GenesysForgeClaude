# Экспорт/импорт персонажа в JSON (u03-character-json-export)

- **Roadmap:** U-03 — Экспорт/импорт персонажа в JSON (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u03-character-json-export`
- **Базовая ветка:** `master` (U-02/PR #31 слит, открытых PR нет — стека нет)
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

Дать пользователю выгрузить персонажа в JSON и загрузить обратно (бэкап, перенос между
аккаунтами, обмен с мастером). Формат `genesysforge.character.v1` (см. GF-002 в
genesysforge_todo_plan.md).

Решения пользователя:
- Custom-контент: **resolve или unresolved** — экспорт пишет Code/Name; импорт мапит built-in по
  Code (fallback System+Name), custom — по Name в scope владельца; неразрешённый
  навык/талант/предмет пропускается с warning; неразрешённый архетип/карьера блокирует импорт.
- **Серверный preview** — `POST /api/characters/import/preview`.

Факты из кода:
- `HeroicAbilityDef` — без `System` (RoT-only); матчим по Code/Name.
- `ArchetypeDef`/`CareerDef` — без `OwnerUserId` (только built-in).
- JSON: `JsonStringEnumConverter` camelCase, регистронезависимый на чтении — enum'ы round-trip строками.
- Загрузка персонажа со связями — `db.GetOwnedAsync` (CharacterLoader). Заметки — отдельная сущность.

## План выполнения

- [x] DTO формата: `CharacterExportDto` (+ sub-records skill/talent/item/note) + константа версии
- [x] DTO результата: `ImportPreviewDto`, `ImportCharacterResult`
- [x] `Common/CharacterImporter` — общий резолвер (build Character + notes + warnings) для import/preview
- [x] Export: `ExportCharacterQuery` + handler (GET /export)
- [x] Import: `ImportCharacterCommand` + handler (POST /import) — всегда создаёт нового
- [x] Preview: `PreviewImportCharacterQuery` + handler (POST /import/preview)
- [x] DI-регистрация трёх handler'ов
- [x] Endpoints в `CharacterEndpoints.cs`
- [x] Frontend: `client.ts` методы export/import/preview; кнопки в SheetPage (Экспорт) и
      CharactersPage (Импорт с preview-модалкой)
- [x] Тесты xUnit (7): export valid; round-trip creates new; invalid format → 400; unknown archetype
      → 400; unknown skill → warning+skip; preview без создания; чужого экспортировать нельзя
- [x] Frontend lint/test/build зелёные (49); backend full suite зелёный (62+120)
- [x] `docs/api.md` + current-state + feature-roadmap обновлены
- [ ] Статус в `unified-roadmap.md` → done после merge
- [ ] PR

## Что осталось / блокеры

(заполняется по ходу)

## Заметки / решения

- Не экспортируем OwnerUserId и internal id; ключ — Code/Name.
- Импорт не перезаписывает существующего — всегда новый персонаж.
- Derived stats не импортируются (пересчитываются SheetBuilder).
- Copyright: формат содержит только Code/Name/числа, без book text.
