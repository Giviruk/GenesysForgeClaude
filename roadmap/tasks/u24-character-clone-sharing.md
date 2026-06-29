# U-24 · Клонирование и read-only шеринг персонажа

- **Roadmap:** U-24 — Клонирование и read-only шеринг персонажа (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u24-character-clone-sharing`
- **Базовая ветка:** `master`
- **PR:** pending
- **Статус:** 🚧 In progress

## Контекст

Задача закрывает аудит §2.4: пользователю нужен быстрый способ клонировать своего персонажа и публичная read-only ссылка на лист без логина.

Текущий roadmap scope:

- Backend: `POST /api/characters/{id}/duplicate`; `POST /api/characters/{id}/share`; публичный `GET /api/share/{token}`.
- Frontend: кнопки «Клонировать» и «Поделиться» на листе/списке.
- DoD: персонаж клонируется; ссылка открывает read-only лист без логина; токен можно отозвать.

## План выполнения

- [x] Создать ветку и plan-файл.
- [x] Отметить U-23 как Done после merge PR #67 и U-24 как In progress.
- [x] Изучить текущие handlers/endpoints для персонажей, export/import и read-only просмотра листов.
- [x] Спроектировать минимальную модель шаринга: token, revoke, публичное чтение листа без auth.
- [x] Реализовать backend clone/share/revoke/public-sheet endpoints.
- [x] Добавить миграцию и обновить `docs/database.md`, если появится persistent model.
- [x] Реализовать frontend API методы и UI кнопки на листе/списке.
- [x] Добавить/обновить xUnit и Vitest тесты.
- [x] Запустить релевантные проверки.
- [ ] Обновить план и открыть PR.

## Что осталось / блокеры

- Нужно открыть PR после финального commit/push.

## Заметки / решения

- Assumption: токен шаринга opaque/random; raw token возвращается только при создании, в БД хранится SHA-256 hash + `RevokedAt`.
- Assumption: публичный endpoint возвращает read-only `CharacterSheetDto`; приватные заметки персонажа не включаются в public view, потому что они остаются за auth-only notes endpoint.
