# Итерация: контент RU/ENG, таланты, ownership, магия, NPC/campaign UI (content-npc-magic-ux-polish)

- **Roadmap:** вне U-нумерации — итерация по brief [docs/claude-fable-5-iteration-brief.md](../../docs/claude-fable-5-iteration-brief.md)
- **Ветка:** `feature/content-npc-magic-ux-polish`
- **Базовая ветка:** `master` (открытых PR нет)
- **PR:** #<номер> (после создания)
- **Статус:** 🚧 In progress

## Контекст

Одна итерация / один PR по brief: RU/ENG отображение навыков и снаряжения, категории талантов по смыслу, аудит user-scoped custom content, ограничение сложности магии ≤ 5, compact tags/chips для эффектов заклинаний, перенос нереализованных решений из HTML-прототипов (`docs/*.html`).

## Аудит HTML-прототипов

- [x] `docs/gm-dashboard-prototype.html` — **Mostly already implemented** в `CampaignOverview` (frontend/src/pages/CampaignsPage.tsx): alert на криты, карточки PC с барами и XP, текущая сцена, сюжетные очки (pips), инициатива, заметки, статистика группы. Осталось перенести: перенос очка «мастер → игрокам» в overview (в Game Table уже есть обе стороны).
- [x] `docs/campaign-encounters-prototype.html` — **Partially implemented**. Есть: список, редактор с GM/player полями, участники, запуск (replace/append). Перенести: статистическую полосу, тулбар с поиском/фильтрами, master-detail «очередь сцен + детали», copy-grid «Игрокам/Мастеру/Цели», таблично-плотный список участников, быструю сборку из бестиария, панель заметок. НЕ возвращать блоки «Готовность» / «Проверка перед стартом» (убраны ранее осознанно). Инициатива внутри энкаунтера не переносится — инициатива живёт в сцене Game Table (конфликт с текущим flow).
- [x] `docs/campaign-game-table-prototype.html` — **Partially implemented**. Есть: сцена/раунд/ход, инициатива, участники, броски через правый dice roller sidebar (openRoller), заметки. Перенести: понятный блок сюжетных очков (pips + обе стороны переноса, как в overview), блок «Текущий ход», компактную полосу состояния сцены, range board (см. ниже).
- [x] `docs/range-band-tracker-prototype.html` — **Not implemented**. Реализуем внутри Game Table как рабочий инструмент: зоны Engaged/Short/Medium/Long/Extreme, токены участников сцены, перемещение (кнопки + drag), лог перемещений. Persistence для зон в модели нет — реализуем как локальный UI state (отмечено осознанно; серверная модель не меняется, миграция не нужна).
- [x] `docs/npc-quick-draft-prototype.html` — **Partially implemented** (`QuickDraftForm` в NpcsPage.tsx — компактная модалка без preview). Перенести: ролевые карточки с описанием профиля, «Профиль: …» (приоритет характеристик/стартовый набор), выбор типа/силы карточками, live preview статблока, блок «Будет добавлено» (навыки/предметы/способности от роли), RU/ENG display. Для preview добавлен endpoint `POST /api/npcs/quick-draft/preview` (генерация без сохранения).
- [x] `docs/npc-create-prototype.html` — **Partially implemented** (`NpcEditor` — длинная модалка). Перенести: секционную структуру (основа / характеристики+пороги / навыки+атаки / контент+теги), правую сводку + live card preview, нормальные отступы полей/селектов, отсутствие слипшихся helper text.

## Аудит данных и контрактов

- `SkillDef`/`ItemDef`/`TalentDef`/`Spell` DTO уже несут `name`+`nameRu` (+`safeDescription`) — второй источник истины не нужен; RU/ENG делается на фронте единым helper.
- Категории талантов живут только в `talents.catalog.json` (120 шт., все категоризированы) — правки категорий не требуют миграции (seed идемпотентен).
- Custom content: `OwnerUserId = command.UserId` во всех Create*, Update*/Delete* фильтруют по `OwnerUserId == UserId`, `GetReferenceHandler` показывает `OwnerUserId == null || == userId` (+ видимость homebrew-паков). **Привязка custom reference content к персонажу: Not found in current codebase.** `HomebrewPackCharacters` — это переключатели видимости пака per-character (by design), не ownership. Изменение модели/миграция не требуются.
- Магия: backend `Features/Spells` — только `GetSpellsQuery` (read-only), save flow магического билда **Not found in current codebase** → cap 5 реализуется в UI + общий helper + тесты; серверная валидация неприменима (билд не сохраняется и не передаётся на backend).
- NPC: `QuickDraftNpcHandler` персистит сразу; для live preview добавляем non-persisting preview endpoint, переиспользуя `NpcDraftGenerator` + `ApplyCatalogLoadout`.

## План выполнения

- [x] Подготовка: AGENTS.md, ai-context.md, git fetch, `gh pr list` (открытых PR нет), ветка от master
- [x] Аудит данных и контрактов + прототипов (миграция НЕ нужна)
- [x] П.2: категории талантов — 7 переносов general→combat (zakalennyy, krepost, vynoslivost, neukrotimyy, podskok, tumble, geroicheskaya-volya — выживаемость/криты/бой) + `SyncTalentCategories` в SeedData (аддитивный SeedMissing не обновляет существующие строки) + тест
- [x] П.1: helpers `secondaryName`/`dualName` в labels.ts; применены: SheetTab, InventoryTab (магазин+инвентарь), TalentsTab, ReferencePage (nameEn правил), NpcsPage (селекты навыков/талантов/снаряжения/магшкол, статблок, gear), CharactersPage (выбор навыков), CustomTab, печать (лист/карточка предмета); MagicBuilder/SpellsTab уже показывали RU·EN
- [x] П.6: helpers `spellDifficulty`/`wouldExceedSpellCap`/`MAX_SPELL_DIFFICULTY` + блокировка добавления эффектов в MagicBuilder + unit/component тесты. Backend save flow магии отсутствует (Not found in current codebase) — cap серверно неприменим
- [x] П.11: доп. эффекты — chips (доступен/выбран/заблокирован с причиной в title), summary выбранных с ×, сворачиваемые описания (`<details>`), warn-бейдж потолка
- [x] П.12: overview — перенос очка в обе стороны (⇄ Мастеру / ⇄ Игрокам)
- [x] П.12: encounters — strip состояния, тулбар (поиск/тип/сложность/видимость), master-detail «очередь+деталь», copy-grid (игрокам/мастеру/цели/осложнения/награды), таблица участников, панель запуска, быстрая сборка из бестиария. Блоки «Готовность»/«Проверка перед стартом» не возвращались; инициатива остаётся в Game Table
- [x] П.12: game table — сюжетные очки pips-блоком (обе стороны переноса), имя текущего участника в шапке, RangeBandTracker (5 зон, drag&drop + ▲/▼, локальный лог, сворачивается; локальный UI state)
- [x] П.12: quick draft NPC — ролевые карточки с подсказками, профиль роли (стиль/приоритет характеристик), карточки типа/силы, live preview через новый `POST /api/npcs/quick-draft/preview` (генерация без сохранения), блок «Будет добавлено», RU/ENG в селектах
- [x] П.12: ручной NPC editor — 4 секции (основа / характеристики и пороги / навыки и атаки / контент), правая сводка-тайлы + live card preview, широкая модалка без слипшихся полей
- [x] Тесты обновлены: labels (dualName/cap), MagicBuilder (блокировка потолка), CharactersPage (RU/ENG чипы), backend: QuickDraftPreview (не персистит), TalentCategories sync
- [x] Миграция НЕ требуется (persistent model не меняется) → `docs/database.md` без изменений; `docs/api.md` дополнен preview-endpoint
- [x] Copyright-проверка: тексты талантов/эффектов не менялись, только структурное поле category; новые UI-строки — собственные
- [x] `dotnet test` (318 passed), `npm run lint` (чисто), `npm test` (121 passed), `npm run build` (ок)
- [x] Browser QA desktop/mobile (docker compose + vite dev): магия (cap 5, chips, summary, блокировка с причиной), quick draft (live preview, роль→стиль/магшкола/способность, RU/ENG), NPC editor (секции, сводка, live card), энкаунтеры (strip/фильтры/copy-grid/быстрая сборка), game table (pips, обе стороны переноса, range tracker + лог перемещений), справочник (nameEn). Горизонтального overflow нет на 375px и desktop; консоль без ошибок
- [x] PR открыт

## Что осталось / блокеры

- Локальный Docker-том pgdata был несовместим с текущим кодом (архетипы с пустым Code от старой схемы валили `SeedOrUpdateArchetypes` на старте — воспроизводится и на master). Перед пересозданием тома снят дамп: `%LOCALAPPDATA%\Temp\claude\H--repos-GenesysForveClaude\1c7b0849-d81c-41c0-b382-4014a415ce46\scratchpad\genesysforge-backup.dump` (pg_dump -F c). Свежий сид отрабатывает.

## Заметки / решения

- Assumption: «RU/ENG» = русское название основное + английское вторичное для одной сущности, без дублирующих записей.
- Magic cap 5: backend save flow отсутствует → «нельзя обойти через прямой API» неприменимо (билд не сохраняется); правило целиком в UI + общий helper с тестами.
- Range band tracker: локальный UI state (persistence зон в модели нет; не выдумываем схему в этой итерации).
- Инициатива в энкаунтерах не добавляется — она принадлежит активной сцене (Game Table), прототип показывал mock.
