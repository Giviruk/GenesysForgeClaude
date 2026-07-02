# Реализация прототипа игрового стола кампании (campaign-game-table-prototype)

- **Roadmap:** вне U-нумерации — доработка UI по HTML-прототипу
- **Ветка:** `feature/campaign-game-table-prototype`
- **Базовая ветка:** `master`
- **PR:** #85
- **Статус:** ✅ Done (PR #85)

## Контекст

Нужно подогнать реальный экран `Игровой стол` внутри кампании под `docs/campaign-game-table-prototype.html`, сохранив текущие API и правила. Прототип задает плотный GM-table layout: верхняя командная панель с текущей сценой, раундом/ходом и сюжетными очками; левая колонка текущего хода и инициативы; центральная доска дистанций; правая колонка бросков и заметок; нижняя строка быстрых действий и изменений.

Затрагиваемые файлы:

- `frontend/src/components/GameTableTab.tsx`
- `frontend/src/index.css`
- `frontend/src/App.tsx`

## План выполнения

- [x] Подготовка: AGENTS.md, docs/ai-context.md, актуальный `master`, новая ветка
- [x] Сравнить текущий `GameTableTab` с `docs/campaign-game-table-prototype.html`
- [x] Перестроить layout игрового стола под командную панель, левую колонку, центральную доску, правую колонку и нижнюю строку
- [x] Сохранить существующие действия: следующий ход, сброс/завершение, сюжетные очки, слоты инициативы, участники, дистанции, заметки, дайсроллер
- [x] Проверить responsive layout и отсутствие horizontal overflow
- [x] Запустить релевантные frontend проверки
- [x] Открыть PR

## Что осталось / блокеры

Нет.

## Заметки / решения

- Не меняем backend/API и persistent model: позиции дистанций остаются локальным UI state, как в текущем `RangeBandTracker`.
- Browser/IAB tool недоступен в текущем окружении; визуальная проверка будет выполнена Playwright fallback на Vite dev server с моками API.
- Реализован встроенный пул кубиков в правой панели через существующий `DiceRoller`, чтобы поведение записи в лог не расходилось с сайдбаром.
- Playwright QA: desktop 1440px и mobile 390px без horizontal overflow; desktop grid `288px 480px 384px`; `.participants-strip` один ряд со скроллом; inline dice pool найден.
