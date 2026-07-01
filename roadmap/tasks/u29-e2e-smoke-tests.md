# U-29 · E2E smoke tests (u29-e2e-smoke-tests)

- **Roadmap:** U-29 — E2E smoke tests (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u29-e2e-smoke-tests`
- **Базовая ветка:** `master`
- **PR:** #74 — https://github.com/Giviruk/GenesysForgeClaude/pull/74
- **Статус:** 🚧 In progress

## Контекст

Roadmap U-29 требует Playwright smoke coverage основных flow и запуск в CI. До задачи в репозитории были xUnit/Vitest, но не было Playwright config, e2e scripts или CI job.

Затрагиваемые файлы:

- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/playwright.config.ts`
- `frontend/e2e/smoke.spec.ts`
- `.github/workflows/ci.yml`
- `roadmap/unified-roadmap.md`

## План выполнения

- [x] Создать ветку и task plan.
- [x] Отметить U-28 как Done после merge PR #72 и U-29 как In progress.
- [x] Добавить Playwright dependency и npm scripts.
- [x] Добавить Playwright config для запуска против полного стека на `http://localhost:8080`.
- [x] Добавить smoke specs для 10 сценариев GF-015/U-29.
- [x] Добавить CI job `e2e`, который поднимает `docker compose up -d --build` и запускает Playwright.
- [x] Добавить readiness wait перед Playwright после первого CI failure из-за раннего старта тестов.
- [x] Уточнить browser assertions после второго CI failure: перейти во вкладку инвентаря перед проверкой предмета и убрать strict-mode ambiguity на Game Table.
- [x] Запустить frontend lint/unit/build и Playwright test discovery локально.
- [ ] Запустить полный локальный E2E против `docker compose`.
- [x] Открыть PR.

## Что осталось / блокеры

Полный локальный E2E не выполнен: Docker Desktop engine недоступен (`dockerDesktopLinuxEngine` pipe not found), поэтому `docker compose up -d --build` не смог поднять стек. CI job должен выполнить полный smoke run после открытия PR.

Первый запуск PR #74 упал в E2E: Playwright начал `POST /api/auth/register` до готовности API за nginx и получил `502 Bad Gateway`. В CI добавлен шаг ожидания `/api/auth/providers` перед запуском Playwright.

Второй запуск PR #74 дошёл до тестов и выявил хрупкие browser assertions: предмет проверялся до перехода на вкладку инвентаря, а имя участника Game Table совпадало в карточке и `<option>`. Селекторы уточнены.

## Заметки / решения

- Assumption: запрос “идти к следующему пункту roadmap” явно включает dependency change для U-29, потому сам U-29 задаёт Playwright как инструмент.
- Smoke tests создают данные через реальные API полного стека, затем проверяют пользовательские экраны в браузере. Это снижает хрупкость тестов на больших формах, но оставляет покрытие browser-visible flow.
- Copyright: seed/reference content не меняется.
