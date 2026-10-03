# Тише SQL-лог и узкие проверки владельца (narrow-owner-queries)

- **Пункт ТЗ:** вне ROT/GEN — производительность, по итогам мониторинга VPS 01.10.2026 (пп. 1, 2 списка оптимизаций)
- **Ветка:** `feature/narrow-owner-queries`
- **Базовая ветка:** `feature/postgres-pin-healthcheck-pgstat` (стек поверх #253), после его мержа — `master`
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

Логи прода 01.10.2026:

- EF Core писал каждый SQL целиком уровнем Information — 64–82% строк лога API; `svcctl logs` (tail 200) покрывал
  6–15 минут;
- `GetOwnedAsync` грузит весь граф персонажа (~37 split-запросов) даже там, где нужна только проверка владельца:
  `POST xp-awards` — 39 SQL / 238 мс в БД, `GET notes` — 18 SQL / 197 мс, `GET audit` — 18 SQL / 125 мс,
  `DELETE critical-injuries` — 38 SQL / 228 мс.

Публичный API, игровые правила и схема БД не меняются.

## План выполнения

- [x] `Program.cs`: `Microsoft.EntityFrameworkCore.Database.Command` → Warning
- [x] `CharacterLoader.EnsureOwnedAsync` — один `EXISTS`, та же ошибка «Персонаж не найден.»
- [x] На `EnsureOwnedAsync`: заметки (чтение, создание), публичные ссылки (создание, отзыв), крит-ранения
  (добавление; снятие — ранение ищется по паре ранение+персонаж)
- [x] История: `CharacterLoader.AuditUndoQuery` — только навыки и таланты со справочниками (их читает `CharacterAuditUndo`)
- [x] Выдача XP: `UpdateQuery(needsXpValidation: true)` — те же проверки, что у правки `TotalXp` в `UpdateCharacterHandler`
- [x] Тесты: форма `AuditUndoQuery`; отказ чужому по всем изменённым командам без побочных эффектов; снятие
  крит-ранения через маршрут другого персонажа; проверки выдачи XP
- [x] `dotnet test` — 744 domain + 876 API
- [ ] PR открыт, CI зелёный, смержен, задеплоен

## Что осталось / блокеры

- Мутации с игровыми правилами (покупки, таланты, предметы, транспорт и т. п.) по-прежнему грузят полный граф —
  им он действительно нужен; сужать их — отдельная задача с разбором каждого правила.

## Заметки / решения

- Миграции не нужны: persistent model не меняется.
- `rot-rules-remediation-progress.md` не меняется: задача не относится к ROT remediation.
