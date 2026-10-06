# Аудит PublicSafe относительно PrivateFull — 05.10.2026

**Эталон:** [app.genesys-forge.com](https://app.genesys-forge.com). **Проверяемая версия:**
[genesys-forge.com](https://genesys-forge.com). **Код:** актуальный на начало аудита
`origin/master`, `4da4636ed7783d9f99340de2a978517dbb9fdb43`. Пользователь подтвердил эту
базу: `origin/main` в репозитории отсутствует. Изменения открытого PR #256 в аудит не включены.

**Основной результат:** публичная версия не является отдельной старой сборкой приложения.
Обе версии используют общий frontend и общий код правил. Подтверждённое отставание находится
в доставке безопасных описаний: PublicSafe очищает `Description`, а три DTO-проекции теряют
`SafeDescription`. Это ухудшает героику, ремесло и транспорт. Дополнительно есть различия в
полноте RU/EN-подсказок, пробелы именно в проверках PublicSafe и общие риски кеширования.
Измерений, подтверждающих существенное общее отставание публичной версии по скорости, нет.

Связанные документы:

- [Полный план тестирования](public-version-test-plan.md).
- [Инвентаризация 167 HTTP-маршрутов](public-api-test-inventory.md).
- [План выполнения аудита](../roadmap/tasks/public-private-audit.md).

## 1. Достоверность и границы

Используются три обозначения:

- **Live:** результат read-only запроса или наблюдения production 05.10.2026.
- **Code:** вывод из указанного commit; это не выполнение сценария в production.
- **Assumption / Verify:** гипотеза или улучшение, эффект которого ещё нужно измерить.

Выполнены: чтение инструкций и кода, проверка GitHub variables с названиями доменов,
статуса deploy, разрешённого `svcctl status genesysforge`, анонимных страниц и HTTP-поверхностей,
сравнение хешей стартовых ассетов, 10 чередующихся GET health на каждый домен, статическая
инвентаризация каталогов, API и тестов. Производственные данные не изменялись.

**Не выполнены:** авторизованные сценарии, сравнение фактических built-in строк двух production
БД, создание/покупки/возвраты, SMTP/OAuth end-to-end, нагрузочное тестирование, восстановление
backup и прогон test suite. Обе браузерные сессии остались на входе; защищённый reference вернул
401 на обоих доменах. Доступный SSH-профиль позволяет проверять статус сервисов, но не даёт
прямого Docker/DB-доступа. Это ограничение доказательств, а не обнаруженный дефект сайта.

Отсутствующий release/seed-version endpoint: **Not found in current codebase**.
Точный SHA реально запущенного backend через доступный status: **Not found in current codebase**.
Последний успешный [deploy run](https://github.com/Giviruk/GenesysForgeClaude/actions/runs/37118333343)
связан с `4da4636`, но сам по себе не доказывает текущий image digest контейнера.
**Assumption:** production backend соответствует этому deploy; это нужно подтвердить digest/label.

## 2. Как устроены две версии

Источники: [prod compose](../docker-compose.prod.yml), [backend Dockerfile](../backend/Dockerfile),
[SeedData](../backend/src/GenesysForge.Infrastructure/Persistence/SeedData.cs),
[Infrastructure DI](../backend/src/GenesysForge.Infrastructure/DependencyInjection.cs).

| Область | PrivateFull | PublicSafe | Вывод |
|---|---|---|---|
| Frontend image | `genesysforge-web:latest` | тот же image | Отдельной ограниченной UI-ветки нет |
| Backend | общий код, private target | общий код, public target | Target исключает private embedded resources, а не функции |
| Контент | `Description` + overlay, safe + EN | `Description` очищен, safe + EN сохранены | Основной источник отличий |
| PostgreSQL | отдельная БД и volume | отдельная БД и volume | Пользователи/персонажи не обязаны совпадать |
| JWT | основной signing key | отдельный namespace signing key | Cross-stack access JWT должны отклоняться |
| Storage | private prefix | public prefix | Разделение префиксов; доступ проверять отдельно |
| Ресурсы compose | API 320 MiB, DB 256 MiB, web 64 MiB | те же лимиты | Меньших public-лимитов не найдено |
| Auth, SMTP, Google | общий код | общий код | Не считать отсутствие тестового входа отсутствием функции |
| XP/пирамида/derived stats | backend rules | те же backend rules | PublicSafe не должен менять математику |

`AppMode Private/Public` как отдельная политика доступа: **Not found in current codebase**.
Наличие GitHub variables с такими именами не означает, что код их использует.

Live-status: оба API и оба web работают около двух дней; обе БД healthy,
`postgres:17.11-alpine`. Контейнеры перечислены с тегом `latest`, а не immutable SHA.

## 3. Функциональная матрица

«Есть в обоих» означает наличие общей реализации; авторизованный live-паритет пока не проверен.

| Функция | Статус кода | Отличие публичной / что проверить |
|---|---|---|
| E-mail registration/login, refresh/logout, Google, password reset | Есть в обоих | Cookie, origin, SMTP и OAuth конфигурация требуют отдельных live smoke |
| Профиль, avatar, portrait | Есть в обоих | Storage provider, ограничения файлов и public prefix |
| Создание Core/RoT, видовые/карьерные выдачи, стартовый комплект | Есть в обоих | Состав и механические поля built-in каталога должны совпадать |
| XP, характеристики, навыки, таланты, refund, завершение создания | Есть в обоих | PublicSafe не изменяет правила; проверить одинаковые последовательности |
| Героика: identity, параметры, upgrades, signature weapon | Есть в обоих | **Потеря RU-описаний всех 22 Power upgrades**, F-01 |
| Инвентарь, экипировка, покупки/продажи, damage, attachments | Есть в обоих | Часть public-подсказок короче; числа/эффекты должны остаться теми же |
| Скакуны, повозка, груз, тяга | Есть в обоих | **Потеря RU-описаний 5 профилей**, F-03 |
| Crafting, alchemy, enchantment | Есть в обоих | **Потеря RU-объяснений 39 трат**, F-02; UI не использует EN-поля |
| Магия, builder, implements, shards, dice roller | Есть в обоих | Safe summary против full; difficulty/availability/exclusions должны совпадать |
| Кампании, join code, notes, chronicle и revisions | Есть в обоих | Ownership и GM/player visibility; общей потери функции не найдено |
| NPC, bestiary, draft, duplicate, encounters | Есть в обоих | Built-in statblocks и вложенные abilities; число пользовательских NPC не сравнивать |
| Game Table, initiative, range, minions, modifiers, rolls, SignalR | Есть в обоих | Proxy/reconnect и нагрузка; общего public-disable не найдено |
| Custom content, content packs, homebrew import/share | Есть в обоих | Пользовательский контент и видимость не должны смешиваться между пользователями |
| Character clone, JSON import/export v1…v8, public share | Есть в обоих | Это уже реализовано; cross-stack перенос встроенного контента по stable code |
| Полный printable sheet и карточки | Есть в обоих | Те же потери описаний могут попасть в печать; layout проверить отдельно |
| RU/EN, responsive navigation, PWA | Есть в обоих | RU/EN полнота, кеш с личным контентом и offline fallback |
| OpenAPI/Scalar | Есть в backend | **Live `/openapi/v1.json` возвращает HTML SPA**, C-05 |

### F-01. SafeDescription Power upgrades не достигает клиента — P1, Code

Цепочка:

1. В `heroics.catalog.json` есть 11 способностей × 2 улучшения = **22**, у всех заполнены
   `safe` и `descEn`.
2. `HeroicCatalog` сохраняет оба текста, но `SeedData` в PublicSafe очищает upgrade `Description`.
3. `HeroicAbilityUpgradeDto` и mapper передают `Description` и `DescriptionEn`, **без safe-поля**.
4. `localizedDescription` в RU ищет `description || safeDescription`; оба значения отсутствуют.
   В EN `descriptionEn` остаётся, поэтому языки ведут себя по-разному.
5. Это затрагивает HeroicTab и `CharacterSheetPrint`, включая выбор до покупки.

Источники: [HeroicCatalog](../backend/src/GenesysForge.Infrastructure/Persistence/HeroicCatalog.cs),
[DTO](../backend/src/GenesysForge.Application/Dtos/HeroicAbilityDto.cs),
[mapper](../backend/src/GenesysForge.Application/Common/Mappers.cs),
[HeroicTab](../frontend/src/components/HeroicTab.tsx),
[localizedDescription](../frontend/src/utils/labels.ts),
[печать](../frontend/src/components/print/CharacterSheetPrint.tsx).

Доработка: провести safe summary до UI/печати через согласованную проекцию, не возвращая private
полное описание. Минимальный вариант без изменения response shape — fallback на safe в существующем
текстовом поле DTO; альтернативу с новым полем согласовать как public API contract.
Приёмка: **PAR-02/HERO-09/PRINT-03**, все 22 улучшения понятны в RU и EN, private text не появляется.

### F-02. Таблицы crafting/alchemy теряют объяснения трат — P1, Code

`SeedData.ProjectContent` очищает `CraftingSpendDef.Description`; `CraftingMapper.ToDto`
передаёт только `s.Description`, хотя safe хранится в entity. DTO не содержит safe.
`CraftingTab` показывает `def.nameRu` и `def.description` напрямую и игнорирует EN-поля.
Затронуты **39 записей**: 21 Item и 18 Potion; у **24** effect = Descriptive.
Для описательных трат проблема особенно существенна: именно текст объясняет результат,
который не исполняется отдельным runtime.

Источники: [CraftingSpendCatalog](../backend/src/GenesysForge.Infrastructure/Persistence/CraftingSpendCatalog.cs),
[CraftingMapper](../backend/src/GenesysForge.Application/Features/Characters/CraftingHandlers.cs),
[DTO](../backend/src/GenesysForge.Application/Dtos/CraftingDtos.cs),
[CraftingTab](../frontend/src/components/CraftingTab.tsx).

Доработка: safe projection в DTO; использовать текущую локаль для имени и объяснения траты.
Приёмка: **PAR-03/CRAFT-07/CRAFT-08**, непустой и пригодный для решения текст у каждой записи,
стоимости/повторяемость/исключения и сохранение выбора остаются прежними.

### F-03. Профили транспорта теряют описание в RU — P1, Code

`MountCatalog` заполняет `SafeDescription = e.Desc`; PrivateFull делает fallback в Description,
PublicSafe его очищает. `MountMapper.DefDto` отдаёт Description и DescriptionEn без safe.
`ShopPage` и `TransportTab` в RU читают только description. Затронуты все **5 bare-кодов**:
`beast-of-burden`, `riding-beast`, `war-mount`, `flying-mount`, `wagon`.
Числа, includedGear и вложенные abilities продолжают передаваться: исчезновение верхнего текста
не означает, что весь транспорт сломан.

Источники: [MountCatalog](../backend/src/GenesysForge.Infrastructure/Persistence/MountCatalog.cs),
[MountMapper](../backend/src/GenesysForge.Application/Common/MountMapper.cs),
[ShopPage](../frontend/src/pages/ShopPage.tsx),
[TransportTab](../frontend/src/components/TransportTab.tsx).

Доработка: сохранить safe description в проекции магазина, полного листа и slices.
Приёмка: **PAR-04/TRAN-01/TRAN-02**, все 5 профилей имеют понятные RU/EN подсказки.

### F-04. Полнота сокращённых подсказок не проверяется механически — P2, Code + Verify

У **32 из 80** quality-каталожных записей RU full длиннее safe; у остальных 48 тексты равны.
У 50 item-кодов в private overlay описание отличается от item safe. Это **не доказывает ошибку
всех 32/50 записей**: сокращение предусмотрено режимом. Требуется содержательная проверка того,
что игрок может понять условие, стоимость, величину, длительность и исключения из safe + structured
fields. Качества с `EffectKind.Descriptive` особенно зависят от текста.

Примеры для первой проверки: `blast`, `burn`, `linked`, `auto-fire`, `guided`, `sunder`,
`superior`, `inferior`, `cumbersome`, `unwieldy`; предметы `rot.item.extra-quiver`,
`rot.item.alchemists-kit`, `rot.item.soulstone-rune`. Не восстанавливать private prose копированием:
написать собственные краткие правила либо вывести уже существующие числовые поля.

`ProjectContent` очищает только RU Description; EN DescriptionEn остаётся. Это не доказанная
copyright-утечка: EN-каталог содержит собственные парафразы. Однако единой формализованной
политики полноты safe RU/EN и её проверки не найдено. Приёмка: **PAR-05…PAR-10**.

## 4. Оптимизация: что уже одинаково и что осталось проверить

### Реализовано в общем коде

- `React.lazy` по страницам; стартовый bundle не содержит все тяжёлые редакторы.
- Memory-кеш reference, character list/sheets/slices/notes/audit/crafting и склейка Promise-запросов.
- Догрузка частей листа по вкладке; `X-Return-Slices` возвращает результат мутации и свежие данные
  одним ответом, вместо обязательного дополнительного GET.
- Узкие EF queries для списка, владения, отдельных чтений и горячих мутаций; SplitQuery снижает
  перемножение Include-коллекций. Это не означает, что каждый обработчик уже оптимален.
- Batch lookup справочников при import вместо одного обращения на строку файла.
- Gzip, immutable hashed assets, API timing event и серверные `app/db/query count` метрики.

Источники: [App](../frontend/src/App.tsx), [client](../frontend/src/api/client.ts),
[SheetPage](../frontend/src/pages/SheetPage.tsx),
[CharacterLoader](../backend/src/GenesysForge.Application/Common/CharacterLoader.cs),
[mutation queries](../backend/src/GenesysForge.Application/Common/CharacterMutationQueries.cs),
[import lookup](../backend/src/GenesysForge.Application/Common/ImportDefinitionSet.cs),
[nginx](../frontend/nginx.conf), [Program](../backend/src/GenesysForge.Api/Program.cs).

### Live-замеры

Загрузка по HTTPS системным curl с проверкой сертификата и `--compressed`. Сначала проверены
обе поверхности; затем 10 health-запросов на домен, чередуя порядок. Это последовательные
малые read-only запросы из одного клиента, **не** нагрузочный benchmark и **не** Core Web Vitals.

| Метрика / артефакт | Private | Public | Интерпретация |
|---|---|---|---|
| Health статусы, n=10 | 10 × 200 | 10 × 200 | БД доступна на момент запросов |
| Health TTFB median, ms | 421.3 | 450.6 | Разница 29.3 ms; сеть/новые соединения влияют |
| Health TTFB min…max, ms | 355.7…658.1 | 353.3…1028.7 | Разброс не позволяет объявить общее отставание |
| Server app median, ms | 1.60 | 3.25 | Микросценарий health, не лист/магазин/стол |
| Server DB median, ms | 0.80 | 0.95 | 1 query; разница очень мала |
| HTML uncompressed / gzip, bytes | 3883 / 1311 | 3883 / 1311 | Одинаковый SHA-256 |
| Main JS uncompressed / gzip, bytes | 313196 / 101062 | 313196 / 101062 | `/assets/index-BfeWa1i1.js`, одинаковый hash |
| JSX runtime uncompressed / gzip, bytes | 9088 / 3537 | 9088 / 3537 | Одинаковый hash |
| CSS uncompressed / gzip, bytes | 113511 / 23744 | 113511 / 23744 | Одинаковый hash |
| Service worker, bytes | 2798 | 2798 | Одинаковый hash |

Main JS SHA-256: `5c539e5f53bce81b8c081b77b0f334a8a6f5f0cd9e0f525aaac2b33bc7655f77`.
HTML SHA-256: `21c034820622aabb3ab1fa649da63d182b092a5ff11f55314c40590cee5c284f`.
Это доказывает совпадение перечисленных ассетов; не доказывает совпадение всех lazy chunks
и backend binary. Авторизованные response bytes, query count и click-to-render не измерены.

### C-01. PWA-кеш персонализированного reference — P1, Code + Verify, общий риск

[VitePWA config](../frontend/vite.config.ts) кеширует `/api/reference` и `/api/spells`
через NetworkFirst на семь дней. Reference включает owner/homebrew/campaign content.
Memory-кеш при logout очищается, но очистка SW CacheStorage или привязка к user/session
**Not found in current codebase**. Один URL может принадлежать разным сессиям одного origin.

Возможный сценарий: A прогрел URL со своим custom-контентом → logout → B → offline/timeout →
NetworkFirst возвращает предыдущий ответ. **Это ещё не воспроизведённая live-утечка.**
Разные домены имеют отдельные origin storage; междоменную утечку этим механизмом не утверждаем.
Доработка после воспроизведения: не кешировать персонализированные ответы либо корректно
изолировать их и удалять при смене сессии. Приёмка: **PWA-03…PWA-06/CACHE-01…CACHE-04**.

### C-02. Нет полноценной проверки PublicSafe за production proxy — P1, Code

Существующий E2E запускает default Docker target `final`, без Content override — PrivateFull.
Он содержит 5 smoke tests, только Desktop Chromium; часть покупок делается API, а не UI.
Это полезная проверка, но она не ловит пустой safe-text именно в public image.
`PublicSafe_StructuralCoverage_MatchesPrivateFull` сравнивает несколько counts и codes талантов,
а не всю структуру всех типов и публичные вложенные DTO. Приёмка: **PAR/API/DEPLOY + smoke**.

### C-03. Скорость общего каталога и отдельных чтений — P2, Code + Verify

`GetReferenceHandler` собирает много коллекций целиком; memory-кеш помогает повторным открытиям,
но не cold start и не контенту нового пользователя. В `GetCraftingProjectsHandler` загружается
полный граф персонажа для проверки владельца перед чтением списка проектов.
Предложение: сначала измерить query count/bytes/render на типовых и больших данных; затем
использовать узкую ownership-проверку и, если оправдано, разделение/кеш built-in каталога с
строгой отдельной проекцией пользовательской части. Не вводить общий кеш пользовательских DTO.
Приёмка: **PERF-03…PERF-09**, отсутствие роста запросов на строку и изменения видимости.

### C-04. Reconnect переподписывает, но не требует свежего snapshot — P2, Code + Verify

[useCampaignHub](../frontend/src/useCampaignHub.ts) после reconnect вызывает SubscribeCampaign,
но не вызывает onGameTableChanged/onCampaignChanged для обязательного catch-up.
Изменения во время разрыва могли быть пропущены. Это общий риск, не доказанная public-only ошибка.
Приёмка: **RT-06…RT-08**, восстановление реального состояния без ручного reload; coalescing
событий проверять по измерениям, чтобы не устроить серию одинаковых GET.

### C-05. OpenAPI не проходит через nginx — P2, Live + Code, общий дефект

`GET /openapi/v1.json` на обоих доменах: **200 `text/html`**, тело совпадает с SPA index.
В backend схема есть; nginx проксирует `/api/` и `/hubs/`, остальные пути обслуживает как SPA.
Scalar настроен читать `/openapi/{documentName}.json`; `/api/docs` отвечает 302.
Доработка: согласованно проксировать schema path или согласованно перенести её в API namespace.
Приёмка: **API-12/DEPLOY-09**, JSON схемы и рабочий Scalar за реальным reverse proxy.

### C-06. Документация и окружения проверок не отражают текущий проект — P2, Code

`docs/ai-context.md`/`docs/testing.md` ещё утверждают отсутствие API versioning, share, import/export,
full print и E2E; код уже содержит их. Компонентных тестов также существенно больше, чем перечислено.
Migration job использует PostgreSQL 16, тогда как production — 17; наполнение базы в job
проверяет seed, но не гарантирует сценарии миграции реальных пользовательских графов.
Это общие пробелы; не требование переписывать рабочие функции. Приёмка: **DEPLOY-02…DEPLOY-06**,
обновлённая документация со ссылками на реальные тесты.

### C-07. Идентификация релиза и web-поверхности — P2/P3, Live + Code

- В compose/runtime status используются `latest`; release/seed identity не доступна через health.
  Добавить безопасную идентификацию сборки/каталога и smoke по двум доменам. Не публиковать secrets.
- Manifest на обоих доменах отдан как `application/octet-stream`; проверить installation на
  целевых браузерах и корректный manifest MIME. Это не доказательство, что PWA не устанавливается.
- Метрики бизнес-сценариев и контролируемый performance budget пока не зафиксированы.
  Порогам из тестового плана дан статус **Assumption**, они не выдаются за текущие результаты.

## 5. Приоритизированный список доработок

Размер: S — локальная проекция/UI/тест; M — несколько поверхностей; L — интеграционный стенд или
нагрузочные сценарии. Это относительная оценка объёма, не обещание срока.

| ID | Приоритет | Объём | Доработка | Зависимости / критерий |
|---|---|---|---|---|
| PUB-01 | P1 | S | Safe Power-upgrade text в DTO, UI и print | F-01; 22/22 RU/EN; HERO-09, PRINT-03 |
| PUB-02 | P1 | S/M | Safe crafting spend projection и локализация таблиц | F-02; 39/39; CRAFT-07/08 |
| PUB-03 | P1 | S | Safe transport text в shop/sheet/slices | F-03; 5/5; TRAN-01/02 |
| PUB-04 | P1 | M | Воспроизвести и закрыть SW user-context fallback | C-01; PWA-03…06; сначала regression reproduction |
| PUB-05 | P1 | M/L | Public-image API/UI smoke и каноническая parity-матрица | C-02; public target + PostgreSQL + proxy |
| PUB-06 | P2 | M | Проверить достаточность safe summary + чисел, единая RU/EN политика | F-04; PAR-05…10; own short paraphrases |
| PUB-07 | P2 | M | Release/image/seed identity и два post-deploy smoke | C-07; digest/commit не угадывать по latest |
| PUB-08 | P2 | M/L | Baseline лист/магазин/reference/table, бюджеты round trips/query/bytes | C-03; PERF-01…14; оптимизировать после замеров |
| PUB-09 | P2 | S | Узкое чтение ownership для crafting list | C-03; PERF-08; сохранение результатов/доступа |
| PUB-10 | P2 | M | Catch-up snapshot после reconnect и тесты нескольких клиентов | C-04; RT-06…08 |
| PUB-11 | P2 | S | Рабочая OpenAPI schema за proxy | C-05; API-12/DEPLOY-09 |
| PUB-12 | P2 | M | PublicSafe migration/seed tests на PostgreSQL 17 с пользовательскими данными | C-06; DEPLOY-02…06 |
| PUB-13 | P2 | S/M | Актуализировать AI/testing/operations docs и индекс реального покрытия | C-06; не помечать планы как выполненные тесты |
| PUB-14 | P3 | S/M | Manifest MIME, PWA install/update, visual/mobile/a11y regression | C-07; PWA/WEB; повысить приоритет при воспроизведённом blocker |

Порядок: PUB-01…03 → PUB-04/05 → PUB-06/07/10/11/12 → baseline PUB-08 → подтверждённые hot paths
PUB-09 и дополнительные оптимизации → PUB-13/14. PUB-04 стоит проверить рано из-за изоляции
пользовательского контента. Ни одна доработка в этом аудите не реализована.

## 6. Что не считать отставанием публичной версии

- Предусмотренное сокращение safe prose, если structured fields и подсказка сохраняют смысл.
- Разные UUID/пользователи/кампании и пользовательские объекты в отдельных БД.
- Функции, уже существующие в общей реализации: JSON import/export, public share, print, E2E,
  lazy loading, memory caches, slices и realtime. Их следует проверять, не создавать заново.
- Автоматический полный encounter runtime, тики длительностей, полное списание ингредиентов,
  боеприпасов и все описательные эффекты: принятые исключения в
  [rot-runtime-out-of-scope.md](../roadmap/tasks/rot-runtime-out-of-scope.md).
- Открытый PR #256 с расширением purchase choices: он пока не входит в подтверждённый master.
  Для требующих выбора талантов тестировать фактический master и отдельно фиксировать принятый gap.
- Ограничение PC Defense не переносить на NPC: владелец разрешил NPC Defense выше 4.

## 7. Непосредственный следующий шаг

Подготовить парные безопасные fixtures на staging PublicSafe/PrivateFull одного commit и
запустить smoke из тестового плана. Три ошибки проекций имеют достаточное code evidence для
отдельных исправлений; production-подтверждение и регрессии должны проверять реальные public
DTO и экран. Остальные гипотезы переводятся в дефекты только после воспроизведения/замера.

Документ не содержит оригинальных текстов книг, private overlays, пользовательских данных,
паролей, токенов или секретов. Изменения аудита — только Markdown.
