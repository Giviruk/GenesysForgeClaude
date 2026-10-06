# GEN-CONTENT-01 — личная библиотека

- **ТЗ:** [account-campaign-content.md](../../docs/account-campaign-content.md), этап 3.
- **Ветка:** `feature/gen-content-01-personal-library`
- **База:** `feature/gen-content-01-campaign-characters` (PR #266).
- **Статус:** реализовано, проверки пройдены; PR готовится.

## План

- [x] Прочитать план, проверить status/fetch/PR и базу стека.
- [x] Личный пакет и nullable CampaignId в custom create commands.
- [x] Шесть POST /api/custom и общие PUT/DELETE, совместимые campaign routes.
- [x] CustomTab без кампании, страница и навигация «Моя библиотека».
- [x] API/frontend тесты, build/lint; документация и PR.

## Остаток

Версии, предложения игрока и отдельный ContentAccessPolicy исключены пользователем.
В этом этапе нет изменения модели или миграций.

Проверено: 19 backend custom/homebrew тестов, 60 frontend тестов, build и lint.

## Исправления ревью PR #267

- [x] Не переносить служебные Personal custom:/Campaign custom: описания при JSON/shared-copy импорте.
- [x] Детерминированно выбирать автопакет по CreatedAt, затем Id.
- [x] Регрессия: собственная библиотека мастера не дополняет импортированный пакет кампании;
      обычное пользовательское описание сохраняется.
- [x] Проверить custom/homebrew тесты и закоммитить исправления.
Ответ на ревью будет опубликован после обновления стека; слияние не выполнять.
