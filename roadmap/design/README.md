# GenesysForge — design inventory for `unified-roadmap.md`

Дата анализа: 2026-06-24.

## Основа текущего стиля

- Тёмная оболочка без бокового меню, рабочая ширина до `70rem`.
- Цвета: `#16131c` фон, `#1f1a28` и `#292235` поверхности, `#3a3148` границы.
- Основной акцент: старое золото `#c8a24a` / `#8d6f2c`.
- Системные акценты: синий Genesys Core и фиолетовый Realms of Terrinoth.
- Типографика: Segoe UI для интерфейса, Georgia для логотипа.
- Геометрия: радиусы 6–10 px, тонкие границы, почти без теней и без градиентов.
- Базовые паттерны: верхние вкладки, панели, компактные таблицы, badges/chips, master-detail.

## Что требуется создать

| Макет | Новые UI-элементы | Задачи roadmap |
|---|---|---|
| 01. App shell | Глобальный поиск, профиль, справка, About/footer, offline indicator | U-02, U-07, U-11, U-21, U-28, U-30 |
| 02. Персонажи | Импорт JSON, preview результата, clone/share/export actions, статусы ссылок | U-03, U-24 |
| 03. Расширенный лист | История XP, мотивации, предыстория, критические ранения, активные эффекты | U-09, U-18, U-22, U-23 |
| 04. Печатный лист | A4/Letter layout, print toolbar, секции полного листа | U-04 |
| 05. Dice & combat roller | Конструктор пула, символы результата, секретный бросок, damage и activation costs | U-08, U-17 |
| 06. Справочник правил | Поиск, фильтры, difficulty/range/symbol spends/crit tables | U-10, U-11 |
| 07. NPC studio | Draft generator, structured attacks, qualities, errors/warnings, bestiary source state | U-14, U-15, U-16, U-19 |
| 08. GM workspace | Read-only player sheet drawer, roll log, critical injuries on participant cards | U-08, U-20, U-23 |
| 09. Homebrew studio | Archetype/career forms, JSON import, pack sharing, per-character toggles | U-12, U-13, U-25, U-26 |
| 10. Account & help | Profile/avatar, change password, help navigation, About/legal | U-02, U-06, U-21, U-30 |

## Задачи без отдельного продуктового макета

- U-01 — только документация.
- U-05 — backend/deploy hardening; UI нужен лишь для статуса недоступности, он покрыт shell.
- U-13 — выбор стартового снаряжения является шагом существующего создания персонажа и покрыт Homebrew/creation patterns.
- U-27 — API versioning, индексы и внешний Scalar/Swagger UI не требуют GenesysForge-компонента.
- U-29 — E2E-тесты, новый UI отсутствует.

## Файлы

- `index.html` — переключаемая галерея всех макетов.
- `gallery.html` — стартовая страница со ссылками на все 10 экранов.
- `styles.css` — изолированный набор стилей, повторяющий текущие tokens приложения.
- `concept-overview.png` — общий AI-концепт направления.
- `*-preview.png` — актуальные рендеры всех 10 экранов в размере `1536×1080` (печатный лист выше из-за полного A4 preview).

Открыть галерею: `gallery.html`. Детальные макеты находятся в `index.html`; допустимые значения `screen`: `shell`, `characters`, `sheet`, `print`, `roller`, `reference`, `npc`, `gm`, `homebrew`, `account`.

## Assumption

Макеты показывают целевое объединение связанных задач, а не обязательную структуру React-компонентов или API. Точные тексты справочных таблиц должны быть написаны как собственные краткие paraphrase и не копировать официальные книги.
