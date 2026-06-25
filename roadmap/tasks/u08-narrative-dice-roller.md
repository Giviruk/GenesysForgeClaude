# Нарративный dice roller + секретный бросок GM (u08-narrative-dice-roller)

- **Roadmap:** U-08 — Нарративный dice roller + секретный бросок GM (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u08-narrative-dice-roller`
- **Базовая ветка:** `feature/u07-deep-links-to-master` (стек поверх PR #38, ещё не слит — AGENTS.md §2)
- **PR:** [#39](https://github.com/Giviruk/GenesysForgeClaude/pull/39) (слит в master)
- **Статус:** ✅ Done

## Контекст

Ключевой GM live-tool. Сейчас `DicePoolView` показывает только **состав** пула (жёлтые/зелёные кубы),
бросать нельзя. Реализуем нарративный roller символами Genesys (Success/Failure/Advantage/Threat/
Triumph/Despair) и лог бросков за столом с realtime-публикацией и секретными бросками мастера.

Решение: **результат считается на клиенте** (v1, как и план U-17 «frontend calc»), сервер хранит
лог (`RollLogEntry`) и рассылает thin-событие `RollAdded` через существующий `ICampaignNotifier`/SignalR.
Источник истины для лога — REST; realtime только инвалидация (как у Game Table).

Затрагиваемое: `DicePoolView.tsx`, `GameTableTab.tsx`, `useCampaignHub.ts`, `client.ts`, `types.ts`,
`CampaignsPage.tsx`; backend `Features/GameTable/Roll*`, `Dtos/RollDtos.cs`, `ICampaignNotifier`,
`AppDbContext`, миграция.

## План выполнения

- [x] F: `utils/diceRoller.ts` — кости Genesys (faces), `rollPool(rng)`, нетто-итог, summary
- [x] F: `utils/diceRoller.test.ts` — Vitest со seedable rng (faces, cancellation, triumph/despair)
- [x] F: `components/DiceRoller.tsx` — сбор пула, бросок, символьный результат (reusable)
- [x] F: интеграция в `GameTableTab` — секция броска + лог стола, секретный чекбокс для GM
- [x] F: кнопка «бросить» по навыку на листе (`SheetTab`) — открывает roller с пулом навыка
- [x] F: `client.ts`+`types.ts` — `rolls`/`createRoll`, типы; `useCampaignHub` onRollAdded; CampaignsPage
- [x] B: `Domain/Entities/RollLogEntry.cs` + DbSet + EF config + миграция `AddRollLog`
- [x] B: `Dtos/RollDtos.cs`, `Features/GameTable/CreateRoll.cs`, `GetRolls.cs`, DI
- [x] B: `ICampaignNotifier.RollAddedAsync` + SignalR impl; `RollEndpoints` + `Program.MapRolls`
- [x] B: секрет только GM (игроку форсим IsSecret=false); чужой видит не-секретные
- [x] Тесты: xUnit `RollLogTests` (5) + Vitest `diceRoller.test.ts` (11) — зелёные
- [x] `npm run lint` / `npm run build` / `npm test` (67) и `dotnet test` (62 dom + 140 api) зелёные
- [x] Миграция создана + `docs/database.md` обновлён
- [x] Статус в `unified-roadmap.md` обновлён
- [x] PR открыт — #39 (base — `feature/u07-deep-links-to-master`)
- [ ] Браузерная проверка стола (нужна auth-сессия + кампания GM+игрок + Postgres для realtime/секрета)

## Что осталось / блокеры

- Браузерная проверка realtime-лога и секретного броска отложена: требует поднятого
  full-stack (Postgres + API + web) и двух пользователей (GM/игрок) в одной кампании.
  Логика покрыта integration-тестами (`RollLogTests`) и unit-тестами кубов.

## Заметки / решения

- `Assumption`: результат броска считает клиент, сервер только логирует (v1). Для доверенного стола
  достаточно; серверный RNG не дублируем (консистентно с планом U-17).
- Секретный флаг чести только у GM; на чтении лог фильтруется по роли (как gmNotes сцены).
- Realtime: добавлено событие `RollAdded`; в `CampaignsPage` бампит тот же `liveSignal`, что и
  `GameTableChanged` → лог перечитывается.
