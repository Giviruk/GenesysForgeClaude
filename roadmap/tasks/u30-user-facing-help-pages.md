# U-30 · User-facing help pages (u30-user-facing-help-pages)

- **Roadmap:** U-30 — User-facing help pages (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u30-user-facing-help-pages`
- **Базовая ветка:** `master`
- **PR:** #75 — https://github.com/Giviruk/GenesysForgeClaude/pull/75
- **Статус:** 🚧 In progress

## Контекст

Roadmap U-30 требует пользовательскую справку: Markdown в `docs/user-guide/`, отрисовка в UI и ссылка «Справка». Справка должна объяснять базовые flow без README и не содержать оригинальные тексты из официальных книг.

Затрагиваемые файлы:

- `docs/user-guide/index.md`
- `frontend/src/pages/HelpPage.tsx`
- `frontend/src/App.tsx`
- `frontend/src/router.ts`
- `frontend/src/components/Footer.tsx`
- `frontend/src/index.css`
- `frontend/src/router.test.ts`
- `frontend/src/pages/HelpPage.test.tsx`
- `roadmap/unified-roadmap.md`

## План выполнения

- [x] Создать ветку и task plan.
- [x] Отметить U-29 как Done после merge PR #74 и U-30 как In progress.
- [x] Добавить copyright-safe Markdown guide в `docs/user-guide/`.
- [x] Добавить страницу `/help` с отрисовкой Markdown без новых зависимостей.
- [x] Добавить ссылки «Справка» в UI.
- [x] Обновить router tests и добавить UI smoke test для HelpPage.
- [x] Запустить релевантные проверки локально.
- [x] Открыть PR.

## Что осталось / блокеры

Ожидаются CI checks PR #75. Полный E2E не запускался локально, потому задача не меняет основные backend/full-stack flow; Playwright discovery выполнен.

## Заметки / решения

- Assumption: для U-30 достаточно одного markdown-файла `docs/user-guide/index.md`, потому roadmap требует `docs/user-guide/` как источник, а не отдельный файл на каждый раздел.
- Markdown рендерится собственным минимальным parser’ом (headings, paragraphs, lists, links), чтобы не добавлять dependency ради статичной справки.
- Локальные проверки: `npm run lint`, `npm test`, `npm run build`, `npm run test:e2e -- --list`.
- Copyright: официальный текст книг не добавлялся; guide содержит только собственные инструкции по использованию приложения.
