# Обновление UI по новым прототипам (ui-redesign-prototypes)

- **Roadmap:** вне U-нумерации — глобальная доработка UI по HTML/PNG-прототипам
- **Ветка:** `feature/ui-redesign-prototypes`
- **Базовая ветка:** `master`
- **PR:** #88
- **Статус:** 🚧 In progress

## Контекст

Нужно обновить текущий UI GenesysForge по новым локальным прототипам:

- `_books/GenesysCore игровая платформа/GenesysForge Redesign.dc.html`
- `_books/GenesysCore игровая платформа/screenshots/*.png`

Прототип считается принятой дизайн-спекой. Новый дизайн должен максимально точно соответствовать прототипу: темная фэнтези-палитра, serif-заголовки, золотые primary actions, боковая навигация с иконками, плотные карточки/таблицы/табы, explicit loading/error/empty states.

Пользователь разрешил добавлять необходимые зависимости. При этом зависимости добавляем только если они реально уменьшают сложность и соответствуют прототипу.

Основной приоритет этой итерации:

- базовая дизайн-система и shell;
- список персонажей по прототипу;
- вкладка талантов по прототипу, сохраняя полный список талантов, сортировку/фильтрацию по тегам и уровням, покупку/refund и пирамиду.

## План выполнения

- [x] Подготовка: прочитать `AGENTS.md`, `docs/ai-context.md`, применимые frontend skills
- [x] Выполнить `git fetch`, проверить статус и открытые PR
- [x] Создать ветку `feature/ui-redesign-prototypes` от свежего `master`
- [x] Сохранить план задачи в `roadmap/tasks/ui-redesign-prototypes.md`
- [x] Снять из прототипа дизайн-токены, типографику, иконки, состояния и component inventory
- [x] Добавить/подключить минимально нужную icon dependency или локальный набор иконок
- [x] Обновить `frontend/src/index.css`: tokens, typography, buttons, tabs, badges, cards, progress bars, modal/forms, responsive shell
- [x] Обновить `frontend/src/App.tsx`: sidebar с иконками, бренд `GENESYSFORGE`, footer nav, layout без лишнего topbar где он расходится с прототипом
- [x] Расширить `CharacterListItem` API для карточек списка: available XP, current/threshold wounds, current/threshold strain
- [x] Обновить backend handler/DTO и frontend types/tests для нового shape списка персонажей
- [x] Обновить `CharactersPage`: header, actions, loading/error/empty states, portrait placeholder, bars, XP, card actions
- [x] Обновить `SheetPage` header/tabs под прототип без изменения игровых правил
- [x] Обновить `TalentsTab`: пирамида, купленные таланты, полный список покупки, группировка/сортировка по категориям-тегам и уровням, actions
- [x] Проверить, что полный список талантов остается доступен и фильтруется по `TALENT_CATEGORIES` + tier tabs
- [x] Обновить/добавить Vitest/xUnit тесты для измененного API shape и UI-критичных чистых helpers
- [x] Запустить `dotnet test backend/GenesysForge.slnx`, `npm run lint`, `npm test`, `npm run build`
- [x] Выполнить browser/Playwright визуальную проверку desktop/mobile против прототипов
- [x] Copyright-проверка: seed/official book text не менялись
- [x] Открыть PR в `master`

## Что осталось / блокеры

- Not found in current codebase: поле portrait/avatar у персонажа. Для списка используем placeholder/инициалы, если отдельное поле не будет добавлено отдельной задачей.
- `_books/` gitignored и не должен попадать в PR.
- `debug.log` уже был untracked до старта задачи; не относится к этой ветке и не трогается.

## Заметки / решения

- Assumption: `GenesysForge Redesign.dc.html` и PNG из `_books/GenesysCore игровая платформа` являются принятой дизайн-спекой; отдельный Image Gen концепт не нужен.
- Assumption: задача вне unified roadmap, поэтому `roadmap/unified-roadmap.md` не обновляется.
- API shape списка персонажей будет изменен только для отображения уже существующих вычисляемых/хранимых значений; persistent model и миграции не требуются.
- Проверки: `dotnet test backend/GenesysForge.slnx`, `npm.cmd run lint`, `npm.cmd run test`, `npm.cmd run build`.
- Visual QA: Playwright с моками API, desktop список персонажей, desktop вкладка талантов, mobile список персонажей; console/page errors отсутствуют. Скриншоты: `C:\Users\givir\AppData\Local\Temp\genesysforge-ui-qa`.
