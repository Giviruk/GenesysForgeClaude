# U-19 · Преднаполненный бестиарий Terrinoth

**Статус:** 🚧 In progress · ветка `feature/u19-terrinoth-bestiary` (от master) · PR #62
**Roadmap:** [unified-roadmap.md](../unified-roadmap.md) U-19 · Источник: Аудит §3.2/§5

## Зачем
Мастеру негде взять готовые стат-блоки существ — каждый NPC забивается руками. Нужна
встроенная библиотека существ Realms of Terrinoth: read-only для всех, клонируется в свою
библиотеку и добавляется в encounter / на Game Table.

## Что сделано

### Модель и доступ
- `Npc.OwnerUserId` → `Guid?` (nullable), новое поле `Npc.IsBuiltIn`. Миграция `AddBuiltInNpcs`
  (nullable owner + столбец/индекс `IsBuiltIn`).
- Видимость: `NpcMapper.CanViewAsync` → `IsBuiltIn` виден всем; `GetNpcsHandler` отдаёт встроенных
  в списке любому пользователю. `GetOwnedAsync` не трогали — правка/удаление встроенных закрыты
  (owner=null ≠ userId → «не найден»).
- Клонирование уже работало через `DuplicateNpcHandler` (`GetViewableAsync` + копия `Private`,
  owner = текущий, `IsBuiltIn=false`). DTO `NpcListItemDto`/`NpcDetailDto` получили `IsBuiltIn`.

### Данные (86 существ)
- Источник — готовый датасет `_books/_adversaries/genesys_fantasy_adversaries_ru.json`,
  фильтр `source_file == "genesys-official-RoT.json"` = **86 официальных существ RoT**
  (42 Rival / 29 Nemesis / 15 Minion). Числа — из канонического `raw_en`, имена/описания/качества —
  из русских переводов. Книжный текст не копируется: только статблок + короткие механич. описания.
- Генератор `_books/gen-bestiary-catalog.mjs` → embedded `SeedContent/bestiary.catalog.json`.
  Маппинг: навыки `"X: Y"→"X (Y)"`, силуэт из ability `Silhouette N`, минион ranks=0/strain=null,
  качества `Vicious 1`→`{code,nameRu,rating}`, грязные range/crit нормализуются.
- Идемпотентный `SeedData.SeedBestiary` (по натуральному ключу System+Name): вставляет
  отсутствующих, качества атак резолвит к `QualityDef` по коду. Существа — `PublicTemplate`,
  Source = «Realms of Terrinoth», пригодны и для PublicSafe, и для PrivateFull.

### Фронт
- `NpcsPage`: фильтр источника (Все / Мои / Встроенные), бейдж «встроенный» в списке,
  бейдж «встроенный · только чтение» и кнопка «Клонировать в мою библиотеку» в детали;
  «Редактировать/Удалить» для встроенных скрыты. Тип `NpcListItem`/`NpcDetail` += `isBuiltIn`.

## Тесты
- Api `SeedDataTests`: все 86 встроенных — без владельца/`PublicTemplate`/RoT, проходят
  `NpcValidator` без Errors; Гигант (Nemesis, силуэт 3, strain, качества привязаны); минионы без
  strain/рангов; идемпотентность (повторный `Apply` не плодит существ/атак).
- Api `NpcTests`: встроенный виден новому пользователю read-only; PUT/DELETE → BadRequest;
  duplicate → копия `IsMine`/не-built-in/`Private`, редактируема.
- Front `NpcsPage.test.tsx`: бейдж + фильтр источника; read-only (clone есть, edit/delete нет).
- Итог: backend Domain 102 / Api 193, frontend 94 — зелёные; `npm run build` (tsc -b) чист.

## DoD
- [x] Встроенные существа RoT в библиотеке NPC, видны всем read-only.
- [x] Клонируются в свою библиотеку; копия редактируема и добавляется в encounter/стол.
- [x] Seed идемпотентен; данные проходят валидатор adversary.
