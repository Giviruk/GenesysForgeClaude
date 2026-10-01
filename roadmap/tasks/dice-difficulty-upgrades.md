# Усиление сложности в дайсроллере (dice-difficulty-upgrades)

- **Пункт ТЗ:** вне ROT/GEN — исправление ошибки сборки пула (найдено при проверке дайсроллера)
- **Ветка:** `feature/dice-difficulty-upgrades`
- **Базовая ветка:** `master`
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

Длительные эффекты критических травм дают «усиления сложности» (`difficultyUpgrades` в
`CharacterSkillDto`). Фронтенд подставлял их в пул как отдельные красные кости
(`challenge: difficultyUpgrades`), а по правилам усиление превращает фиолетовую кость сложности
в красную; если фиолетовых не осталось — добавляется фиолетовая. Из-за этого проверка с
усилением получала лишнюю кость: 3 Ability против сложности 2 с одним усилением давали
2P+1R (41.8 % успеха) вместо 1P+1R (53.0 %).

Затронутые места:
- `frontend/src/utils/diceRoller.ts` — чистые правила пула.
- `frontend/src/components/DiceRoller.tsx` — роллер: базовый пул + усиления → итоговый пул.
- `frontend/src/dice-roller-store.ts`, `frontend/src/dice-roller-context.tsx` — заявка на бросок.
- `frontend/src/components/SheetTab.tsx`, `InventoryTab.tsx`, `MagicBuilder.tsx` — открытие роллера.
- `frontend/src/components/DicePoolView.tsx` — статичный показ пула (лист, инвентарь, магия, печать).

## План выполнения

- [ ] `applyDifficultyUpgrades(pool, upgrades)` в `diceRoller.ts` + тесты
- [ ] `DiceRoller`: проп `difficultyUpgrades`, счётчик усилений с +/−, бросок и лог по итоговому пулу, показ итогового пула
- [ ] `DiceRollerRequest` (roll/combat/magic): поле `difficultyUpgrades`, проброс в `DiceRoller`
- [ ] Вызовы в `SheetTab`, `InventoryTab`, `MagicBuilder`: передавать усиления отдельно, а не красными костями
- [ ] `DicePoolView`: показывать усиление маркером, а не красной костью (база сложности на листе неизвестна)
- [ ] Тесты Vitest (`diceRoller.test.ts`, `DiceRoller.test.tsx`), `npm run lint`, `npm test`, `npm run build`
- [ ] PR открыт

## Что осталось / блокеры

—

## Заметки / решения

- Правило (Genesys Core, «Upgrading and downgrading dice»): каждое усиление сложности превращает
  одну кость Difficulty в Challenge; если Difficulty не осталось, добавляется кость Difficulty,
  и следующее усиление превращает уже её.
- Усиления применяются в момент броска к пулу, который собрал игрок/мастер: базовую сложность
  проверки приложение не знает, поэтому заранее «запечь» усиление в пул нельзя.
- Бэкенд не меняется: он по-прежнему отдаёт число усилений, интерпретация — на клиенте, где
  известна сложность конкретного броска.
- В лог стола (`poolJson`) пишется итоговый пул — то, что реально бросили.
