# Printable Cards / Export

## 1. Назначение

**Printable Cards / Export** — режим печати и экспорта игровых материалов.

Фича позволяет мастеру и игрокам быстро получить физические или цифровые карточки:

- NPC;
- энкаунтеров;
- магических действий;
- предметов;
- талантов;
- заметок;
- handouts;
- элементов campaign handbook.

В MVP не нужен backend PDF generator. Достаточно качественного print layout во frontend.

---

## 2. Пользовательская ценность

### Для мастера

Мастер может:

- распечатать противников;
- распечатать энкаунтер;
- подготовить handouts;
- держать карточки за столом;
- не открывать несколько экранов во время игры;
- дать игрокам публичные карточки.

### Для игрока

Игрок может:

- распечатать карточки своих талантов;
- распечатать предметы;
- распечатать магические действия;
- иметь быстрый reference за столом.

---

## 3. Типы карточек

## 3.1. Adversary Card

Карточка NPC.

GM-версия содержит:

- имя;
- тип;
- роль;
- характеристики;
- wounds;
- strain;
- soak;
- defense;
- skills;
- talents;
- abilities;
- equipment;
- GM notes;
- tags.

Player-версия содержит:

- имя;
- описание;
- видимые эффекты;
- публичные заметки;
- без скрытых stats, если GM их не открыл.

## 3.2. Encounter Sheet

Карточка или лист энкаунтера.

GM-версия содержит:

- название;
- тип;
- threat level;
- описание для GM;
- описание для игроков;
- цели игроков;
- цели NPC;
- участники;
- осложнения;
- награды;
- заметки;
- initiative slots;
- место для wounds/strain.

Player-версия содержит:

- название;
- player description;
- цели;
- публичные handouts;
- открытые NPC;
- без скрытых данных.

## 3.3. Magic Action Card

Карточка магического действия.

Поля:

- название;
- magic skill;
- dice pool, если связан с персонажем;
- base difficulty;
- total difficulty;
- selected effects;
- advantage/threat spends;
- notes;
- source/page refs.

## 3.4. Item Card

Карточка предмета.

Поля:

- название;
- тип;
- encumbrance;
- price;
- rarity;
- description/safe summary;
- source/page ref.

Для оружия:

- skill;
- damage;
- crit;
- range;
- properties.

Для брони:

- soak bonus;
- melee defense;
- ranged defense;
- encumbrance.

## 3.5. Talent Card

Карточка таланта.

Поля:

- название;
- tier;
- ranked/non-ranked;
- activation;
- safe summary;
- source/page ref;
- заметки;
- effects, если они уже структурированы в системе.

## 3.6. Campaign Handout

Карточка или лист для игроков.

Поля:

- заголовок;
- текст;
- публичное описание;
- NPC или локация;
- слухи;
- цели задания;
- изображение, если позже будет поддержка изображений.

---

## 4. Основные сценарии

## 4.1. GM печатает NPC

1. GM открывает NPC в Adversary Library.
2. Нажимает **Печать**.
3. Выбирает GM version или Player version.
4. Открывается print preview.
5. GM нажимает browser print.

## 4.2. GM печатает энкаунтер

1. GM открывает Encounter Builder.
2. Выбирает энкаунтер.
3. Нажимает **Печать энкаунтера**.
4. Система открывает Encounter Sheet.
5. GM печатает.

## 4.3. Игрок печатает магическую карточку

1. Игрок собирает действие в Magic Builder.
2. Нажимает **Печать карточки**.
3. Получает компактную карточку с итоговой сложностью и выбранными эффектами.

## 4.4. Batch Print

1. Пользователь отмечает несколько карточек.
2. Нажимает **Печать выбранного**.
3. Система открывает print layout с несколькими карточками.
4. Пользователь печатает.

---

## 5. Print Layout

## 5.1. Общие требования

Print layout должен:

- скрывать навигацию;
- скрывать кнопки;
- использовать белый фон;
- быть читаемым на A4;
- не разрывать карточку между страницами;
- поддерживать 1 / 2 / 4 карточки на страницу;
- использовать компактную типографику.

## 5.2. CSS

Использовать `@media print`.

Правила:

- `.no-print { display: none; }`;
- `.print-card { page-break-inside: avoid; }`;
- скрыть topbar;
- скрыть формы;
- скрыть кнопки;
- оставить только контент печати.

## 5.3. Print Preview

Print Preview — отдельный режим в SPA.

Он должен:

- показывать только печатаемый контент;
- иметь кнопку **Печать**;
- иметь кнопку **Назад**;
- поддерживать переключение GM/player version.

---

## 6. Экспорт

## 6.1. Copy as Markdown

Для большинства сущностей нужна кнопка **Скопировать как Markdown**.

## 6.2. Export JSON

Для custom content можно поддержать JSON export.

Ограничение:

- официальный контент экспортируется только как id/source/pageRef/safeSummary;
- не экспортировать длинные official descriptions.

## 6.3. PDF

Backend PDF generation не нужен в MVP.

V2-варианты:

- frontend print to PDF через browser;
- backend PDF generator;
- шаблоны карточек;
- экспорт в zip.

---

## 7. Доступы

| Действие | GM | Игрок |
|---|---:|---:|
| Печатать свои материалы | Да | Да |
| Печатать NPC кампании | Да | Только player-visible |
| Печатать GM-версию NPC | Да | Нет |
| Печатать энкаунтер GM-version | Да | Нет |
| Печатать player-version энкаунтера | Да | Да, если visible |
| Печатать handbook | Да | Да, если опубликован |
| Batch print GM materials | Да | Нет |
| Copy as markdown своих карточек | Да | Да |

---

## 8. UI

## 8.1. Кнопки печати

Кнопки должны быть в:

- AdversaryCard;
- EncounterDetail;
- MagicBuilder result;
- Item detail;
- Talent detail;
- Handbook entry;
- Campaign notes.

## 8.2. Выбор версии

Для материалов с приватными данными:

- GM version;
- Player version.

По умолчанию:

- GM видит GM version;
- игрок видит только player version.

## 8.3. Batch Selection

В библиотеках добавить checkbox selection:

- выбрать несколько NPC;
- выбрать несколько item cards;
- выбрать несколько talents;
- печать выбранного.

В MVP batch print можно сделать только для NPC и Encounter.

---

## 9. MVP

В MVP входит:

- print CSS;
- print preview;
- Adversary Card;
- Encounter Sheet;
- Magic Action Card;
- Item Card;
- Talent Card;
- кнопка browser print;
- GM/player version для NPC и Encounter.

---

## 10. Не входит в MVP

- backend PDF generation;
- drag-and-drop layout editor;
- изображения;
- кастомные темы;
- экспорт в DriveThruRPG-ready PDF;
- сложные шаблоны карточек;
- массовый zip export.

---

## 11. Критерии готовности

Фича готова, если:

- GM может распечатать NPC;
- GM может распечатать Encounter Sheet;
- игрок может распечатать Magic Action Card;
- print layout не содержит навигации;
- карточки не разрываются между страницами;
- приватные данные не попадают в player version;
- official content не экспортируется полным текстом.
