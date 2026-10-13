# Приёмка редизайна v5

Дата: 13.10.2026. Источник требований: [ТЗ v5](../redesign_v5_tz/TZ_redesign_v5.md).
Реализован весь стек GEN-V5-01–12, включая GEN-V5-03b. Код находится в
`feature/gen-v5-12-cleanup-qa`; [итоговый план](../roadmap/tasks/gen-v5-12-cleanup-qa.md).
PR #281–293 открыты для ревью и не слиты. Приёмка относится к совокупному результату стека.

## Результат

Единая светлая тема применена к каркасу сервиса, общим элементам, персонажам и всем
вкладкам листа, всем разделам кампании, бестиарию и редакторам NPC, библиотеке,
магазину, входу/регистрации, профилю, справочнику, справке, публичным страницам и
листу по ссылке, роллеру и баннеру аналитики. Шрифты локальные; старые Cinzel и
Spectral удалены. Плашки, рамки, штрихованные заголовки и смысловые цвета следуют ТЗ.

Покупки, возвраты, формулы, ownership, API и маршруты используют существующий код.
Backend, seed, migrations, зависимости и конфигурация deployment не менялись.
Из инфраструктуры frontend изменены только разрешённые ТЗ цвета PWA и иконки.
Пользовательские изменения из исходного `git status` не включены в коммиты.

## Проверки

| Проверка | Результат |
|---|---|
| `npm --prefix frontend run lint` | Пройдена |
| `npm --prefix frontend test -- --maxWorkers=2` | 80 файлов, 652 теста пройдены |
| `npm --prefix frontend run build` | Пройдена, включая PWA |
| Playwright `layout.spec.ts`, `bestiary.spec.ts` | 27 проверок пройдены |
| Smoke: создание, XP/refund, экипировка, кампании, NPC, энкаунтер, стол, магия, JSON, восстановление пароля, RU/EN | 5 проверок пройдены |
| Smoke пользовательского контента GEN-CONTENT-01 | 1 проверка пройдена |
| Smoke `E2E_CONTENT_MODE=PublicSafe` | 1 проверка пройдена |
| 13 разделов × 1440/1240/1024/768/375 px | 65 измерений: горизонтальное переполнение страницы 0 px |
| Исходные `@media print` в обоих CSS | Сохранены; добавлен блок, восстанавливающий палитру символов из master |
| Печатные React-компоненты | Diff относительно master отсутствует |
| `git diff --check`, поиск старых цветов/токенов/шрифтов из §9 | Пройдены |

Браузерные тесты дополнительно проверяют RU/EN на 320/390/768/1280/1440 и
бестиарий на семи ширинах, переключение режима редактора, сохранность введённых
данных, Back/Forward, повторное добавление талантов, пробелы между элементами,
место для стрелки селектов и клавиатурную доступность. Визуально проверены
основной лист, таланты, инвентарь, магия, героика, улучшения/ремесло, транспорт,
образ/приватные заметки и история; кампании, хроника, энкаунтеры, стол, контент,
настройки, черновик/ручной редактор NPC, библиотека/редактор, магазин/диалог и
служебные экраны. Полный Vitest ограничен двумя workers, чтобы исключить
наблюдавшиеся при неограниченном параллелизме timeout в существующих тестах.

Для QA использовался временный локальный API в режиме PublicSafe и отдельная БД
compose-проекта `genesys-v5-qa`. Персонажи, кампания и собственные тексты на снимках —
тестовые данные, в приложение они не добавлены. Проверена загрузка кириллицы в
новых шрифтах. Сумма UI WOFF2 — 410,396 байт (~401 КиБ), glyph — ещё 4,268 байт.

## Снимки до и после

Десктопные снимки — 1240×900, мобильные — 375×900. «До» снято с актуального
master, «после» — с итогового frontend, на тех же тестовых данных.

| Экран | До, 1240 | После, 1240 | До, 375 | После, 375 |
|---|---|---|---|---|
| Персонажи | [До](assets/redesign-v5/characters-before-1240.jpg) | [После](assets/redesign-v5/characters-after-1240.jpg) | [До](assets/redesign-v5/characters-before-375.jpg) | [После](assets/redesign-v5/characters-after-375.jpg) |
| Лист | [До](assets/redesign-v5/sheet-before-1240.jpg) | [После](assets/redesign-v5/sheet-after-1240.jpg) | [До](assets/redesign-v5/sheet-before-375.jpg) | [После](assets/redesign-v5/sheet-after-375.jpg) |
| Кампания | [До](assets/redesign-v5/campaign-before-1240.jpg) | [После](assets/redesign-v5/campaign-after-1240.jpg) | [До](assets/redesign-v5/campaign-before-375.jpg) | [После](assets/redesign-v5/campaign-after-375.jpg) |
| Игровой стол | [До](assets/redesign-v5/table-before-1240.jpg) | [После](assets/redesign-v5/table-after-1240.jpg) | [До](assets/redesign-v5/table-before-375.jpg) | [После](assets/redesign-v5/table-after-375.jpg) |
| Бестиарий | [До](assets/redesign-v5/bestiary-before-1240.jpg) | [После](assets/redesign-v5/bestiary-after-1240.jpg) | [До](assets/redesign-v5/bestiary-before-375.jpg) | [После](assets/redesign-v5/bestiary-after-375.jpg) |
| Магазин | [До](assets/redesign-v5/shop-before-1240.jpg) | [После](assets/redesign-v5/shop-after-1240.jpg) | [До](assets/redesign-v5/shop-before-375.jpg) | [После](assets/redesign-v5/shop-after-375.jpg) |
| Хроника | [До](assets/redesign-v5/chronicle-before-1240.jpg) | [После](assets/redesign-v5/chronicle-after-1240.jpg) | — | — |
| Энкаунтер | [До](assets/redesign-v5/encounter-before-1240.jpg) | [После](assets/redesign-v5/encounter-after-1240.jpg) | — | — |
| Магия | [До](assets/redesign-v5/magic-before-1240.jpg) | [После](assets/redesign-v5/magic-after-1240.jpg) | — | — |
| Библиотека | [До](assets/redesign-v5/library-before-1240.jpg) | [После](assets/redesign-v5/library-after-1240.jpg) | — | — |

Дополнительно: [навыки](assets/redesign-v5/sheet-skills-after-1240.jpg),
[таланты](assets/redesign-v5/sheet-tab-0-after-1240.jpg),
[инвентарь](assets/redesign-v5/sheet-tab-1-after-1240.jpg),
[героика](assets/redesign-v5/sheet-tab-2-after-1240.jpg),
[улучшения](assets/redesign-v5/sheet-tab-3-after-1240.jpg),
[транспорт](assets/redesign-v5/sheet-tab-4-after-1240.jpg),
[образ](assets/redesign-v5/sheet-tab-5-after-1240.jpg),
[история](assets/redesign-v5/sheet-tab-6-after-1240.jpg),
[редактор библиотеки](assets/redesign-v5/library-editor-after-1240.jpg),
[диалог покупки](assets/redesign-v5/shop-dialog-after-1240.jpg),
[вход](assets/redesign-v5/auth-after-1240.jpg),
[мобильный вход](assets/redesign-v5/auth-after-375.jpg),
[публичная страница](assets/redesign-v5/public-after-1240.jpg).

Открыты и визуально проверены предпросмотры печати
[листа](assets/redesign-v5/print-sheet-preview.jpg),
[предмета](assets/redesign-v5/print-item-preview.jpg),
[таланта](assets/redesign-v5/print-talent-preview.jpg),
[магии](assets/redesign-v5/print-magic-preview.jpg),
[NPC](assets/redesign-v5/print-npc-preview.jpg) и
[энкаунтера](assets/redesign-v5/print-encounter-preview.jpg).
У NPC и энкаунтера проверено переключение мастер/игрок; приватный текст
энкаунтера скрывается в версии игрока. В исходные шаблоны печати правки не внесены.

## Расхождения с макетами и решения

- Макеты находятся в `redesign_v5_tz/genesys-core/project`, а не прямо в `project`.
  Использован предоставленный HTML `Genesys v5 - Страницы.dc.html` и ТЗ.
- Genesys Glyphs & Dice уже был в master в локальном MIT-licensed WOFF2.
  Он повторно используется с классом `.glyph`; дубликат TTF из макета не добавлен.
- Сохранены существующие поля, фильтры, кнопки создания/клонирования/удаления,
  состояния экипировки, damage/repair, цены и ограничения. Поэтому насыщенные
  карточки и мобильные экраны иногда выше статических примеров. Google-кнопка
  сохраняет официальный брендинг.
- Данные макетов не переносились: XP, цены, материалы, редкость, NPC-атаки и
  пороги отображаются из текущего кода/API. Нативные селекты сохранены.
- `Not found in current codebase`: поиск в каталоге талантов, автоматическая
  кнопка броска критического ранения и отдельные сводные XP-счётчики из макета.
  Их функциональность не добавлена; существующие фильтры и формы оформлены v5.
- `GameParticipant` не содержит `npcKind`. `Assumption`: цвет участника на столе
  определяется существующим `participantType`; имена NPC не используются для
  догадок о типе. Для карточек NPC доступный `kind` имеет точный тон из ТЗ.
- Общая сборка магии использует фиолетовую тему из экрана 02b; отдельные школы
  сохраняют собственные тона. Неполные/неизвестные метаданные получают серый тон.
- Старые печатные правила/шаблоны сохранены. Управление предпросмотром оформлено
  светлой панелью v5; новые глобальные локальные шрифты используются и при
  наследовании типографики печатными компонентами. Ревью выявило также смену
  цветов символов через базовые правила; отдельный `@media print` восстанавливает
  палитру master для костей и результатов, проверенную в Chromium.
- Белый текст на оранжевых primary-кнопках оставлен согласно явному решению ТЗ.
  Тёмные варианты текста применяются в смысловых мягких тегах и обычных полях.

## Стек PR

Слои сливаются снизу вверх; общий прогресс остаётся `[~]` до слияния.
Исправления footer, селектов, степперов и ожиданий героики перенесены
из финальной QA в соответствующие нижние слои. Бестиарий и диалог магазина
также содержат исправления приёмки в своих PR. Ветки обновлены последовательно
через merge базы стека, без force-push; итоговый frontend сохранён без изменений.
CI повторно запущен для обновлённых веток.

| Задача | PR |
|---|---|
| GEN-V5-01: токены и шрифты | [#281](https://github.com/Giviruk/GenesysForgeClaude/pull/281) |
| GEN-V5-02: каркас | [#282](https://github.com/Giviruk/GenesysForgeClaude/pull/282) |
| GEN-V5-03: примитивы | [#283](https://github.com/Giviruk/GenesysForgeClaude/pull/283) |
| GEN-V5-03b: glyph | [#284](https://github.com/Giviruk/GenesysForgeClaude/pull/284) |
| GEN-V5-04: персонажи | [#285](https://github.com/Giviruk/GenesysForgeClaude/pull/285) |
| GEN-V5-05: основной лист | [#286](https://github.com/Giviruk/GenesysForgeClaude/pull/286) |
| GEN-V5-06: таланты, инвентарь, магия | [#287](https://github.com/Giviruk/GenesysForgeClaude/pull/287) |
| GEN-V5-07: остальные вкладки | [#288](https://github.com/Giviruk/GenesysForgeClaude/pull/288) |
| GEN-V5-08: кампании | [#289](https://github.com/Giviruk/GenesysForgeClaude/pull/289) |
| GEN-V5-09: бестиарий | [#290](https://github.com/Giviruk/GenesysForgeClaude/pull/290) |
| GEN-V5-10: библиотека и магазин | [#291](https://github.com/Giviruk/GenesysForgeClaude/pull/291) |
| GEN-V5-11: служебные экраны | [#292](https://github.com/Giviruk/GenesysForgeClaude/pull/292) |
| GEN-V5-12: итоговая QA | [#293](https://github.com/Giviruk/GenesysForgeClaude/pull/293) |

Copyright: оригинальные тексты книг, описания из макетов и новые seed-данные
не добавлены. Шрифты поставляются с лицензиями OFL/MIT; на снимках — текущие
PublicSafe-данные и собственный тестовый контент.

Приёмка после 12 замечаний ревью #293: [исправления, регрессии и новые снимки](redesign-v5-review.md).
