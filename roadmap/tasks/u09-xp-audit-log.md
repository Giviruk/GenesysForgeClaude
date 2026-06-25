# История трат XP / audit log (u09-xp-audit-log)

- **Roadmap:** U-09 — История трат XP / audit log (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u09-xp-audit-log`
- **Базовая ветка:** `master` (открытых PR нет на момент старта)
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

GF-004 · Аудит §2.3. У персонажа есть `TotalXp`/`SpentXp`, но нет истории: за что выдали XP,
что купили, что отменили. Добавляем `CharacterAuditEntry` и пишем запись в **той же транзакции**,
что и операция (один `SaveChangesAsync`), при buy/refund характеристик/навыков/талантов/предметов,
complete-creation, ручном изменении XP и выдаче XP.

`XpDelta` = изменение **доступного** XP (покупка → −cost, рефанд → +cost, награда → +amount);
для операций без XP (предметы, смена героики, complete-creation) — `null`.

Затрагиваемое: все `Features/Characters/*Handler.cs` с XP/инвентарём; `CharacterEndpoints`;
фронт `SheetPage` (вкладка «История»), `client.ts`, `types.ts`.

## План выполнения

- [x] B: enum `CharacterAuditAction` + entity `CharacterAuditEntry` (по GF-004)
- [x] B: DbSet (`IAppDbContext`/`AppDbContext`) + EF config + миграция `AddCharacterAudit`
- [x] B: `Common/CharacterAudit.Record(...)` helper (общая запись)
- [x] B: запись аудита в Buy/Refund (char/skill/talent), CompleteCreation, AddItem/SellItem/RemoveItem,
      SetHeroicAbility, UpdateCharacter (ручной TotalXp → ManualEdit)
- [x] B: `AwardXp` command/handler + `POST /api/characters/{id}/xp-awards` (XpAwarded)
- [x] B: `GetCharacterAudit` query/handler + `GET /api/characters/{id}/audit` + DTO + DI
- [x] F: типы + `client.ts` (`characterAudit`, `awardXp`)
- [x] F: вкладка «История» (`HistoryTab`) — дата/тип/описание/ΔXP/состояние после; форма выдачи XP
- [x] Тесты: xUnit `CharacterAuditTests` (7) — buy/refund/award/manual/idempotent/доступ
- [x] `dotnet test` (62 dom + 147 api) и frontend lint/build/test (67) зелёные
- [x] Миграция создана + `docs/database.md` обновлён
- [x] Статус в `unified-roadmap.md` обновлён
- [ ] PR открыт

## Что осталось / блокеры

- Vitest для `HistoryTab`/клиента не добавлял: вкладка — отображение данных без чистой логики,
  методы клиента — тонкие обёртки `request<>` (как остальные). Логика покрыта xUnit-тестами аудита.
- Браузерная проверка вкладки — опционально (нужны auth + персонаж с операциями).

## Заметки / решения

- Аудит пишется через `db.CharacterAuditEntries.Add(...)` перед единственным `SaveChangesAsync`
  операции → атомарность (сбой записи откатывает всю операцию). Отдельной транзакции не требуется.
- `XpDelta` = Δдоступного XP (см. контекст); summary — на русском, метки характеристик как на фронте.
- Изменение ранга улучшения героики (очки, не XP) пока не логируется; selection — `HeroicAbilityChanged`.
