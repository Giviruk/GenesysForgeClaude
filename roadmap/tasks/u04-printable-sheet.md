# Полный printable / PDF-friendly лист персонажа (u04-printable-sheet)

- **Roadmap:** U-04 — Полный printable / PDF-friendly лист персонажа (см. [unified-roadmap.md](../unified-roadmap.md))
- **Ветка:** `feature/u04-printable-sheet`
- **Базовая ветка:** `master` (U-03/PR #32 слит, открытых PR нет — стека нет)
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

«Нормальный PDF-экспорт» — боль сообщества. Сейчас печать только для карточек/материалов.
Нужен полный печатный лист персонажа.

Решение по подходу: **переиспользовать существующий `PrintPreview`** (overlay + `body.printing`,
печатает только `.print-area`) + кнопка «Печать листа» в `SheetPage`. Без новых endpoints и без
изменения роутера — консистентно с печатью NPC/карточек. Данные — существующий `CharacterSheet`
(+ `Reference` для RU-имён навыков/предметов, `api.notes(id)` для заметок).

Факты из кода:
- `CharacterSheet`: characteristics, derived (woundThreshold/strainThreshold/soak/melee+rangedDefense/
  encumbranceThreshold/encumbranceLoad/encumbered), skills (SheetSkill: name, characteristic, ranks,
  isCareer, pool), talents (SheetTalent: nameRu, tier, ranked, ranks, activation, description, бонусы),
  heroicAbility (activation/duration/frequency/upgrades + upgradeRank), items (SheetItem: combat/armor).
- Навыки RU — из `reference.skills` (SkillDef.nameRu) по skillDefId; предметы RU — `reference.items`.
- Заметки — отдельный endpoint `api.notes(id)`.

## План выполнения

- [x] Компонент `components/print/CharacterSheetPrint.tsx` — полный лист (инфо, характеристики,
      derived, навыки RU/EN+пул, таланты, героика, инвентарь по состояниям, заметки)
- [x] Кнопка «Печать листа» в `SheetPage` → `PrintPreview` с этим компонентом
- [x] CSS: `.sheet-doc` + `@media print` (`@page A4`, скрыть chrome, page-break-inside: avoid)
- [x] Frontend lint/build/test (49) зелёные; preview-проверка end-to-end (backend in-memory):
      открыт лист, нажата «Печать листа», скриншоты header/характеристики/навыки/инвентарь/заметки,
      без console-ошибок
- [x] docs: current-state/feature-roadmap отметили полный printable лист
- [ ] Статус в `unified-roadmap.md` → done после merge
- [ ] PR

## Что осталось / блокеры

(заполняется по ходу)

## Заметки / решения

- Без backend-изменений и миграций.
- Печать через системный диалог браузера (Ctrl+P / кнопка 🖨), сохранение в PDF.
- Copyright: печатаем пользовательские/структурные данные; book text не добавляем.
