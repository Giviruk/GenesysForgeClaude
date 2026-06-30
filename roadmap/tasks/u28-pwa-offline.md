# U-28 · PWA / офлайн (u28-pwa-offline)

- **Roadmap:** U-28 — PWA / офлайн (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u28-pwa-offline`
- **Базовая ветка:** `master`
- **PR:** pending
- **Статус:** 🚧 In progress

## Контекст

Задача закрывает P3 polish: приложение должно иметь web app manifest, service worker и кеширование статических ассетов и read-only справочника, чтобы справочник был доступен офлайн после первого онлайн-запроса.

Roadmap scope:

- manifest + service worker (`vite-plugin-pwa`);
- кеш статики;
- кеш read-only справочника.

## План выполнения

- [x] Создать ветку и plan-файл.
- [x] Отметить U-27 как Done после merge PR #71 и U-28 как In progress.
- [x] Изучить текущий frontend build/Vite/API client.
- [x] Добавить PWA manifest и service worker через `vite-plugin-pwa`.
- [x] Настроить runtime cache для read-only reference endpoints.
- [x] Добавить минимальные PWA assets.
- [x] Обновить документацию по frontend/current-state.
- [x] Запустить релевантные frontend проверки.
- [ ] Открыть PR.

## Что осталось / блокеры

- Открыть PR после финального просмотра diff.
- Полный `npm audit` показывает high advisory в dev dependency path `jsdom -> undici@7.27.2`; `npm audit --omit=dev` чистый. `npm audit fix` не выполнен: требует отдельного явного одобрения, так как может менять dependency tree шире U-28.

## Заметки / решения

- Assumption: roadmap scope U-28 явно разрешает добавить dev dependency `vite-plugin-pwa`.
- Runtime cache покрывает legacy `/api/*` и versioned `/api/v1/*`, потому frontend пока использует legacy paths.
- Проверки: `npm run build`, `npm run test`, `npm run lint`, `npm audit --omit=dev`.
- Copyright: задача инфраструктурная; seed/private content не должен меняться.
