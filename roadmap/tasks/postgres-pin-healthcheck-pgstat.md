# Postgres: закрепить версию, разредить healthcheck, pg_stat_statements (postgres-pin-healthcheck-pgstat)

- **Пункт ТЗ:** вне ROT/GEN — эксплуатация, по итогам мониторинга VPS 01.10.2026 (пп. 3, 4, 6 списка оптимизаций)
- **Ветка:** `feature/postgres-pin-healthcheck-pgstat`
- **Базовая ветка:** `master` (открытых PR нет)
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

Мониторинг 01.10.2026 показал:

- `docker compose pull` в деплое тянет плавающий `postgres:17-alpine`: 01.10 обе БД пересоздались вместе с релизом
  приложения (вышел патч 17.11);
- healthcheck'и раз в 10 с — это `docker exec` (runc + процессы); на общей машине проверки всех контейнеров съедали
  ~6% CPU хоста (containerd ~11% ядра);
- простые split-SELECT по PK занимают 6–12 мс — для БД в десятки МБ много, нужна статистика по запросам.

Меняется только `docker-compose.prod.yml` и документация; код, схема БД (миграций нет) и API не меняются.

## План выполнения

- [x] `postgres`, `postgres-public`, `backup`: `postgres:17-alpine` → `postgres:17.11-alpine` (та же версия, что уже работает)
- [x] healthcheck обеих БД: `interval` 10s → 30s, `start_period: 60s` + `start_interval: 2s` (Docker 29.6 на сервере)
- [x] `postgres`: `shared_preload_libraries=pg_stat_statements`
- [x] `docs/production-operations.md`: версия Postgres и pg_stat_statements
- [x] `docker compose -f docker-compose.prod.yml config` разбирается
- [ ] PR открыт, CI зелёный, смержен
- [ ] После деплоя: `CREATE EXTENSION pg_stat_statements` в `genesysforge`, обе БД healthy, `/api/health` → 200
- [ ] Снять статистику после живой нагрузки и разобрать медленные запросы

## Что осталось / блокеры

- Деплой перезапустит обе БД один раз (меняются command/healthcheck) — секунды простоя API.

## Заметки / решения

- Расширение создаётся вручную, а не миграцией: оно нужно только проду для диагностики, а миграция затронула бы
  dev/CI-базы без preload (где запросы к `pg_stat_statements` падали бы).
- `pg_stat_statements` включён только в приватной БД: у публичной реального трафика нет.
- Тесты не нужны: меняется только конфигурация деплоя.
