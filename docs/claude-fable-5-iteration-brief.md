# Инструкция для Claude Fable 5: итерация по контенту, магии и NPC UI

## Цель итерации

За одну итерацию и один PR закрыть список доработок по локализации справочников, категориям талантов, ownership кастомного контента, ограничениям магии и переработке экранов с учетом всех подготовленных HTML-прототипов из `docs`, которые еще не перенесены в приложение.

Работать в рамках существующей архитектуры GenesysForge. Не дробить на несколько PR, если нет технического блокера.

## Обязательная подготовка

1. Прочитать `AGENTS.md` и `docs/ai-context.md`.
2. Проверить рабочее дерево: `git status`.
3. Выполнить `git fetch` и проверить открытые PR: `gh pr list --state open`.
4. Создать рабочую ветку по правилам репозитория. Рекомендуемый slug: `content-npc-magic-ux-polish`.
5. Создать план задачи: `roadmap/tasks/content-npc-magic-ux-polish.md` по шаблону `roadmap/tasks/_TEMPLATE.md`.
6. Перед изменениями найти актуальные зоны кода через `rg`, а не полагаться только на этот brief.
7. Провести аудит всех `docs/*.html` прототипов и отметить в плане, какие уже реализованы, какие частично реализованы, какие еще нужно перенести.

## HTML-прототипы, которые нужно учесть

Перед реализацией открыть каждый HTML-файл из списка ниже, сравнить с текущим приложением и включить в итерацию все прототипные решения, которые еще не реализованы. Если конкретный прототип уже полностью перенесен, отметить это в плане как `Already implemented` с кратким указанием, где именно реализовано.

Текущий список прототипов:

- `docs/gm-dashboard-prototype.html` — вкладка "Обзор" в разделе кампании / GM dashboard.
- `docs/campaign-encounters-prototype.html` — вкладка "Энкаунтеры" внутри кампании.
- `docs/campaign-game-table-prototype.html` — игровой стол кампании.
- `docs/range-band-tracker-prototype.html` — трекер дистанций/range bands, если его еще нет в боевом или campaign flow.
- `docs/npc-quick-draft-prototype.html` — быстрое создание NPC.
- `docs/npc-create-prototype.html` — обычное/ручное создание NPC.

Что сделать по каждому прототипу:

- Найти целевой экран/компонент в текущем приложении.
- Проверить, какие UX-решения уже реализованы, а какие остались только в HTML.
- Переносить не пиксель-в-пиксель, а в текущую дизайн-систему приложения: те же плотность, цвета, радиусы, кнопки, табы, chips, панели.
- Не переносить статичные mock data как реальные данные. Подключать существующие API/DTO/state.
- Проверить desktop/mobile и отсутствие horizontal overflow.
- Если прототип конфликтует с более свежей реализацией, выбрать текущий продуктовый flow и перенести только полезные элементы прототипа.

## Исходные доработки

### 1. Все навыки и снаряжение должны отображаться RU/ENG

Assumption: `ru/eng` означает, что в пользовательском интерфейсе нужно показывать русское название и оригинальное/английское название одной сущности, а не создавать дублирующие записи.

Что проверить и исправить:

- Все `SkillDef` и `ItemDef` уже имеют `NameRu` и `Name`/`NameEn` в модели/DTO. Не вводить второй источник истины.
- В UI использовать единый формат отображения: русское название как основное, английское как вторичное. Например: `Ближний бой / Melee` или компактный secondary label, если место ограничено.
- Пройти все места выбора/отображения навыков и снаряжения: создание персонажа, лист персонажа, инвентарь, NPC editor, quick draft NPC, справочник, импорт/экспорт preview, карточки печати, глобальный поиск.
- Переиспользовать существующие helpers из `frontend/src/utils/labels.ts`, особенно `localizedName`, либо расширить helper так, чтобы не плодить форматирование вручную.
- Проверить, что значения для API и правил продолжают использовать стабильные `Code`, `Id` или canonical `Name`, а не переведенный display label.

Ориентиры по коду:

- `frontend/src/utils/labels.ts`
- `frontend/src/api/types.ts`
- `frontend/src/components/InventoryTab.tsx`
- `frontend/src/pages/ReferencePage.tsx`
- `frontend/src/pages/NpcsPage.tsx`
- `frontend/src/components/SheetTab.tsx`
- `backend/src/GenesysForge.Application/Dtos/SkillDefDto.cs`
- `backend/src/GenesysForge.Application/Dtos/ItemDefDto.cs`
- `backend/src/GenesysForge.Application/Common/Mappers.cs`

Критерии приемки:

- В пользовательских селектах, списках, карточках и инвентаре навыки/снаряжение читаются как RU/ENG.
- Нет мест, где один и тот же навык или предмет отображается только на английском без причины.
- Нет регрессии сохранения/покупки/экипировки из-за использования display label вместо идентификатора.

### 2. Проанализировать смысл талантов и распределить их по существующим тегам

Существующие теги/категории талантов: `general`, `social`, `combat`, `magic`.

Что сделать:

- Пройти весь catalog талантов и проверить `category`.
- Назначить категорию по смыслу таланта:
  - `combat`: атаки, защита, критические попадания, оружие, броня, инициатива, выживаемость в бою.
  - `social`: переговоры, обман, лидерство, влияние, репутация, контакты, торговля через социальное взаимодействие.
  - `magic`: заклинания, магические навыки, руны, divine/primal/arcana/verse, модификация магии.
  - `general`: универсальные, исследовательские, ремесленные, экономические, путешествия, выживание, знания, небоевые улучшения без явной социальной/магической привязки.
- Не копировать official text из книг. Разрешены только структурные данные и собственные краткие paraphrase-заметки, если они уже нужны в проекте.
- Проверить, что фильтр талантов работает по этим категориям и не ломает пирамиду/стоимость XP.

Ориентиры по коду:

- `backend/src/GenesysForge.Infrastructure/Persistence/SeedContent/talents.catalog.json`
- `backend/src/GenesysForge.Domain/Enums/TalentCategory.cs`
- `backend/src/GenesysForge.Domain/Entities/TalentDef.cs`
- `backend/src/GenesysForge.Application/Dtos/TalentDefDto.cs`
- `frontend/src/utils/labels.ts`
- `frontend/src/components/TalentsTab.tsx`
- `frontend/src/components/TalentsTab.test.tsx`

Критерии приемки:

- Каждый built-in талант имеет осмысленную категорию.
- Фильтр по категориям показывает ожидаемые таланты.
- Категория не влияет на цену, ранги и pyramid validation.

### 3. Привязать создание кастома к пользователю, а не к персонажу

Важно: по текущему контексту проекта custom reference content уже должен быть user-scoped через `OwnerUserId`, где `OwnerUserId = null` означает built-in content. Сначала провести аудит. Если привязки к персонажу для custom reference content не найдено, зафиксировать это в плане как `Not found in current codebase` и проверить только UI/flows.

Что проверить:

- Создание кастомных навыков, талантов, предметов, heroic abilities, archetypes, careers.
- Импорт homebrew/content packs.
- Visibility custom content в reference data и при создании/редактировании персонажа/NPC.
- Удаление и обновление custom content: доступ только владельцу.
- UI flow: если пользователь создает кастом из контекста персонажа, запись все равно создается на пользователя и доступна другим персонажам этого пользователя.

Ориентиры по коду:

- `backend/src/GenesysForge.Application/Features/CustomContent`
- `backend/src/GenesysForge.Application/Common/HomebrewVisibility.cs`
- `backend/src/GenesysForge.Application/Features/Reference/GetReferenceHandler.cs`
- `backend/src/GenesysForge.Application/Features/HomebrewPacks`
- `backend/src/GenesysForge.Application/Features/Characters`
- `frontend/src/components/CustomTab.tsx`
- `frontend/src/api/client.ts`

Критерии приемки:

- Custom content создается с `OwnerUserId = current user`.
- Custom content не привязан к одному персонажу и виден в справочниках/формах для всех персонажей владельца.
- Другой пользователь не видит и не может менять чужой custom content.
- Если требуется изменение persistent model, добавить EF migration и обновить `docs/database.md`. Не делать destructive migration.

### 6. Ограничить эффекты магии максимальной сложностью 5

Правило: итоговая сложность магического действия не может быть выше 5. При достижении сложности 5 нельзя добавлять новые эффекты.

Что сделать:

- Посчитать итоговую сложность как base effect difficulty + сумма выбранных additional effects.
- Если итоговая сложность равна 5, UI должен блокировать добавление новых эффектов.
- Если добавление конкретного эффекта превысит 5, этот эффект должен быть disabled и иметь понятную причину.
- Если итоговая сложность уже 5, кнопки/чипы добавления новых эффектов не должны позволять добавить еще один modifier.
- Если есть API/save flow для магического билда, backend/application должен валидировать это же правило. UI не должен быть единственным источником истины.
- Использовать существующий `parseDifficulty` или общий helper, чтобы `+1`, `+2` и base difficulty считались одинаково во всех местах.

Ориентиры по коду:

- `frontend/src/components/MagicBuilder.tsx`
- `frontend/src/components/SpellsTab.tsx`
- `frontend/src/utils/labels.ts`
- `frontend/src/api/types.ts`
- `backend/src/GenesysForge.Application/Features/Spells`
- `backend/src/GenesysForge.Domain/Entities/SpellDef.cs`

Критерии приемки:

- Пользователь не может собрать магическое действие сложнее 5 через UI.
- Нельзя обойти ограничение через прямой API, если build сохраняется/передается на backend.
- В UI понятно, почему эффект недоступен.
- Есть тесты на расчет и блокировку.

### 11. Уменьшить табы для эффектов заклинаний, добавить теги с описанием свойств

Что сделать:

- Заменить крупные табы дополнительных эффектов на компактные tags/chips.
- У каждого tag должно быть короткое название, сложность (`+N`) и доступное описание свойства.
- Для описаний использовать только `SafeDescription` или собственные краткие paraphrase-описания. Не копировать official text.
- Состояния tag:
  - доступен;
  - выбран;
  - disabled, если добавление превысит сложность 5;
  - disabled/locked, если не подходит к выбранному base effect.
- Добавить compact summary выбранных эффектов и итоговой сложности.
- Проверить desktop/mobile, чтобы chips не ломали сетку и не вызывали горизонтальный overflow.

Ориентиры по коду:

- `frontend/src/components/MagicBuilder.tsx`
- `frontend/src/components/SpellsTab.tsx`
- `frontend/src/utils/labels.ts`
- `frontend/src/index.css`

Критерии приемки:

- Экран магии стал компактнее.
- Описания эффектов доступны без раздувания табов.
- Ограничение сложности 5 визуально связано с disabled states.

### 12. Переработать экраны из всех нереализованных HTML-прототипов

Добавить в эту же итерацию переработку экранов с учетом всех HTML-прототипов из `docs`, которые еще не реализованы или реализованы частично. Это часть того же PR, потому что campaign flow, game table, NPC creation, skills/equipment display, role-based generation и magic/combat UX пересекаются в пользовательских сценариях ведущего.

Кампании: обзор / GM dashboard:

- Сверить текущую вкладку "Обзор" кампании с `docs/gm-dashboard-prototype.html`.
- Перенести полезные решения по структуре GM dashboard: активная сцена, быстрые статусы, заметки, участники, подготовка/сводка, быстрые действия.
- Не превращать обзор в landing page; это рабочий экран ведущего.
- Сохранить верхнюю навигацию внутри кампании и общий layout с левым app sidebar / правым dice roller sidebar, если они уже есть в приложении.

Кампании: энкаунтеры:

- Сверить текущую вкладку "Энкаунтеры" с `docs/campaign-encounters-prototype.html`.
- Перенести структуру, где заметки и быстрая сборка не сдвигают ключевые блоки.
- Выровнять по высоте/плотности основные блоки: содержание сцены, участники, запуск, инициатива.
- Не возвращать удаленные блоки "Готовность" и "Проверка перед стартом", если они были убраны по предыдущему решению.

Кампании: игровой стол:

- Сверить текущий Game Table с `docs/campaign-game-table-prototype.html`.
- Перенести понятный flow для бросков с выбором кубиков как в dice roller.
- Переработать блок со счетчиками игроков/мастера в более понятный элемент, если текущая реализация все еще выглядит неоднозначно.
- Проверить, что броски открывают/используют правый dice roller sidebar и не блокируют основной интерфейс.

Range band tracker:

- Сверить наличие и качество реализации по `docs/range-band-tracker-prototype.html`.
- Если трекер дистанций еще не встроен, определить правильное место: Game Table, Encounter или combat tool внутри кампании.
- Реализовать как рабочий инструмент, а не декоративную карточку: участники, диапазоны, перемещение, быстрые изменения дистанции.
- Сохранять/использовать существующие campaign/game table state только если такая persistence уже есть; иначе явно ограничить как локальный UI state и отметить это в плане.

NPC creation:

- `docs/npc-quick-draft-prototype.html` — быстрое создание NPC.
- `docs/npc-create-prototype.html` — обычное/ручное создание NPC.

Быстрое создание NPC:

- Перестроить текущий `QuickDraftForm` в более понятный wizard/editor без модального блокирования основного контекста, если это согласуется с текущей навигацией.
- Сделать профиль генерации читаемым: система, тип NPC, роль, уровень угрозы, стиль боя, магический навык, окружение.
- Показывать live preview результата: характеристики, навыки, атаки, снаряжение, способности, теги.
- Явно показывать, какие навыки/предметы/способности будут выданы из-за роли NPC.
- Для навыков и снаряжения использовать RU/ENG display.
- Проверить правила генерации: навыки, снаряжение и способности должны соответствовать роли NPC.

Обычное создание NPC:

- Переработать экран ручного создания по `docs/npc-create-prototype.html`.
- Оставить плотный, рабочий интерфейс без маркетинговых блоков.
- Разделы: базовая информация, характеристики, пороги/защита, навыки, атаки, способности, снаряжение, теги/видимость.
- Добавить правую сводку или live card preview, если это не конфликтует с существующей layout-системой.
- Исправить проблемы прототипа, которые уже были выявлены: не слипать helper text с полями, у select должны быть нормальные отступы и стрелки.
- Desktop и mobile не должны иметь горизонтальный overflow.

Ориентиры по коду:

- `frontend/src/pages/CampaignsPage.tsx`
- `frontend/src/components/EncountersTab.tsx`
- `frontend/src/components/GameTableTab.tsx`
- `frontend/src/pages/NpcsPage.tsx`
- `frontend/src/api/types.ts`
- `frontend/src/api/client.ts`
- `backend/src/GenesysForge.Application/Dtos/NpcDtos.cs`
- `backend/src/GenesysForge.Application/Features/Npcs/QuickDraftNpcHandler.cs`
- `backend/src/GenesysForge.Domain/Rules/NpcDraftGenerator.cs`
- `backend/src/GenesysForge.Application/Features/Npcs/NpcMapper.cs`
- `frontend/src/index.css`

Критерии приемки:

- Quick draft NPC позволяет быстро понять, что будет создано и почему.
- Ручной NPC editor не выглядит как длинная простыня без структуры.
- Campaign overview, Encounters, Game Table и Range Band Tracker сверены с HTML-прототипами; нереализованные части перенесены или явно отмечены как out of scope/blocker в плане.
- Роль NPC влияет на выдаваемые навыки, снаряжение и способности предсказуемо и проверяемо.
- Нет слипшихся элементов, съехавших select arrows, перекрытия текста и горизонтального overflow.

## Рекомендуемый порядок выполнения

1. Аудит данных и контрактов: `SkillDef`, `ItemDef`, `TalentDef`, `SpellDef`, custom content, NPC DTO.
2. Обновить план задачи и отметить, нужна ли миграция.
3. Привести RU/ENG отображение skills/equipment к единому helper.
4. Перепроверить и поправить категории талантов в catalog.
5. Проверить user-scoped custom content; исправлять только найденные реальные привязки к персонажу.
6. Реализовать ограничение сложности магии до 5.
7. Переработать UI эффектов заклинаний на compact tags/chips.
8. Провести аудит всех HTML-прототипов в `docs` и отметить статус каждого в плане.
9. Перенести нереализованные части campaign overview / encounters / game table / range band tracker.
10. Переработать quick draft NPC по прототипу.
11. Переработать ручное создание NPC по прототипу.
12. Обновить тесты и документацию.
13. Прогнать проверки и открыть PR.

## Тесты и проверки

Минимальный набор:

- `dotnet test backend/GenesysForge.slnx`
- `cd frontend; npm run lint`
- `cd frontend; npm test`
- `cd frontend; npm run build`

Дополнительно:

- Component tests для talent category filter.
- Unit/component tests для magic difficulty cap.
- Tests/audit для custom content ownership, если меняется backend flow.
- Проверка NPC quick draft: разные роли должны давать ожидаемые навыки, снаряжение и способности.
- Browser QA desktop/mobile для magic UI и NPC screens.
- Browser QA desktop/mobile для campaign overview, encounters, game table и range band tracker, если эти экраны менялись.

Если тесты нельзя запустить из-за окружения, указать это в PR с конкретной причиной и командой, которая не прошла.

## Ограничения и риски

- Не добавлять официальные тексты из Genesys Core Rulebook, Realms of Terrinoth или других книг.
- Не менять XP rules, talent pyramid, derived stats и auth/JWT behavior без прямой необходимости.
- Не делать destructive migration.
- Не менять package versions, Docker/CI/deploy config без отдельного решения.
- Frontend-компоненты не должны напрямую вызывать `fetch`; новые API методы добавлять в `frontend/src/api/client.ts`.
- Backend остается source of truth для правил, если поведение влияет на данные или API.

## PR checklist

- [ ] RU/ENG display для skills/equipment проверен во всех основных UI.
- [ ] Все built-in talents имеют осмысленную категорию.
- [ ] Talent category filter работает.
- [ ] Custom content audit выполнен; найденные character-scoped проблемы исправлены или отмечено `Not found in current codebase`.
- [ ] Magic difficulty cap 5 работает в UI и backend/API, если применимо.
- [ ] Spell effects отображаются compact tags/chips с описаниями.
- [ ] Все `docs/*.html` прототипы проверены и статус каждого отмечен в плане.
- [ ] Нереализованные части `gm-dashboard-prototype.html`, `campaign-encounters-prototype.html`, `campaign-game-table-prototype.html` и `range-band-tracker-prototype.html` перенесены или явно обоснованы как out of scope/blocker.
- [ ] Quick draft NPC переработан по прототипу.
- [ ] Ручное создание NPC переработано по прототипу.
- [ ] Нет horizontal overflow и слипшихся элементов на desktop/mobile.
- [ ] Тесты/линт/билд пройдены или причина невозможности запуска указана.
- [ ] `docs/database.md` обновлен, если была миграция.
- [ ] PR description содержит цель, изменения, тесты, миграции, риски, copyright-note и ссылку на план.
