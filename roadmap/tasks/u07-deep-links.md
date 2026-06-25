# Реальные URL / deep links (u07-deep-links)

- **Roadmap:** U-07 — Реальные URL / deep links (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u07-deep-links`
- **Базовая ветка:** `feature/u06-email-provider-password-reset` (стек поверх PR #36, ещё не слит — см. AGENTS.md §2)
- **PR:** pending
- **Статус:** 🚧 In progress

## Контекст

Расширяем клиентский роутер ([router.ts](../frontend/src/router.ts)) — **без перехода на React Router**
(не тянем новую зависимость, текущий History-API роутер чистый). Нужны deep links на под-вью,
которые сейчас живут как внутренний state страниц:

- `/characters/:id/print` — печатный лист (`SheetPage`, state `printing`)
- `/campaigns/:id/table` — Game Table (`CampaignDetailView`, tab `table`)
- `/campaigns/:id/handbook` — Handbook (tab `handbook`)
- `/campaigns/:id/encounters` и `/campaigns/:id/encounters/:eid` — список/конкретный энкаунтер
  (`EncountersTab`, state `openId`)

Уже работают: `/login /register /characters /characters/:id /campaigns /campaigns/:id /npcs /npcs/:id
/magic /about`. Аутентификация и возврат на исходный URL после логина уже есть (`Shell` рендерит
`AuthPage` при отсутствии токена; `session.ts` хранит returnTo).

## План выполнения

- [x] `parseRoute` → `{ area, id, sub, subId, unknown }`; area-aware валидация sub (print только для
      characters; table/handbook/encounters для campaigns; encounters/:eid → subId)
- [x] `router.test.ts` — обновлены существующие кейсы (новые поля) + кейсы под-роутов
- [x] `App.tsx` — прокинуты sub/subId в SheetPage и CampaignsPage + navigate-колбэки
- [x] `SheetPage` — `printing` управляется URL (`/print`), кнопка/закрытие навигируют
- [x] `CampaignsPage`/`CampaignDetailView` — вкладка из URL (overview/handbook/encounters/table)
- [x] `EncountersTab` — `openId` управляется URL (`openEncounterId` + onOpen/onClose)
- [x] `npm run lint`, `npm test` (56), `npm run build` (tsc) зелёные
- [ ] Браузерная проверка deep link / back-forward / refresh (нужна auth-сессия + данные)
- [x] Миграции не требуются (frontend-only)
- [x] Статус в `unified-roadmap.md` обновлён
- [ ] PR открыт (base — ветка U-06)

## Что осталось / блокеры

(заполняется по ходу)

## Заметки / решения

- Выбран вариант «расширить router.ts», а не React Router — без новой зависимости (AGENTS.md:
  зависимости только по явному запросу), консистентно с текущим минимальным роутером.
- Под-вью становятся URL-управляемыми (state поднимается в URL), поэтому back/forward/refresh
  работают для печати, Game Table и энкаунтеров.
