# U-22 · Мотивации и предыстория персонажа

**Статус:** ✅ Done · ветка `feature/u22-motivations-background` (от master) · PR #65
**Roadmap:** [unified-roadmap.md](../unified-roadmap.md) U-22 · Источник: Аудит §2.1

## Зачем
У персонажа нет места для мотиваций (стремление/страх/сильная сторона/слабость) и свободной
предыстории — ключевых для отыгрыша и решений мастера элементов листа Genesys.

## Что сделано

### Backend
- `Character`: nullable-поля `Desire`/`Fear`/`Strength`/`Flaw` (мотивации) + `Background` (предыстория).
  Конфиг в `AppDbContext`: мотивации `HasMaxLength(300)`, предыстория `HasMaxLength(8000)`.
  Миграция `AddCharacterMotivations`.
- `CreateCharacterRequest` / `UpdateCharacterRequest`: опциональные текстовые поля (по умолчанию null).
- `CreateCharacterHandler`: проставляет поля при создании через `Clean` (trim, пустое → null).
- `UpdateCharacterHandler`: семантика null = не трогаем, пустая строка = очистить в null.
- `CharacterSheetDto` + `SheetBuilder`: поля попадают в лист.

### Frontend
- `CharacterSheet` тип + `CharacterBio` (для create/update); `api.createCharacter` принимает `bio`,
  `api.updateCharacter` — те же поля в patch.
- Вкладка **«Образ»** (`BioTab`) на листе: 4 мотивации + textarea предыстории, сохранение через
  `updateCharacter`, кнопка активна только при изменениях.
- Форма создания: свёрнутый `<details>` «Мотивации и предыстория (необязательно)».
- Печать (`CharacterSheetPrint`): секция «Образ персонажа» — непустые мотивации + предыстория
  (`white-space: pre-wrap` для переносов).

## Тесты
- Api `ApiTests.Motivations_CreatedSaved_Updated_AndCleared`: создание с trim, частичный PATCH
  (null не трогает), очистка пустой строкой → null.
- Front `BioTab.test.tsx`: предзаполнение + блокировка кнопки без изменений; сохранение изменённых полей.
- Front `CharacterSheetPrint.test.tsx`: печать секции «Образ персонажа», пустые поля пропускаются.
- Итог: backend Domain 102 / Api 203, frontend 102 — зелёные; `npm run build` (tsc -b) чист.

## DoD
- [x] Мотивации и предыстория сохраняются (create + sheet edit).
- [x] Видны на листе (вкладка «Образ») и в печати (U-04).
