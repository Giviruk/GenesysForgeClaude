# Runebound shards и обязательный инструмент Runes (rot-mag-11-runebound-shards)

- **Roadmap:** ROT-MAG-11 из [rot-rules-remediation-tasks.md](rot-rules-remediation-tasks.md)
- **Ветка:** `feature/rot-mag-11-runebound-shards`
- **Базовая ветка:** `master`
- **PR:** [#125](https://github.com/Giviruk/GenesysForgeClaude/pull/125)
- **Статус:** 🚧 In progress

## Контекст

В каталоге уже есть 17 runebound shards, но они являются обычным gear с
`Price=0`, `Rarity=1`, неполными описаниями и без структурной implement-механики.
Нужно сделать shard обязательным единственным инструментом для Runes, применить
его бесплатные/обязательные эффекты в Magic Builder, исправить экономику и вынести
руны в отдельную вкладку магазина.

Исполняющие encounter-эффекты остаются вне текущего scope согласно
[rot-runtime-out-of-scope.md](rot-runtime-out-of-scope.md). Их механика должна быть
сохранена структурно и полностью описана, но приложение не должно изображать её
автоматически исполненной.

## План выполнения

- [x] Добавить доменную модель и точный manifest 17 runebound shards.
- [x] Добавить typed implement-эффекты, mandatory/free additions и flat difficulty reductions.
- [x] Сделать `Price`/`Rarity` nullable, добавить признаки purchase/sale и неразрушающую миграцию.
- [x] Добавить instance-конфигурацию Lesser Rune и сохранение в duplicate/export/import.
- [x] Передать shards в DTO листа и интегрировать обязательный picker в Magic Builder.
- [x] Сбрасывать невалидный shard при смене навыка, состояния или инвентаря.
- [x] Вынести runebound shards в отдельную вкладку магазина.
- [x] Заполнить механически полные RU/EN-парафразы и PublicSafe summaries.
- [x] Обновить `docs/database.md`, API/модель при изменении контрактов и файл прогресса.
- [x] Добавить/обновить xUnit и Vitest-тесты.
- [x] Проверить миграцию на непустой БД.
- [x] Выполнить copyright-проверку seed/справочников.
- [x] Запустить backend tests, frontend lint/test/build и E2E smoke.
- [x] Открыть PR.

## Что осталось / блокеры

Реализация и проверки завершены, PR #125 открыт. Блокеров нет.

## Заметки / решения

- Persistent spell preset `Not found in current codebase`; инвалидируется только
  текущий выбор Magic Builder.
- `Assumption`: отдельная GM-страница Magic Builder остаётся справочным preview и
  явно не заявляет проверку конкретного inventory instance.
- Encounter activation, Story Points, раны, состояния, длительности и
  `once/encounter` не входят в эту ветку.
- Миграция проверена на PostgreSQL 17 поверх предыдущей версии: legacy-строка
  `Quantity=3` превратилась в три строки `Quantity=1`, исходный ID сохранён,
  суммарное количество не изменилось.
- Проверки: backend 633 domain + 679 API; frontend 202 Vitest, lint и production build.
  E2E smoke покрыт API/UI интеграционными тестами; отдельного браузерного E2E suite для
  этого маршрута `Not found in current codebase`.
