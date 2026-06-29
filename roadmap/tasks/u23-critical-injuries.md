# U-23 · Критические ранения

**Статус:** 🚧 In progress · ветка `feature/u23-critical-injuries` (стек поверх U-22 / PR #65) · PR #66
**Roadmap:** [unified-roadmap.md](../unified-roadmap.md) U-23 · Источник: Аудит §2.2 + §8

## Зачем
В Genesys критические ранения — постоянное состояние персонажа (до лечения, каждое даёт +10
к будущим броскам). Их негде вести: ни на листе, ни за столом.

## Дизайн-решение
Две независимые, самодостаточные стороны (чтобы не протаскивать крит-данные через 11 вызовов
`GameTableMapper.ToDto`):
- **Лист персонажа** — полный список `CharacterCriticalInjury` (снимок названия/тяжести),
  привязка к строке таблицы крит-ранений из U-11 (`RuleCode` → `RuleTableEntry`), add/remove.
- **Стол** — счётчик `GameParticipant.CriticalInjuries` (int) для быстрого учёта за столом;
  засевается из числа критов персонажа при добавлении участника, дальше GM/игрок крутит вручную.

## Что сделано

### Backend
- Сущность `CharacterCriticalInjury` (Id/CharacterId/RuleCode?/NameRu/Severity?/RollResult?/Notes?/CreatedAt);
  `Character.CriticalInjuries`; конфиг + индекс по `CharacterId`; включена в `CharacterLoader.LoadWithRelationsAsync`.
- `GameParticipant.CriticalInjuries` (счётчик), в `GameParticipantDto` и `UpdateParticipantRequest`;
  `UpdateParticipant` (ApplyVitals — игрок может крутить и свои), `ParticipantFactory.FromCharacterAsync`
  засевает из `c.CriticalInjuries.Count`, `GameTableMapper.ToDto` отдаёт поле.
- `AddCriticalInjuryCommand`/Handler: по `RuleCode` снимает название/тяжесть из таблицы U-11
  (`RuleTableKind.CriticalInjury`), иначе требует ручное `NameRu`; пустые поля → null;
  `db.CharacterCriticalInjuries.Add` + nav (как `AddItemHandler`). `RemoveCriticalInjuryCommand`/Handler.
- Endpoints `POST /api/characters/{id}/critical-injuries`, `DELETE …/{injuryId}`; DI-регистрация;
  `AddCriticalInjuryRequest` в `Dtos`; `CharacterSheetDto.CriticalInjuries` + `SheetBuilder`.
- Миграция `AddCriticalInjuries` (таблица + столбец счётчика на `GameParticipants`).

### Frontend
- Типы `CriticalInjury`, `CharacterSheet.criticalInjuries`, `GameParticipant.criticalInjuries`,
  `UpdateParticipantRequest.criticalInjuries`; `api.addCriticalInjury`/`removeCriticalInjury`.
- `CriticalInjuriesSection` на вкладке «Лист» (под производными): список с «Снять» + форма-пикер
  из таблицы U-11 (optgroup по тяжести) / ручной ввод + бросок d100 + заметки; показ парафраза эффекта.
- Печать (`CharacterSheetPrint`): секция «Критические ранения».
- Game Table: счётчик критов на карточке участника с −/+ (для тех, кто может править вайталы); CSS.

## Тесты
- Api `CriticalInjuries_AddFromTable_AddManual_AndRemove`, `CriticalInjury_UnknownRuleCode_Rejected`;
  GameTable `ParticipantCritCounter_SeedsFromCharacter_AndEditable`.
- Front `CriticalInjuriesSection.test.tsx` (список/снятие, добавление по коду), расширен
  `CharacterSheetPrint.test.tsx`.
- Итог: backend Domain 102 / Api 206, frontend 105 — зелёные; `npm run build` (tsc -b) чист.

## DoD
- [x] Криты добавляются/снимаются (лист).
- [x] Видны на листе и за столом (счётчик участника); ссылаются на таблицу U-11.
