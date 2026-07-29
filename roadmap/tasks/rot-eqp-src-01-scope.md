# Закрыть ROT-EQP-SRC-01 по принятому объёму (rot-eqp-src-01-scope)

- **Roadmap:** ROT-EQP-SRC-01 (см. [rot-rules-remediation-tasks.md](rot-rules-remediation-tasks.md))
- **Ветка:** `feature/rot-eqp-src-01-scope`
- **Базовая ветка:** `feature/gen-shop-01-general-store` (stacked поверх PR #132)
- **PR:** [#133](https://github.com/Giviruk/GenesysForgeClaude/pull/133)
- **Статус:** ✅ Scope complete

## Контекст

По решению пользователя `ROT-EQP-SRC-01` считается выполненным в текущем объёме. Существующие
источники записей и действующий приоритет Core/RoT/official errata принимаются как достаточные.
Архивная расширенная модель provenance больше не является обязательной доработкой этого пункта.

## План выполнения

- [x] Зафиксировать принятое завершение `ROT-EQP-SRC-01`.
- [x] Сохранить исходные расширенные требования как архив аудита.
- [x] Исключить расширенную provenance-модель из списка незавершённой работы.
- [x] Обновить общий progress-файл.
- [x] Тесты не требуются: код приложения не менялся.
- [x] Миграция не требуется: persistent model не менялась.
- [x] Copyright-проверка не требуется: seed и книжные тексты не менялись.
- [x] PR открыт.

## Что осталось / блокеры

Ничего в рамках принятого объёма. Код приложения не меняется.

## Заметки / решения

- Отдельные поля `BookCode`, edition/language, printed page range, section, authority и errata
  version не требуются для закрытия пункта.
- Это решение не отменяет базовый принцип: совместимые Core rules допустимы, если они не
  противоречат RoT; RoT override и подходящая official errata имеют приоритет.
