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
- [ ] Изучить текущие handlers/endpoints для персонажей, export/import и read-only просмотра листов.
- [ ] Спроектировать минимальную модель шаринга: token, revoke, публичное чтение листа без auth.
- [ ] Реализовать backend clone/share/revoke/public-sheet endpoints.
- [ ] Добавить миграцию и обновить `docs/database.md`, если появится persistent model.
- [ ] Реализовать frontend API методы и UI кнопки на листе/списке.
- [ ] Добавить/обновить xUnit и Vitest тесты.
- [ ] Запустить релевантные проверки.
- [ ] Обновить план и открыть PR.

## Что осталось / блокеры

- Нужно выбрать точную модель токена после изучения текущего кода.

## Заметки / решения

- Assumption: токен шаринга должен быть opaque/random, храниться сервером, иметь `RevokedAt`; публичный endpoint возвращает тот же read-only `CharacterSheetDto`, но без правок.
