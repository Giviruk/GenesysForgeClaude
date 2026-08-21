# Уточнить описание «Взрывного снаряда» (feature/fix-explosive-missile-description)

- **Пункт ТЗ:** ROT-EQP-ATT-02 — полный fantasy/RoT weapon attachment catalog (объём — [rot-rules-remediation-tasks.md](rot-rules-remediation-tasks.md))
- **Ветка:** `feature/fix-explosive-missile-description`
- **Базовая ветка:** `master`
- **PR:** [#216](https://github.com/Giviruk/GenesysForgeClaude/pull/216)
- **Статус:** 🚧 In progress

## Контекст

Описание встроенного улучшения `Explosive Missile` не показывало рейтинг качества Blast,
хотя структурный эффект уже задаёт значение 5 и таблица ROT указывает Blast 5.

## План выполнения

- [x] Уточнить русское и английское описание улучшения до Blast 5.
- [x] Добавить проверку описания в каталогные тесты.
- [x] Выполнить тесты и copyright-проверку seed-данных.
- [ ] Строка пункта в `rot-rules-remediation-progress.md` обновлена.
- [x] PR открыт.

## Что осталось / блокеры

Открыть PR; родительский пункт ROT-EQP-ATT-01…03 остаётся частично выполненным до
завершения остальных работ по каталогу улучшений.

## Заметки / решения

Число 5 сверено со структурным эффектом `SetQualityAtLeast(blast, 5)` и локальной
парафразой ROT в `rot-rules-remediation-tasks.md`; оригинальный текст книги не добавляется.
