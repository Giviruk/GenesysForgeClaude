# Feature Roadmap

Roadmap описывает продуктовые этапы. Он не является жестким планом релизов, но помогает AI-агентам понимать приоритеты и не добавлять future scope в MVP-задачи.

## MVP

Цель: рабочий личный лист персонажа с auth, расчетами и базовым custom content.

- Регистрация и авторизация по JWT.
- Создание персонажа.
- Выбор `GameSystem`: Genesys Core или Realms of Terrinoth.
- Выбор архетипа и карьеры.
- Стартовые характеристики из архетипа.
- Стартовый XP.
- Выбор бесплатных career skill ranks.
- Список навыков с ranks, career status и dice pool.
- Покупка и refund рангов навыков в фазе создания.
- Покупка и refund характеристик в фазе создания.
- Завершение фазы создания.
- Список талантов.
- Покупка и refund талантов в фазе создания.
- Проверка пирамиды талантов.
- Поддержка ranked talents.
- Derived stats: wounds, strain, soak, defense, encumbrance.
- Инвентарь: добавить предмет, изменить количество, экипировать/перенести, удалить.
- Автоматический пересчет derived stats от инвентаря.
- Heroic abilities для Realms of Terrinoth.
- Custom skills, talents, items, heroic abilities через UI.
- Изоляция custom content по пользователю.
- Docker compose для локального запуска.
- CI: backend tests, frontend lint/test/build.

## Beta

Цель: сделать приложение удобным для реальных кампаний.

- Улучшенная UX-навигация по листу персонажа.
- Валидация форм на frontend до отправки.
- Более полные empty/loading/error states.
- Редактирование имени, текущих wounds/strain и total XP.
- История XP операций.
- Лог покупок и refund.
- Мягкое предупреждение при перегрузе.
- Более подробные filters/search для навыков, талантов, предметов.
- Импорт/экспорт персонажа в JSON.
- Печать или PDF-friendly лист.
- Административный/maintenance сценарий для seed data.
- Расширенные API tests на негативные сценарии.
- E2E smoke tests для основных пользовательских flows.

## Release 1.0

Цель: стабильная версия для кампаний.

- Стабильная версия REST API.
- Миграции вместо destructive schema setup.
- Production-ready configuration для PostgreSQL, JWT, CORS.
- Backup/restore рекомендации.
- Observability basics: structured logs и health checks.
- Accessibility pass для основных экранов.
- Responsive UI для desktop/tablet/mobile.
- Документированная политика copyrighted content.
- User-facing documentation или help pages без официальных текстов.
- Release checklist и changelog.
- Security review auth и ownership checks.

## Future Features

Идеи после 1.0; не добавлять в MVP без отдельного решения.

- Кампании и приглашения игроков.
- Роли ведущий/игрок.
- Shared character sheets.
- Notes, injuries, obligations/motivations и campaign-specific fields.
- Dice roller и история бросков.
- Custom rule packs.
- Версионирование homebrew content.
- Marketplace/community sharing для пользовательского контента с moderation.
- Поддержка дополнительных Genesys settings.
- Импорт из сторонних форматов.
- Offline-first режим.
- Real-time collaboration.
- Audit log для изменений листа.
- Fine-grained permissions.
- Internationalization.

