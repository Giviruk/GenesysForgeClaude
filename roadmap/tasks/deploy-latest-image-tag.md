# Deploy latest image tag cleanup (deploy-latest-image-tag)

- **Roadmap:** вне roadmap — production deploy maintenance
- **Ветка:** `feature/deploy-latest-image-tag`
- **Базовая ветка:** `master`
- **PR:** #73
- **Статус:** ✅ Done

## Контекст

На VPS закончилось место из-за множества старых Docker images. Текущий deploy публикует образы и с `latest`, и с git SHA tag, затем compose использует SHA через `IMAGE_TAG`. Старые SHA-tagged images остаются локально и не удаляются обычным `docker image prune -f`.

Затрагиваемые файлы:

- `.github/workflows/deploy.yml`
- `docker-compose.prod.yml`

## План выполнения

- [x] Убрать публикацию SHA-тегов для app images в deploy workflow.
- [x] Убрать `IMAGE_TAG` из SSH env и `.env` на VPS.
- [x] Переключить production compose на явный `:latest`.
- [x] Заменить dangling-only prune на удаление всех неиспользуемых images после успешного `up -d`.
- [x] Проверить diff и YAML/compose синтаксис.
- [x] Открыть PR.

## Что осталось / блокеры

PR открыт: #73.

## Заметки / решения

`docker image prune -af` выполняется после `docker compose up -d`, поэтому images, используемые текущими running containers, не удаляются. Это удалит старые неиспользуемые tagged SHA images на сервере.
