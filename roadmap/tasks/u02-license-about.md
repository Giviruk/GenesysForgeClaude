# Лицензия, авторские права и публичность (u02-license-about)

- **Roadmap:** U-02 — Лицензия, авторские права и публичность (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u02-license-about`
- **Базовая ветка:** `master` (U-01/PR #30 уже слит, открытых PR нет — стека нет)
- **PR:** [#31](https://github.com/Giviruk/GenesysForgeClaude/pull/31)
- **Статус:** 🚧 In progress

## Контекст

Отсутствие `LICENSE` — юридический блокер публичного релиза; нужен дисклеймер о правах FFG/Genesys,
changelog и публичная страница «О проекте».

Решения пользователя:
- Лицензия кода — **Apache-2.0**; контент — отдельный дисклеймер «not affiliated with FFG», без текста книг.
- **Всё в одном PR**: LICENSE + CHANGELOG + дисклеймер + in-app About-страница + футер.
- **FUNDING.yml пока пропускаем** (нет аккаунта/решения) — sponsors-ссылку в футер не добавляем.
- `roadmap/` уже опубликован (новая unified-структура заменяет старые `01..06`) — отдельно восстанавливать не нужно.

## План выполнения

- [x] `LICENSE` — Apache-2.0 (copyright «GenesysForge contributors», 2026)
- [x] `NOTICE` — контент/trademark-дисклеймер (Apache-конвенция)
- [x] `CHANGELOG.md` — формат Keep a Changelog (Unreleased + baseline)
- [x] In-app страница «О проекте» (`/about`), публичная (до auth), с дисклеймером и ссылками
- [x] Глобальный `Footer` (О проекте / Changelog / Исходный код + дисклеймер), на auth-экране и в Shell
- [x] Роутер: добавлен area `about` (`router.ts` + тест `router.test.ts`)
- [x] `README.md`: секция «Лицензия и правовая информация»
- [x] Frontend: lint ✅, build ✅, test ✅ (49 tests), preview-проверка About+footer (logged out/in)
- [ ] Статус U-02 в `unified-roadmap.md` → done после merge
- [ ] PR открыт

## Что осталось / блокеры

- Открыть PR; после merge — U-02 → ✅ Done (PR #N).
- FUNDING.yml + GitHub Sponsors — отложено по решению пользователя (можно отдельной задачей).

## Заметки / решения

- Apache-2.0 покрывает только код; игровой контент — под отдельным дисклеймером (NOTICE/About).
- About сделана публичной (рендерится до проверки токена), чтобы дисклеймер был виден без логина.
- Footer вынесен общим компонентом, переиспользован в `App.tsx` (Shell) и `AuthPage.tsx`.
- Copyright: оригинальных текстов книг не добавлялось.
