# Адаптивный shell и навигация (responsive-shell-navigation)

- **Пункт ТЗ:** Адаптивная вёрстка — пункт 1: общий shell и навигация
- **Ветка:** `feature/responsive-shell-navigation`
- **Базовая ветка:** `master`
- **PR:** [#237](https://github.com/Giviruk/GenesysForgeClaude/pull/237)
- **Статус:** 🚧 In progress — код и CI готовы, нужна визуальная проверка PR-превью

## Контекст

Приватная версия GenesysForge имеет desktop-shell с фиксированной боковой панелью шириной 14.5rem. Текущие правила до 48rem переносят все пункты навигации в несколько строк, из-за чего мобильный экран получает слишком высокий и неудобный header. Требуется сохранить desktop-навигацию и добавить отдельный мобильный сценарий.

## Зафиксированные требования

- Desktop: сохранить боковую навигацию и текущую визуальную систему.
- Телефон и планшет до 48rem: мобильная верхняя панель с hamburger-кнопкой.
- Навигация открывается как выезжающий drawer слева.
- При открытом drawer показывается затемнённый backdrop.
- Drawer закрывается выбором пункта, кликом по backdrop и клавишей Escape.
- При открытом drawer запрещается прокрутка основного контента.
- Навигация и кнопки должны иметь доступные labels/aria-состояния.
- Учитывать safe-area inset на мобильных устройствах.
- Не менять бизнес-логику, API, auth, seed, package versions и deployment files.

## План выполнения

- [x] Аудит текущего shell и существующих responsive rules.
- [x] Создать отдельную ветку задачи.
- [x] Добавить мобильный topbar и drawer-состояние в `frontend/src/App.tsx`.
- [x] Добавить menu/close icons в `frontend/src/components/Icon.tsx`.
- [x] Добавить CSS shell/drawer/backdrop в `frontend/src/index.css`.
- [x] Проверить lint, unit tests и production build.
- [~] Проверить desktop/tablet/mobile сценарии браузером — desktop-аудит выполнен; PR preview для новых mobile-стилей пока не развернут.
- [x] Открыть PR.

## Проверки

CI run 580 успешно завершил:

- Frontend: lint, tests, build.
- Backend: build, tests, PublicSafe publish check.
- Migrations: non-empty DB.
- E2E smoke: full-stack startup и Playwright smoke tests.

## Что осталось / блокеры

Нужно открыть PR-превью или развернуть ветку на тестовом окружении и проверить viewport 360–430px и 768px: открытие drawer, backdrop, Escape, переход по пункту, блокировку scroll и сохранение desktop sidebar.

## Заметки / решения

- Assumption: breakpoint `48rem` остаётся границей drawer-режима, чтобы не ломать существующие page-level responsive rules.
- Существующие page-level media rules не удаляются на этом этапе; shell overrides добавляются отдельно, чтобы снизить риск регрессии.
- Изменения ограничены frontend shell/navigation и task-документацией; бизнес-логика, API, auth, seed, package versions и deployment files не менялись.
