# Исправление прогрессии героических способностей RoT (u31-heroic-ability-rules)

- **Roadmap:** U-31 — Исправление прогрессии героических способностей RoT (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/heroic-ability-rules`
- **Базовая ветка:** `master`
- **PR:** #97
- **Статус:** 🚧 In progress

## Контекст

Текущая реализация ошибочно выдаёт одно стартовое ability point и поддерживает только линейную покупку
Improved/Supreme. По правилам RoT очки начисляются за каждые полные 50 XP сверх стартового XP вида;
доступны универсальные улучшения Duration, Frequency, Story и Secondary Effect.

Справочные материалы: Realms of Terrinoth, раздел Heroic Ability Upgrades (с. 79);
официальное превью FFG «Heroic Feats». Тексты книги не копируются: используются структурные значения
и собственные парафразы.

## План выполнения

- [x] Проверить текущую domain/application/API/frontend реализацию и тесты.
- [x] Сверить начисление очков, стоимости и ограничения улучшений с правилами RoT.
- [x] Расширить domain-модель и persistent model для универсальных улучшений.
- [x] Добавить backend-команду и валидацию покупки/возврата улучшений.
- [x] Обновить DTO/API, импорт/экспорт и frontend-лист.
- [x] Добавить полные private-full парафразы и короткие public-safe описания.
- [x] Обновить xUnit/Vitest тесты.
- [x] Создать миграцию и обновить `docs/database.md`, `docs/domain-model.md`, `docs/api.md`.
- [x] Запустить backend/frontend проверки.
- [x] Copyright-проверка seed/справочников.
- [x] Статус в `unified-roadmap.md` обновлён.
- [x] PR открыт.

## Что осталось / блокеры

Блокеров нет. Draft PR #97 открыт; после review/merge статус U-31 можно перевести в Done.

## Заметки / решения

- `HeroicUpgradeRank` сохраняется как ранг Power (0/1/2) для обратной совместимости.
- Assumption: возврат улучшений разрешён только во время `IsCreationPhase`, как и другие исправления
  стартового листа; после завершения создания покупки постоянны.
- PrivateFull получает полные по смыслу авторские парафразы. Дословный текст книги не хранится.
- Проверки: backend 115 domain + 230 API; frontend lint + 130 Vitest + production build.
