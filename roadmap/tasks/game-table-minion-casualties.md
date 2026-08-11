# Потери миньонов и управление состоянием NPC (game-table-minion-casualties)

- **Пункт ТЗ:** follow-up к GEN-GT-NPC; ограниченный runtime-срез ROT-MIN-02/04
- **Ветка:** `feature/game-table-minion-casualties`
- **Базовая ветка:** `master`
- **PR:** [#169](https://github.com/Giviruk/GenesysForgeClaude/pull/169)
- **Статус:** ✅ Done

## Контекст

После слияния PR #168 пользователь уточнил, что групповые навыки должны учитывать не исходное,
а оставшееся после ран число миньонов. В открытом статблоке также нужны GM-действия для ран и
усталости. Реализация использует существующий общий tracker и не меняет persistent model.

## План выполнения

- [x] Добавить авторитетный backend-расчёт `remainingCount` и индивидуального WT
- [x] Закрепить строгие casualty boundaries domain/API-тестами
- [x] Пересчитывать пулы по оставшимся миньонам и блокировать броски пустой группы
- [x] Показывать `осталось/было` без дублирования старого `×N`
- [x] Добавить GM-действия `±1` для ран и доступной NPC усталости
- [x] Обновить API-документацию
- [x] Пройти Vitest, xUnit, ESLint и production build
- [x] Подтвердить отсутствие миграции, seed и copyright-изменений
- [x] Открыть draft PR

## Что осталось / блокеры

Новый draft PR открыт: исходный PR #168 уже был слит до follow-up. Блокеров нет.

## Заметки / решения

- Backend отдаёт response-only `remainingCount` и `perMemberWoundThreshold`; request shape не менялся.
- Неоднозначный legacy snapshot (`total WT % count != 0`) не интерпретируется автоматически.
- Это не заявляет полноту ROT-MIN-02/06: persistent monotonic casualty counters, audited restore,
  idempotent damage operations и отдельная миграция остаются в `rot-runtime-out-of-scope.md`.
- В текущем согласованном с пользователем срезе остаток непосредственно зависит от текущих ран;
  уменьшение ран через GM-кнопку может вернуть число действующих миньонов.
- Проверки: Vitest 308, domain 740, API 838, ESLint и production build — успешно.
