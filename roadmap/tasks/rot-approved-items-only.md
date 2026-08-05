# Только одобренные предметы в ROT (rot-approved-items-only)

- **Пункт ТЗ:** уточнение владельца к ROT-EQP-GEAR-01 / ROT-WPN-01 / ROT-ARM-01 / ROT-CLEAN
- **Ветка:** `feature/rot-approved-items-only`
- **Базовая ветка:** `master`
- **PR:** [#162](https://github.com/Giviruk/GenesysForgeClaude/pull/162)
- **Статус:** ✅ Implementation complete; draft PR open, awaiting CI/review

## Контекст

Аудит полного `ItemDef`-каталога обнаружил 15 активных RoT-записей из Genesys Core сверх
утверждённых RoT-манифестов. Причина — прежняя политика `setting: Any → обе системы`, которая
автоматически добавляла в RoT любой Core-предмет. `Backpack` и `Rope` являются исключением: они
прямо входят в принятый RoT gear catalog и карьерные комплекты.

## План выполнения

- [x] Сделать Core-предметы retired в RoT по умолчанию
- [x] Оставить явный whitelist одобренных общих предметов (`backpack`, `rope`)
- [x] Зафиксировать точный активный RoT-набор и сохранность Core-набора интеграционными тестами
- [x] Проверить синхронизацию legacy-строк без удаления данных
- [x] Полные xUnit / Vitest / lint / build проверки пройдены
- [x] Миграция и изменение схемы не требуются
- [x] Copyright-проверка seed/справочников выполнена
- [x] `rot-rules-remediation-progress.md` обновлён
- [x] PR открыт

## Что осталось / блокеры

Реализация и полные локальные проверки завершены; draft PR открыт, остаются CI и review.

## Заметки / решения

- Записи не удаляются физически: RoT-копии получают `Retired=true`, Core-копии остаются активными.
- Custom content не участвует в catalog seed и не затрагивается.
- Services остаются reference rows общего магазина, но не являются `CharacterItem`.
- Проверки: 730 domain + 830 API, 289 frontend, lint и production build — passed.
