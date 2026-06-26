# Структурные модели карьер + стартовое снаряжение (u13-career-models)

- **Roadmap:** U-13 — структурные модели карьер + стартовое снаряжение (см. [unified-roadmap.md](../unified-roadmap.md)); источник GF-007 / Аудит §3.
- **Ветка:** `feature/u13-career-models`
- **Базовая ветка:** `master` (стек пуст — U-01…U-12 слиты).
- **PR:** _(заполняется после открытия)_
- **Статус:** 🚧 In progress

## Контекст

`CareerDef` хранил только карьерные навыки. U-13 делает стартовое снаряжение, деньги и правила карьер
данными. Источник — `_books/genesys_rot_core_careers_ru.csv` (колонки «Стартовое снаряжение RU», «Заметки RU»).
Снаряжение есть у 9 RoT-карьер; Core — без снаряжения (как в Genesys).

Решения с пользователем: (1) деньги бросаются кубами на сервере (1d100); (2) пикер выбора снаряжения при
создании; (3) сущность `CareerRule` сделать сейчас; (4) недостающие предметы добавить в каталог (а не fallback).

## План выполнения

- [x] Domain: `CareerStartingGear`, `CareerRule`, enum `CareerRuleKind`; в `CareerDef` — money-поля + nav `StartingGear`/`Rules`.
- [x] В каталог предметов добавлен «Дорожный набор» (`adventuring-pack`) — всё снаряжение резолвится (генератор без warning'ов).
- [x] Генератор `gen-career-extras-catalog.mjs`: парсинг снаряжения (слоты `;`, выбор `или`, набор `и`/`,`, количества, деньги «серебра»),
      override Рунного мастера («Как маг, но…»), маппинг RU→код предметов; embed `career-extras.catalog.json`.
- [x] `CareerExtrasCatalog.Load()`; `SeedData.SeedCareerExtras` — раскладывает money/gear/rules по карьерам (идемпотентно, полная замена детей).
- [x] `AppDbContext` + `IAppDbContext`: DbSet'ы + конфиг (FK cascade, индекс CareerId); миграция `AddCareerStartingGearAndRules`.
- [x] `CareerDto` (+ `CareerStartingGearDto`/`CareerRuleDto`); `Mappers.ToDto`; `GetReferenceHandler` с Include.
- [x] `CreateCharacterRequest.CareerGearChoices`; `CreateCharacterHandler`: деньги (бросок NdM), фикс-снаряжение в инвентарь, выбор (лениво) + валидация индекса.
- [x] Frontend: типы, `createCharacter` клиент, `CharactersPage` — деньги/снаряжение/правила, пикер вариантов, блокировка submit.
- [x] Тесты: SeedData (gear/money/rules, идемпотентность, резолв кодов), CreateCharacter (фикс/выбор/деньги-диапазон/невалидный индекс), правка теста брони на дельты; Vitest CharactersPage. Api 176 / Domain 65 / front 75 зелёные.
- [x] docs/database.md, docs/domain-model.md обновлены.
- [ ] PR открыт.

## Что осталось / блокеры

- Требуется применить миграцию + перезапуск backend (SeedCareerExtras отрабатывает на старте).

## Заметки / решения

- Карьеры остаются код-сидом; из каталога берутся только extras (money/gear/rules), раскладываются по `CareerDef.Code`.
- 9 gear-карьер CSV (по EN-slug) совпадают с засиженными RoT-кодами (в т.ч. CSV «Друид» = EN Primalist → `rot.career.primalist`).
- Деньги — реальный бросок (`Random.Shared`), поэтому тесты проверяют диапазон.
- Выбор снаряжения на бэке лениво: невыбранный слот пропускается (снаряжение не критично), фронт гейтит для UX.
- Стартовое снаряжение кладётся как `Carried` — не влияет на soak/защиту (только нагрузку); тест брони переписан на дельты от стартового набора.
