# U-25 · Кастомные архетип/раса и карьера

- **Roadmap:** U-25 — Кастомные архетип/раса и карьера (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u25-custom-archetype-career`
- **Базовая ветка:** `master`
- **PR:** pending
- **Статус:** 🚧 In progress

## Контекст

Задача закрывает homebrew gap из аудита §4.1: пользователь должен создавать свои виды/архетипы и карьеры без правки seed/code.

Текущий roadmap scope:

- Backend: расширить `CustomContentEndpoints.cs`: CRUD кастомного архетипа (характеристики/пороги/XP/способность) и кастомной карьеры (карьерные навыки).
- Использовать `OwnerUserId` как у прочего homebrew.
- Учесть структурные модели U-12/U-13.
- Frontend: формы в `CustomTab.tsx`; доступность в создании персонажа.
- DoD: пользователь создаёт свой вид/карьеру и собирает на них персонажа без правки кода.

## План выполнения

- [x] Создать ветку и plan-файл.
- [x] Отметить U-24 как Done после merge PR #68 и U-25 как In progress.
- [ ] Изучить текущие модели `ArchetypeDef`/`CareerDef`, custom endpoints, reference filtering и creation flow.
- [ ] Спроектировать минимальный API/DTO для custom archetype/career CRUD без изменения copyright/seed policy.
- [ ] Реализовать backend CRUD и валидацию ownership/visibility.
- [ ] Реализовать frontend API методы и формы в `CustomTab.tsx`.
- [ ] Убедиться, что кастомные архетипы/карьеры доступны в создании персонажа.
- [ ] Добавить/обновить xUnit и Vitest тесты.
- [ ] Запустить релевантные проверки.
- [ ] Обновить docs/plan и открыть PR.

## Что осталось / блокеры

- Нужно подтвердить по коду, какие поля structural U-12/U-13 должны быть в v1 формы, чтобы не раздувать scope.

## Заметки / решения

- Assumption: v1 должен покрыть создание playable archetype/career для собственного аккаунта; built-in seed и official text не меняются.
