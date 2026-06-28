# GenesysForge — общий план работ (ранжированный)

Этот документ объединяет два источника:

1. **Аудит** требований — [audit-gaps.md](audit-gaps.md) (из `GENESYSFORGE_AUDIT.md`).
2. **Существующий план** — [genesysforge_todo_plan.md](genesysforge_todo_plan.md) (задачи `GF-001…GF-017`).

Это **единый рабочий список**: иди по нему сверху вниз. Каждая задача самодостаточна
(зачем · scope · файлы · справочные материалы · Definition of Done). Колонка **Источник**
показывает происхождение (`GF-xxx`, `Аудит §N`, или оба).

Справочные материалы (книги/CSV) — см. [reference-materials.md](reference-materials.md).
Условные обозначения файлов: `B` backend, `F` frontend, `D` данные/seed, `Docs`.

Шкала приоритетов: **P0** блокеры/фундамент → **P1** ядро ценности → **P2** игровой движок/глубина → **P3** полировка/паблик.

**Статусы задач** (строка `Статус` под каждым `U-xx`; обновляет агент — см. [AGENTS.md](../AGENTS.md) §5):
⬜ Todo · 🚧 In progress · ✅ Done (PR #N). `✅` — только после слияния PR.
Детальные планы по задачам — в [tasks/](tasks/).

---

## Карта соответствия (audit ↔ GF-plan)

| Тема | Аудит | GF-plan | Итоговая задача |
|---|---|---|---|
| Экспорт/импорт персонажа | §2.4 | GF-002 | **U-03** |
| Printable/PDF лист | §2.4, §9.2 | GF-003 | **U-04** |
| XP история | §2.3 | GF-004 | **U-09** |
| Структурные свойства/эффекты | §8 (крафт) | GF-005 | **U-10** |
| Модели архетипов/карьер | §3 | GF-006/007 | **U-12, U-13** |
| Атаки NPC | §5 | GF-008/009 | **U-14, U-15** |
| Боевой roller | §7 | GF-011 | **U-17** |
| Автоматизация талантов/героик | §2.2 | GF-012 | **U-18** |
| NPC-генератор / бестиарий | §3.2, §5 | GF-010 | **U-16, U-19** |
| Email / password reset | §1.1 | GF-014 | **U-06** |
| Prod hardening / rate limit / changelog | §9, §10 | GF-016 | **U-05** |
| Deep links | — | GF-013 | **U-07** |
| Dice roller (нарративный) + секретный | §7 | — (GF-011 только боевой) | **U-08** |
| Профиль/смена пароля | §1.2 | — | **U-21** |
| Мотивации/предыстория | §2.1 | — | **U-22** |
| Критические ранения | §2.2, §8 | — | **U-23** |
| Клон/шеринг персонажа | §2.4 | — | **U-24** |
| Справочные таблицы правил | §8 | — | **U-11** |
| Кастомные архетип/карьера, импорт homebrew | §4 | — | **U-25, U-26** |
| Лицензия/About/sponsors | §10 | (changelog в GF-016) | **U-02** |
| Swagger UI / версии API / индексы / PWA | §9 | — | **U-27, U-28** |
| GM видит листы игроков | §6 | — | **U-20** |
| E2E / help | — | GF-015/017 | **U-29, U-30** |

---

# P0 — Стабилизация и снятие блокеров запуска

## U-01 · Синхронизация документации с кодом
- **Статус:** ✅ Done (PR #30)
- **Источник:** GF-001
- **Зачем:** документы расходятся с кодом (refresh-токены, password reset, Game Table, magic builder уже есть). AI-агенты получают неверный контекст.
- **Scope (Docs):** обновить `README.md`, `docs/current-state.md`, `docs/feature-roadmap.md`, `docs/mvp-ux-account-readiness.md`, `docs/api.md`. Разнести в `current-state.md`: Implemented / Partially / Not implemented / Technical risks / Domain gaps.
- **DoD:** документы не противоречат коду; в `feature-roadmap.md` задачи разбиты MVP/Beta/1.0/Future.

## U-02 · Лицензия, авторские права и публичность
- **Статус:** ✅ Done (PR #31)
- **Источник:** Аудит §10 (+ changelog из GF-016)
- **Зачем:** **отсутствие `LICENSE` — юридический блокер** публичного релиза; нужен дисклеймер о правах FFG/Genesys.
- **Scope:**
  - `LICENSE` в корне (выбрать: код — напр. MIT/AGPL; контент — отдельный дисклеймер «не аффилировано с FFG», без текста книг).
  - `Docs/About`: страница «О проекте» + копирайт-дисклеймер (PublicSafe-режим уже есть в коде).
  - `CHANGELOG.md` (Keep a Changelog) + `.github/FUNDING.yml` (sponsors), ссылки в README/футере.
  - Восстановить/опубликовать `roadmap/` (сейчас удалён в рабочем дереве; вернуть из git: `roadmap/01..06`, `README.md`).
- **DoD:** есть LICENSE; футер/README ссылаются на About, changelog, sponsors; дисклеймер виден; roadmap опубликован.

## U-03 · Экспорт/импорт персонажа в JSON
- **Статус:** ✅ Done (PR #32)
- **Источник:** GF-002 · Аудит §2.4 (боль сообщества #3)
- **Зачем:** бэкапы, перенос между аккаунтами, обмен с мастером, совместимость.
- **Scope (B):** `GET /api/characters/{id}/export`, `POST /api/characters/import`, опц. `POST /api/characters/import/preview`. Формат `genesysforge.character.v1` (см. GF-002 в todo-plan). Без `OwnerUserId`/internal id; built-in маппится по `Code`, fallback `System+Name`; custom — импортировать вместе или помечать unresolved.
- **Scope (F):** кнопка «Экспорт JSON» на листе ([SheetPage.tsx](../frontend/src/pages/SheetPage.tsx)); «Импорт» на списке ([CharactersPage.tsx](../frontend/src/pages/CharactersPage.tsx)) с preview (имя/система/архетип/карьера/XP/warnings).
- **Файлы:** новый `Features/Characters/Export*`, `Import*`; `client.ts`.
- **Тесты:** export валиден; import создаёт нового; невалидный формат отклоняется; missing refs; чужого персонажа экспортировать нельзя.
- **DoD:** round-trip export→import сохраняет основные значения; нет зависимости от старых id.

## U-04 · Полный printable / PDF-friendly лист персонажа
- **Статус:** ✅ Done (PR #34)
- **Источник:** GF-003 · Аудит §2.4, §9.2 (сейчас только печать карточек)
- **Зачем:** «нормальный PDF-экспорт» — боль сообщества #4.
- **Scope (F):** маршрут `/characters/:id/print` или кнопка «Печать листа» в `SheetPage`. На листе: основная инфо, 6 характеристик, derived (wounds/strain/soak/melee+ranged defense/enc threshold/current enc), навыки (RU/EN, карьерный, ранг, характеристика, dice pool), таланты (tier/ranked/activation/эффект), героика (activation/duration/frequency/upgrade rank/эффект), инвентарь (equipped/backpack/qty/enc/боевые статы/броня), заметки. Использовать существующий `CharacterSheetDto`.
- **CSS:** расширить `@media print` в [index.css](../frontend/src/index.css) (`.topbar/.no-print` скрыть, `page-break-inside: avoid`).
- **DoD:** печатается на A4/Letter без разрывов; навигация/кнопки не печатаются; сохранение в PDF через системный диалог.

## U-05 · Production hardening (безопасность + эксплуатация)
- **Статус:** ✅ Done (PR #35)
- **Источник:** GF-016 · Аудит §9 (rate limiting, логи), §10 (changelog)
- **Scope (B/Deploy):**
  - **Rate limiting** на `/api/auth/*` (`AddRateLimiter`, пакет уже в зависимостях) — против brute force.
  - **Secure cookies** в prod (refresh-cookie: `Secure`/`SameSite`), валидация длины `JWT_KEY` на старте, CORS только нужные origin (уже из конфига).
  - **Структурное логирование** (Serilog) + health checks API/DB (есть `/api/health`, добавить DB-чек).
  - **Backup**: ежедневный `pg_dump` + инструкция restore; **rollback** по предыдущему image-тегу; release checklist.
  - **PublicSafe-стек**: отдельный hostname, `ContentMode=PublicSafe`, проверить, что private-content не попал в public-образ.
- **DoD:** auth защищён от простого brute force; secure cookies в prod; есть backup/restore и rollback инструкции; health checks покрывают API+DB; PublicSafe не содержит private content.

## U-06 · Реальный email provider + публичный password reset
- **Статус:** ✅ Done (PR #36)
- **Источник:** GF-014 · Аудит §1.1
- **Зачем:** сейчас отправка письма — `LoggingEmailSender` (stub).
- **Scope (B):** реализация `IEmailSender` (SMTP/Resend/Mailgun/SendGrid). Конфиг `Email__Provider/From/Smtp__*`. Токен — только hash, expiry 15–60 мин, single-use, revoke сессий после смены (refresh-семейство уже есть). Rate limiting (из U-05).
- **Scope (F):** экраны forgot/reset уже есть в `AuthPage` — довести success/error, без account enumeration.
- **DoD:** восстановление пароля без доступа к логам; токен одноразовый; тесты на expiry/reuse/invalid.

## U-07 · Реальные URL / deep links
- **Статус:** ✅ Done (PR #37)
- **Источник:** GF-013
- **Зачем:** refresh страницы сбрасывает экран; нельзя поделиться ссылкой.
- **Scope (F):** маршруты `/login /register /characters /characters/:id /characters/:id/print /campaigns /campaigns/:id /campaigns/:id/table /campaigns/:id/encounters/:eid /npcs /npcs/:id /magic`. Есть [router.ts](../frontend/src/router.ts) — расширить или перейти на React Router. После login возврат на исходный URL; 404/empty state; нет данных без авторизации.
- **DoD:** ссылку на сущность можно открыть; back/forward и refresh работают.

---

# P1 — Полнота персонажа и «правила как данные»

## U-08 · Нарративный dice roller + секретный бросок GM
- **Статус:** ✅ Done (PR #39)
- **Источник:** Аудит §7 (НЕ покрыто GF-планом — GF-011 только боевой)
- **Зачем:** ключевой GM live-tool; сейчас `DicePoolView` показывает только **состав** пула.
- **Scope (F, v1 без сохранения):** собрать пул (ability/proficiency/difficulty/challenge/boost/setback), бросок, результат символами **Success/Failure/Advantage/Threat/Triumph/Despair** с нетто-итогом. Кнопка «бросить» из листа/NPC по навыку.
- **Scope (B, для стола):** `RollLogEntry` (campaignId, actor, poolJson, resultJson, summary) + публикация в Game Table через `ICampaignNotifier`/SignalR; флаг **секретного броска** (виден только GM).
- **Файлы:** новый `DiceRoller` util (F), `Features/GameTable/Roll*` (B), интеграция в [GameTableTab.tsx](../frontend/src/components/GameTableTab.tsx).
- **DoD:** игрок/GM бросает пул и видит символьный результат; секретный бросок не виден игрокам; бросок появляется в логе стола realtime.

## U-09 · История трат XP / audit log
- **Статус:** ✅ Done (PR #41)
- **Источник:** GF-004 · Аудит §2.3
- **Scope (B):** `CharacterAuditEntry` + enum `CharacterAuditAction` (см. GF-004). Писать запись при buy/refund характеристик/навыков/талантов/предметов, complete-creation, ручном изменении XP — в одной транзакции с операцией. `GET /api/characters/{id}/audit`, `POST /api/characters/{id}/xp-awards`.
- **Scope (F):** вкладка «История» на листе (дата/тип/описание/ΔXP/состояние после).
- **DoD:** все XP-операции в истории; видно, почему изменился `SpentXp`; виден refund.

## U-10 · Структурные свойства предметов и эффекты заклинаний
- **Статус:** ✅ Done (PR #44)
- **Источник:** GF-005 · Аудит §8 (крафт/качества)
- **Зачем:** строки `Properties` мешают фильтрам/тултипам/валидации/роллеру.
- **Scope (B/D):** `QualityDef` (Code/NameEn/NameRu/Kind/HasRating/DefaultActivationCost/Desc/SafeDesc/Source) + `ItemQualityValue(ItemDefId, QualityDefId, Rating)`. Seed справочника из **`_books/_qualities/genesys_rot_item_and_spell_qualities.csv`** (94 качества, уже с рейтингом/тратой/категорией). Миграция: сохранить старое `ItemDef.Properties`, написать parser строк («Точное 1», «Pierce 3»), fallback на строку.
- **Scope (F):** тултип по свойству, фильтр, отдельный rating, бейджи; селектор свойства+rating в форме custom item.
- **DoD:** структурные свойства у предметов; старые не ломаются; tooltips и фильтр работают; готово для роллера.

## U-11 · Справочные таблицы правил + глобальный поиск
- **Статус:** ✅ Done (PR #46; доработки #47/#48/#50)
- **Источник:** Аудит §8 (НЕ в GF-плане)
- **Зачем:** сейчас `/api/reference` отдаёт контент, а не правила-таблицы.
- **Scope (B/D):** статические данные-таблицы: сложности (Difficulty/Challenge по задаче), траты Advantage/Threat/Triumph/Despair по ситуациям (бой/социалка/прочее), range bands, таблица критических ранений (d100). Источники парафразов — `_magic` CSV (траты символов уже размечены колонками «Расход …») и Core Rulebook.
- **Scope (F):** раздел «Справочник правил» + глобальный поиск по справочнику (контент + таблицы).
- **DoD:** мастер видит таблицы трат/сложностей/дистанций/критов; поиск находит по названию/символу.

## U-12 · Структурные модели архетипов/видов
- **Статус:** ✅ Done (PR #52)
- **Подготовка:** ростер видов/карьер + RU-имена при создании уже сделаны (PR #49/#51, см. [archetype-roster-refresh.md](tasks/archetype-roster-refresh.md)); способности/стартовые навыки видов вынесены в структурные `ArchetypeAbilityDef`/`ArchetypeStartingSkill` (см. [u12-archetype-models.md](tasks/u12-archetype-models.md)).
- **Источник:** GF-006 · Аудит §3
- **Scope (B):** `ArchetypeAbilityDef` (видовые способности + AutomationKind) и `ArchetypeStartingSkill` (FreeRanks/IsChoice/ChoiceGroup). Применять стартовые навыки/выборы при создании.
- **Scope (F):** на выборе вида показывать характеристики, wounds/strain/xp, стартовые навыки, способности, предупреждения по выборам.
- **DoD:** видовые способности — данные, а не только описание; стартовые навыки применяются автоматически; новые виды без кода.

## U-13 · Структурные модели карьер + стартовое снаряжение
- **Статус:** ✅ Done (PR #53)
- **Детали:** `CareerStartingGear` + `CareerRule` + стартовые деньги; снаряжение из CSV через генератор `gen-career-extras-catalog.mjs`, применяется при создании (фикс + пикер выборов). См. [u13-career-models.md](tasks/u13-career-models.md).
- **Источник:** GF-007 · Аудит §3
- **Scope (B):** `CareerStartingGear` (ItemCode/qty/choice) и `CareerRule` (Kind). На создании RoT: выбор бесплатных карьерных рангов, выбор стартового снаряжения → в инвентарь, валидация числа опций.
- **Справочные данные:** карьерный CSV `_books/genesys_rot_core_careers_ru.csv` (есть; колонки «Стартовое снаряжение RU», «Заметки RU»).
- **DoD:** RoT-карьеры выдают стартовое снаряжение в инвентарь; правила выбора в UI; карьерные навыки остаются data-driven.

## U-14 · Структурированные атаки NPC
- **Статус:** ✅ Done (PR #54)
- **Детали:** `NpcAttack` + `NpcAttackQuality` (качества через каталог `QualityDef` из U-10); backfill боевых строк из `Equipment` парсером; генератор/QuickDraft выдают структурные атаки. См. [u14-npc-attacks.md](tasks/u14-npc-attacks.md).
- **Источник:** GF-008 · Аудит §5
- **Зачем:** `Npc.Equipment` = `List<string>` — мало для боя/карточек/импорта.
- **Scope (B):** `NpcAttack` (Name/SkillName/Damage/Critical/RangeBand/Notes) + `NpcAttackQuality` (QualityCode/NameRu/Rating). Перенести боевые строки из `Equipment` в атаки, оставить `Equipment` для небоевого. Обновить `NpcDto`, endpoints.
- **Scope (F):** секция «Атаки» в форме NPC (skill dropdown, damage, crit, range, qualities); статблок-атаки в карточке ([cards.tsx](../frontend/src/components/print/cards.tsx)) с бейджами-tooltip.
- **DoD:** у NPC несколько структурных атак; Encounter/Game Table их видят; карточка пригодна для стола.

## U-15 · Валидация NPC + соответствие правилам создания adversary
- **Статус:** ✅ Done (PR #55)
- **Источник:** GF-009 · Аудит §5 · правила [_books/_npc/genesys_adversary_creation_rules_for_claude.json](../_books/_npc/genesys_adversary_creation_rules_for_claude.json)
- **Детали:** расширен после аудита U-14 — NPC должны создаваться по правилам adversary (Minion/Rival/Nemesis). См. [u15-npc-validation.md](tasks/u15-npc-validation.md). Четыре блока:
  - **Гарды валидации** (`NpcValidationResult { Errors, Warnings }`): defense >4 warning / >6 error; soak чрезмерный (>7) warning; лимит ~8 навыков warning; magic NPC должен иметь магнавык+заклинания; атаки требуют skill/damage/range, crit ≥1 или пусто, качества из справочника или custom; Minion strain=null + **без рангов** (group skills); Rival strain обычно пуст (warning при наличии); Nemesis strain обязателен.
  - **Minion group_skills:** навыки миньона трактуются как групповые — ранги не значимы (ранг группы = размер−1 за столом); валидатор требует ranks=0; генератор/UI это учитывают (без отдельной сущности — переиспользуем `NpcSkill`).
  - **silhouette + tactics:** поля `Npc.Silhouette` (правило wound ≥ sil×10 для крупных) и `Npc.Tactics` (модель+миграция+DTO+UI).
  - **Генератор по формулам JSON:** `NpcDraftGenerator` — Rival wound `8+Brawn`, Nemesis `12+Brawn` / strain `10+Willpower`, Rival без strain по умолчанию, финальный урон миньона вместо `+X`.
- **DoD:** errors блокируют сохранение; warnings показываются и не блокируют; QuickDraft создаёт NPC без errors и соответствует формулам; финальный чеклист из JSON проходит.

---

# P2 — Игровой движок и глубина контента

## U-16 · NPC Draft Generator под Realms of Terrinoth
- **Статус:** ✅ Done (PR #56)
- **Источник:** GF-010 · Аудит §3.2/§5
- **Scope (B):** расширить [NpcDraftGenerator.cs](../backend/src/GenesysForge.Domain/Rules/NpcDraftGenerator.cs): роли Undead/Beast/Dragon/Demon/Construct/Minion Swarm; параметры setting/creatureTags/magicSkill/environment. Для RoT — навыки RoT (Melee Light/Heavy, Ranged, Runes, Verse, Knowledge (Lore)), без Core-only (Computers/Driving/Operating); magic NPC → magic skill; undead → теги/сопротивления/terror; beast/monster → natural weapons. Генерить структурные `NpcAttack` (из U-14).
- **DoD:** разные NPC для RoT и Core; RoT не получает Core-навыки; генерит структурные атаки + теги + warnings.

## U-17 · Боевой attack/damage roller
- **Статус:** ✅ Done (PR #59)
- **Источник:** GF-011 · Аудит §7
- **Зачем:** первый слой боевых бросков поверх U-08 (пул) и U-10 (качества).
- **Scope:** выбрать персонажа/NPC и оружие/атаку → показать skill+характеристику, собрать базовый пул, вручную добавить difficulty/boost/setback/upgrade, бросок (через U-08) → ввод/расчёт net successes/adv/threat/triumph/despair → базовый damage + доступные качества с ценой активации. v1 — frontend calc; лог — через `RollLogEntry` (U-08).
- **DoD:** видно расчёт базового урона по оружию; качества показаны с ценой активации; roller не решает за мастера.

## U-18 · Автоматизация талантов и героических способностей
- **Статус:** ✅ Done (Stage 1 — PR #60; Stage 2 — PR #61)
- **Источник:** GF-012 · Аудит §2.2
- **Scope (B):** `RuleEffectDef` (Source/Timing/Automation/Target/DataJson). Автоматизировать простое: +wt/+st/+soak/+melee+ranged def/+enc threshold, heal wounds/strain, spend story points, add boost/setback к следующей проверке, timed-effect с длительностью. НЕ автоматизировать сложные нарративные/GM-решения (показывать как manual prompt).
- **Scope (F):** для активных талантов/героик — кнопка «Активировать» (стоимость, авто-часть, timed effect, запись в audit-log U-09).
- **DoD:** простые активные эффекты применяются кнопкой; сложные — manual prompt; видно, что применилось автоматически.

## U-19 · Преднаполненный бестиарий Terrinoth
- **Статус:** ✅ Done (PR #62)
- **Источник:** Аудит §3.2/§5 (НЕ в GF-плане)
- **Детали:** `Npc.IsBuiltIn` + `OwnerUserId` nullable; встроенные существа видны всем read-only (`CanViewAsync`/`GetNpcsHandler`), правка/удаление закрыты, клонируются в свою библиотеку (`DuplicateNpc`). Импорт **86 официальных существ RoT** из `_books/_adversaries/genesys_fantasy_adversaries_ru.json` (генератор `gen-bestiary-catalog.mjs` → embedded `bestiary.catalog.json`, идемпотентный `SeedBestiary`). См. [u19-terrinoth-bestiary.md](tasks/u19-terrinoth-bestiary.md).
- **Scope (D):** seed-набор существ RoT как built-in NPC (stat-блоки) поверх существующей модели NPC + U-14 атак.
- **DoD:** в библиотеке NPC есть встроенные существа RoT; их можно клонировать/добавлять в encounter/стол.

## U-20 · GM видит полные листы персонажей игроков
- **Статус:** ✅ Done (PR #63)
- **Источник:** Аудит §6
- **Детали:** `GET /api/campaigns/{id}/characters/{characterId}/sheet` (`GetCampaignMemberSheetHandler`): проверка GM (`CampaignMapper.GetAsGmAsync`) + членство персонажа, лист строится под владельца-игрока (`SheetBuilder` + новый `CharacterLoader.LoadWithRelationsAsync` без owner-check). UI — кнопка «Лист» у участника (GM) → read-only `CharacterSheetPrint` в `PrintPreview`. См. [u20-gm-view-sheets.md](tasks/u20-gm-view-sheets.md).
- **Scope (B/F):** в кампании GM-доступ к read-only листу персонажа участника (переиспользовать `GetCharacterSheet` с проверкой роли GM в кампании). UI — открытие листа из списка участников.
- **DoD:** GM открывает лист игрока read-only; игрок чужие листы не видит.

## U-21 · Профиль / управление аккаунтом
- **Статус:** ✅ Done (PR #64)
- **Источник:** Аудит §1.2 (НЕ в GF-плане)
- **Детали:** `User.AvatarUrl` (миграция `AddUserAvatar`); `GET/PATCH /api/account`, `POST /api/account/change-password` (verify current → revoke всех refresh-сессий → свежий cookie текущему устройству). Фронт — страница «Профиль» (`ProfilePage`, область роутера `account`): имя/аватар + смена пароля. См. [u21-account-profile.md](tasks/u21-account-profile.md).
- **Scope (B):** `GET /api/account`, `PATCH /api/account` (displayName, опц. avatarUrl/инициалы), `POST /api/account/change-password` (old+new, revoke семейства). Поле `User.AvatarUrl` (миграция).
- **Scope (F):** страница профиля/настроек.
- **DoD:** пользователь видит/редактирует имя и аватар, меняет пароль в сессии.

## U-22 · Мотивации и предыстория персонажа
- **Статус:** 🚧 In progress (PR #65)
- **Источник:** Аудит §2.1 (НЕ в GF-плане)
- **Детали:** поля `Desire/Fear/Strength/Flaw` + `Background` в `Character` (миграция `AddCharacterMotivations`), в Create/Update-request + `CharacterSheetDto`. Вкладка «Образ» (`BioTab`) на листе + свёрнутый блок в создании + секция в печати. См. [u22-motivations-background.md](tasks/u22-motivations-background.md).
- **Scope (B):** поля `Desire/Fear/Strength/Flaw` + `Background` в `Character` (миграция), в `CreateCharacterRequest`/`UpdateCharacterRequest`/`CharacterSheetDto`.
- **Scope (F):** блок мотиваций и текст предыстории на листе/создании; выводить в печать (U-04).
- **DoD:** мотивации и предыстория сохраняются, видны на листе и в печати.

## U-23 · Критические ранения
- **Статус:** ⬜ Todo
- **Источник:** Аудит §2.2 + §8 (НЕ в GF-плане)
- **Scope (B):** `CharacterCriticalInjury` (severity/result/notes) на персонаже; для участников стола — счётчик/список критов. Связать с таблицей крит-ранений из U-11.
- **Scope (F):** секция критов на листе и на карточке участника стола.
- **DoD:** криты добавляются/снимаются; видны на листе и за столом; ссылаются на таблицу.

## U-24 · Клонирование и read-only шеринг персонажа
- **Статус:** ⬜ Todo
- **Источник:** Аудит §2.4 (НЕ в GF-плане)
- **Scope (B):** `POST /api/characters/{id}/duplicate` (по образцу `DuplicateNpc`); шеринг — `POST /api/characters/{id}/share` (генерит токен) + публичный `GET /api/share/{token}` (read-only sheet, без auth).
- **Scope (F):** кнопки «Клонировать» и «Поделиться (ссылка)» на листе/списке.
- **DoD:** персонаж клонируется; по ссылке открывается read-only лист без логина; токен можно отозвать.

---

# P2 — Глубина homebrew

## U-25 · Кастомные архетип/раса и карьера
- **Статус:** ⬜ Todo
- **Источник:** Аудит §4.1 (боль сообщества #1, НЕ в GF-плане)
- **Scope (B):** расширить [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs): CRUD кастомного архетипа (характеристики/пороги/XP/способность) и кастомной карьеры (карьерные навыки). Использовать `OwnerUserId` как у прочего homebrew; учесть структурные модели U-12/U-13.
- **Scope (F):** формы в [CustomTab.tsx](../frontend/src/components/CustomTab.tsx); доступность в создании персонажа.
- **DoD:** пользователь создаёт свой вид/карьеру и собирает на них персонажа без правки кода.

## U-26 · Импорт homebrew из JSON + шеринг + per-character toggle
- **Статус:** ⬜ Todo
- **Источник:** Аудит §4.2 (НЕ в GF-плане)
- **Scope (B):** импорт набора homebrew (skills/talents/items/heroics/archetypes/careers) из JSON (совместимый формат, маппинг по Code); публикация набора другим пользователям (отдельно от campaign Content Packs, которые остаются справочными); флаг включения homebrew-набора на персонажа/кампанию.
- **DoD:** homebrew переносится через JSON; можно расшарить набор; персонаж может включать/выключать наборы.

---

# P3 — Полировка, паблик, инфраструктура

## U-27 · API versioning + Swagger/Scalar UI + индексы БД
- **Статус:** ⬜ Todo
- **Источник:** Аудит §9.1/§9.3
- **Scope (B):** префикс `/api/v1` (или version header) на группах; подключить Scalar/Swagger UI поверх существующего `MapOpenApi`; явные индексы EF на «горячих» полях (поиск NPC по System/Kind/Role/Tag, контент по System/OwnerUserId, токены).
- **DoD:** версия в путях; UI документации открывается; миграция с индексами применена.

## U-28 · PWA / офлайн (можно отложить)
- **Статус:** ⬜ Todo
- **Источник:** Аудит §9.2 (в GF-плане отложено)
- **Scope (F):** manifest + service worker (vite-plugin-pwa), кеш статики и справочника read-only.
- **DoD:** установка как PWA; справочник доступен офлайн. *(низкий приоритет, после Beta)*

## U-29 · E2E smoke tests
- **Статус:** ⬜ Todo
- **Источник:** GF-015
- **Scope:** Playwright — 10 сценариев из GF-015 (register→sheet, buy/refund skill+talent, equip→derived, campaign join, NPC duplicate→encounter, encounter→table, magic builder, export→import, reset screen). Job `e2e` в CI или отдельный workflow.
- **DoD:** smoke-покрытие главных flow; запускается локально и в CI.

## U-30 · User-facing help pages
- **Статус:** ⬜ Todo
- **Источник:** GF-017
- **Scope:** `docs/user-guide/` (Markdown) → отрисовка в UI: создание персонажа, карьерные навыки, покупка навыков/талантов, инвентарь, магия, NPC, encounter, Game Table, PublicSafe/PrivateFull. Без текста книг.
- **DoD:** новичок понимает базовый flow без README; ссылка «Справка» в UI.

---

## Рекомендуемый порядок (сводно)

1. **P0:** U-01 → U-02 → U-03 → U-04 → U-05 → U-06 → U-07
2. **P1:** U-08 → U-09 → U-10 → U-11 → U-12 → U-13 → U-14 → U-15
3. **P2 движок:** U-16 → U-17 → U-18 → U-19 → U-20 → U-21 → U-22 → U-23 → U-24
4. **P2 homebrew:** U-25 → U-26
5. **P3:** U-27 → U-29 → U-30 → U-28

## Definition of Done для Beta
Документация синхронизирована · персонажа можно экспортировать/импортировать и печатать ·
есть история XP · свойства предметов структурированы · NPC имеют структурные атаки ·
работает нарративный + базовый боевой roller · password reset через реальный email ·
есть LICENSE/changelog/About · backup/restore и PublicSafe проверены · smoke E2E зелёные.
