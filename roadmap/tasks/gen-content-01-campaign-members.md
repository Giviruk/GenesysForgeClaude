# GEN-CONTENT-01 — членство аккаунта в кампании

- **ТЗ:** [account-campaign-content.md](../../docs/account-campaign-content.md), этап 1.
- **Ветка:** `feature/gen-content-01-campaign-members`
- **База:** `feature/account-campaign-content-design` (PR #264).
- **Статус:** реализовано, проверки пройдены; PR готовится. Пользователь запросил реализацию 07.10.2026.

## План

- [x] Прочитать исправленное ТЗ, проверить status/fetch/PR, сохранить редакцию пользователя.
- [x] CampaignMember, helper, перенос авторизации и DTO игроков.
- [x] Join без персонажа, добавление персонажа, выход/исключение аккаунта.
- [x] UI и клиент API.
- [x] Миграция AddCampaignMembers с backfill; database/api docs.
- [x] Backend/frontend тесты и проверки.
- [x] Коммит и отдельный stacked PR, обновление общего прогресса.

## Остаток / блокеры

Этап 1 реализован. Проверено: 48 backend campaign/NPC тестов, 50 frontend тестов, build и lint. Этапы 2–4 выполняются следующими отдельными ветками/PR.
Seed, правила XP и JWT не меняются. Членство мастера определяется только GmUserId.
