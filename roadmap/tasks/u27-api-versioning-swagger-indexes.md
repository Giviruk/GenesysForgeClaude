# U-27 · API versioning + Swagger/Scalar UI + индексы БД

- **Roadmap:** U-27 — API versioning + Swagger/Scalar UI + индексы БД (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u27-api-versioning-swagger-indexes`
- **Базовая ветка:** `master`
- **PR:** pending
- **Статус:** 🚧 In progress

## Контекст

Задача закрывает P3 infrastructure/polish: API должен иметь версионированный путь, удобную OpenAPI UI-страницу и явные индексы на горячие запросы.

Roadmap scope:

- Префикс `/api/v1` или version header на группах.
- Подключить Scalar/Swagger UI поверх существующего `MapOpenApi`.
- Явные EF индексы на горячих полях: поиск NPC по `System`/`Kind`/`Role`/`Tag`, контент по `System`/`OwnerUserId`, токены.
- DoD: версия в путях; UI документации открывается; миграция с индексами применена.

## План выполнения

- [x] Создать ветку и plan-файл.
- [x] Отметить U-26 как Done после merge PR #70 и U-27 как In progress.
- [ ] Изучить текущий `Program.cs`, endpoint mappings, OpenAPI setup и EF indexes.
- [ ] Выбрать безопасную стратегию `/api/v1` без поломки существующих frontend/API tests.
- [ ] Подключить Scalar/Swagger UI поверх OpenAPI.
- [ ] Добавить EF индексы и миграцию.
- [ ] Обновить docs/api.md и docs/database.md.
- [ ] Добавить/обновить backend/frontend тесты по изменённому API contract.
- [ ] Запустить релевантные проверки.
- [ ] Открыть PR.

## Что осталось / блокеры

- Нужно проверить, можно ли добавить `/api/v1` без массового переписывания всех endpoint route templates.

## Заметки / решения

- Assumption: для совместимости v1 можно добавить как новый публичный alias, не удаляя legacy `/api/*` в этом PR.
- Copyright: задача инфраструктурная; seed/private content не должен меняться.
