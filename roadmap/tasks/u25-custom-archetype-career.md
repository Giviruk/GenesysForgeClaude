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
- [x] Изучить текущие модели `ArchetypeDef`/`CareerDef`, custom endpoints, reference filtering и creation flow.
- [x] Спроектировать минимальный API/DTO для custom archetype/career CRUD без изменения copyright/seed policy.
- [x] Реализовать backend CRUD и валидацию ownership/visibility.
- [x] Реализовать frontend API методы и формы в `CustomTab.tsx`.
- [x] Убедиться, что кастомные архетипы/карьеры доступны в создании персонажа.
- [x] Добавить/обновить xUnit и Vitest тесты.
- [x] Запустить релевантные проверки.
- [ ] Обновить docs/plan и открыть PR.

## Что осталось / блокеры

- Остались commit/push/PR.

## Заметки / решения

- Assumption: v1 должен покрыть создание playable archetype/career для собственного аккаунта; built-in seed и official text не меняются.
- Решение: v1 архетипа поддерживает одну manual ability и не добавляет стартовые skill choices; v1 карьеры поддерживает career skills и стартовые деньги, без стартового снаряжения/правил. Это держит scope U-25 минимальным и совместимым с U-12/U-13.
