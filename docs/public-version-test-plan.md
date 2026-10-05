# Полный план тестирования публичной GenesysForge

**Объект:** https://genesys-forge.com, PublicSafe. **Эталон функциональности:**
https://app.genesys-forge.com, PrivateFull. **Срез кода:** `origin/master`, `4da4636`, 05.10.2026.
Основание: [аудит и список доработок](public-private-audit-2026-10-05.md).
Полнота HTTP-поверхностей: [инвентаризация API](public-api-test-inventory.md).

**Это план будущих проверок, не отчёт об их прохождении.** Выполненные read-only проверки
перечислены в аудите. Ни один сценарий ниже не считается Passed без отдельного результата.
План содержит **373 сценария в 36 семействах**, дополненных параметрическими вариантами,
матрицей устройств/ролей и инвентаризацией 167 API-маршрутов.

## 1. Цель и проверяемый контракт

Публичная версия должна позволять совершать те же поддержанные действия и получать те же
механические результаты, что эталон, без private full prose. Сравнивать не длину текста,
а доступность действия, валидацию, стоимость, persisted outcome, вычисления, visibility,
понятность безопасной подсказки и скорость на одинаковых fixtures.

При расхождении:

1. Проверить commit/image/seed identity и одинаковость fixtures.
2. Воспроизвести действие в PrivateFull того же commit.
3. Если различается только PublicSafe — public regression. Если ошибка есть в обоих — общий дефект.
4. Не менять game rules ради совпадения с UI. Expected задаётся текущим Domain/Application и
   принятым объёмом [ТЗ](../roadmap/tasks/rot-rules-remediation-tasks.md), с учётом
   [исключений runtime](../roadmap/tasks/rot-runtime-out-of-scope.md).
5. Новая желаемая функция записывается как change request, а не как провал текущего контракта.

## 2. Стенд, роли и тестовые данные

Основной прогон — на изолированном staging: public Docker target, `IncludePrivateContent=false`,
`Content__Mode=PublicSafe`, PostgreSQL 17, production frontend/nginx и TLS/reverse proxy.
Парный стенд — PrivateFull **того же commit** с собственным DB/volume/JWT/storage prefix.
Не запускать мутирующие smoke, stress, rate-limit flood, fault injection или restore на реальных
пользовательских production данных. Production smoke использует выделенные fixtures и разрешённый
объём действий; read-only health/static checks можно повторять без создания игровых данных.

| Fixture | Состав / назначение |
|---|---|
| U0 | Аноним; новый browser context без cookies, localStorage и SW |
| U1, U2 | Два независимых игрока; одинаковые названия custom-записей, разные владельцы |
| GM1, GM2 | Два ведущих; разные кампании и NPC, чтобы проверить isolation |
| P1, P2, OUT | Два участника кампании GM1 и посторонний пользователь |
| CORE-C | Core creation character; свободные и платные ранги, 500 standard money |
| ROT-C | RoT creation character; видовые выборы, career gear и героика |
| CORE-A, ROT-A | Завершённое создание; snapshots порогов и progression XP |
| LOW-XP | Available XP: цена−1, цена, цена+1; spent/total граничные сочетания |
| HERO-XP | Starting XP + 49, +50, +99, +100; identity/parameter incomplete и complete |
| BIG-SHEET | Большой, но валидный лист: 100 item instances, 40 талант-покупок, 100 notes, 1000 audit entries; валидность пирамиды и лимитов обеспечивается fixture generator |
| EQUIP | Несовместимая броня/слоты, щит, quality bonuses, materials, attachment host, damage |
| TRANS | Все 5 transport codes; свободный/полный/перегруженный cargo, разные traction links |
| CRAFT | Item/Potion/Enchantment, успех/провал; 39 spend codes, 12 алхимических целей |
| CAMP | Валидный/неизвестный join code, персонажи двух игроков, hidden/visible notes и encounters |
| NPCS | Minion/Rival/Nemesis; private/campaign-visible/public-template; built-in/retired |
| TABLE-S/M/L | 5/20/50 участников; PC/NPC/minion groups, hidden state, initiative/range |
| CUSTOM | Skill/talent/item/heroic/archetype/career; own/foreign/campaign/pack контекст |
| IMPORT | Валидные v1…v8, неизвестный формат, битый JSON, unresolved code, чужой custom code |
| TEXT | RU/EN, кириллица, emoji, переносы, длинные слова, Markdown и безопасные XSS-пробы |
| FILES | Допустимые PNG/JPEG/WebP и реальные invalid/truncated/oversize файлы по upload validator |
| SAFE-MARKER | Синтетический private marker в permitted private fixture и отдельный safe marker; никаких оригинальных текстов книг; markers проверяются во всех public responses/caches |

Для каждого семейства: happy path, границы, invalid input, ownership, reload persistence,
ошибка сети и одинаковый mechanical outcome в парном стенде. Применять только осмысленные
сочетания: например, героика недоступна Core, а NPC Defense не ограничивается PC-правилом 4.

**Общие наборы границ:** число −1/0/1/max−1/max/max+1, дробь для целого, int overflow,
null/отсутствующее/пустое, неверный enum; строки whitespace/максимальная длина/длина+1,
RU/EN/emoji; коллекции пустая/один/duplicate/большая. Максимумы брать из validator/DTO текущего
commit. Отсутствующий лимит: **Not found in current codebase**, затем отдельно определить
желаемый защитный контракт; не придумывать Passed по несуществующему ограничению.

## 3. Уровни, приоритеты и исполнение

- **D:** xUnit Domain — числовые правила без UI/БД.
- **A:** API/integration — public projection, authorization, persistence; PostgreSQL для SQL,
  транзакций, constraints, concurrent writes и миграций. InMemory не заменяет PostgreSQL.
- **F:** Vitest/RTL — чистые helpers, API client, локализация и компоненты с реалистичными DTO.
- **E:** Playwright — настоящий public image/proxy, действия пользователя и несколько клиентов.
- **M:** ручной/визуальный/операционный прогон — print, touch, assistive tech, SMTP/OAuth,
  performance/backup/restore.

P0 — блокировка выпуска при утечке данных, повреждении/потере persisted state, обходе ownership,
недоступности входа/основного листа или неверной финансовой/XP-мутации. P1 — основная механика,
safe text и regressions PUB-01…05. P2 — остальные поддержанные сценарии. P3 — polish.
Приоритет кейса определяется этой шкалой и воспроизведённым влиянием; весь документ не объявлен P0.

Каждая строка ниже — самостоятельный сценарий, а наборы параметров разворачиваются в варианты.
Уровни задают рекомендуемый способ проверки, не требуют бессмысленного дублирования всех слоёв.

### WEB — входная поверхность, маршруты и навигация (E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| WEB-01 | U0 открыть `/`, `/login`, `/register` | Видимый вход, без бесконечного loader и ошибок chunks |
| WEB-02 | U0 открыть about/help, ссылки footer | Доступ без входа; правильные страницы и лицензия |
| WEB-03 | После входа пройти все sidebar sections и Back/Forward | URL, active section и содержимое согласованы |
| WEB-04 | Открыть character/campaign/NPC/table/print deep link, reload | Восстановлен нужный объект/подвид, доступ проверен |
| WEB-05 | Неизвестный путь, отсутствующий объект, invalid GUID | Понятная ошибка/NotFound, без чужого объекта и crash |
| WEB-06 | 320/360/390/768/1024/1440 px, portrait/landscape | Основные действия доступны; нет перекрытого меню/кнопок |
| WEB-07 | Keyboard Tab/Shift-Tab/Enter/Escape в меню и modal | Порядок focus, доступное закрытие и возврат focus |
| WEB-08 | RU→EN→RU, reload и новый раздел | Локаль сохраняется; тексты/labels меняются согласованно |
| WEB-09 | После expiry перейти login и войти снова | Возврат к ожидаемому deep link, без петли входа |
| WEB-10 | Недоступный lazy chunk после deploy, slow/offline | Понятное восстановление; нет бесконечного пустого экрана |

### AUTH — регистрация, вход и сессии (A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| AUTH-01 | Новый e-mail, допустимый password/displayName; UI register | Создан один user; доступен пустой собственный список |
| AUTH-02 | Duplicate e-mail, различный case/whitespace | Политика нормализации соблюдена; второго аккаунта нет |
| AUTH-03 | Неверный e-mail/password, пустые и граничные поля | Ошибки понятны; сервер не принимает обход UI validation |
| AUTH-04 | Верный и неверный password; unknown e-mail | Успех только при верных данных; ошибок с secret нет |
| AUTH-05 | Reload с JWT; без JWT, но с refresh cookie | Сессия корректно восстанавливается; запросы не зацикливаются |
| AUTH-06 | JWT expired; одновременно 5 защищённых GET | Один refresh; допустимые запросы повторены один раз |
| AUTH-07 | Нет/отозван refresh, protected GET | Сессия завершается; memory caches очищены, показан login |
| AUTH-08 | Rotate refresh; повторно использовать старый token | Семейство отозвано по контракту; replay не продлевает сессию |
| AUTH-09 | Logout, повторный refresh и reload | Cookie очищен; отозванное семейство не возвращает вход |
| AUTH-10 | Две вкладки одновременно refresh/logout | Нет вечной 401-петли; состояние каждой вкладки понятно |
| AUTH-11 | Проверить cookies в HTTPS production topology | HttpOnly/Secure/SameSite и host/path соответствуют контракту |
| AUTH-12 | Public JWT на private API и наоборот | Защищённые запросы отклонены; чужой stack не авторизован |
| AUTH-13 | Google enabled/disabled, правильный/неверный audience | Кнопка и server validation согласованы; invalid token отклонён |
| AUTH-14 | Existing/new Google account, cancel/provider failure | Нет дублей или зависшего UI; повторный вход возможен |
| AUTH-15 | Password-reset request для known/unknown e-mail | Ответ не раскрывает существование account; письмо ведёт на public host |
| AUTH-16 | Валидный/expired/reused reset; смена пароля и старые refresh | Token одноразовый; новый вход работает; старые сессии отозваны |

### PROF — профиль и изображения (A/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| PROF-01 | Читать/изменить displayName; reload | Сохраняется у владельца, не меняет игровые данные |
| PROF-02 | Загрузить avatar и character portrait, reload | URL/изображение доступны; правильный объект обновлён |
| PROF-03 | Invalid format, spoofed MIME, truncated/oversize image | Сервер валидирует bytes; ошибка без частичной записи |
| PROF-04 | Upload в чужой character и при отключённом storage | Ownership enforced; понятное сообщение о недоступности |
| PROF-05 | Storage timeout; повторная загрузка | Предыдущий working URL сохраняется либо явная согласованная ошибка |
| PROF-06 | Проверить object key/prefix и CDN после deploy | Public не записывает в private namespace; URLs не содержат credentials |
| PROF-07 | Change password: верный/неверный current, invalid new password | Validation до записи; старый password не работает, новый работает |
| PROF-08 | Change password при нескольких refresh sessions | Остальные refresh отозваны; текущему устройству выдан новый cookie |

### API — применить к каждому маршруту инвентаризации (A/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| API-01 | Валидный positive request для каждой строки inventory | Правильный status/body/headers, persisted outcome |
| API-02 | Повторить через `/api/` и `/api/v1/` | Совпадают механика, validation, ownership и shape |
| API-03 | Без/invalid/expired JWT на protected route | Нет доступа; ошибки клиентом обработаны даже при пустом 401 body |
| API-04 | Подменить owner/campaign/character/child ID | Чужой объект не читается и не мутируется |
| API-05 | Invalid GUID, enum, JSON, null, duplicate collection | 4xx по контракту; не необработанный 500/частичная запись |
| API-06 | Content-Type/body отсутствует или не соответствует route | Явная ошибка; endpoints не интерпретируют мусор как действие |
| API-07 | DomainRule/Conflict/Unauthorized случаи | message/reasonCode/status соответствуют текущему mapping |
| API-08 | `X-Return-Slices` без/с набором/неверным набором | Legacy shape сохранён; свежие части или documented fallback |
| API-09 | Created item/mount против duplicate/import/share response | createdId не потерян; чужой новый объект не заменён исходными slices |
| API-10 | 204/empty body, non-JSON 502/503, timeout | Клиент не падает на JSON parse; показывает пригодную ошибку |
| API-11 | OPTIONS/CORS, forwarded HTTPS, auth rate limits | Только разрешённые origins; правильный IP/cookie/429; isolation |
| API-12 | `/openapi/v1.json`, Scalar за nginx | Schema JSON, документирует реальные routes; HTML SPA не принят за schema |

### PAR — обязательный PublicSafe/PrivateFull паритет (A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| PAR-01 | Канонически сравнить все built-in active/retired каталоги | Code-наборы, structured fields и доступность равны, UUID исключены |
| PAR-02 | Все 22 heroic Power upgrades: reference/sheet/print, RU/EN | Safe explanation непустое; private marker отсутствует |
| PAR-03 | Все 39 crafting spends в PublicSafe | Safe объяснение, цена, repeatability и parameter доступны |
| PAR-04 | Все 5 transport codes: shop/sheet/slices | Safe RU/EN описание сохранено, механические поля равны |
| PAR-05 | Все 80 qualities; особенно 32 с разным full/safe | Условия/стоимости/числа читаются из safe + structured fields |
| PAR-06 | Item overlays и gameplay tools/implements/shards | Нужные действия понятны; отсутствие full не скрывает правило |
| PAR-07 | Talents, species/career abilities, spells/effects, secondary effects | Safe text хватает для выбора; метаданные/числа совпадают |
| PAR-08 | RU/EN переключение по каждому виду контента | Одинаковая механика; нет пустого языка при наличии safe/EN |
| PAR-09 | SAFE-MARKER через вложенные DTO, search, share, export/print | Ни одна публичная поверхность не возвращает private marker |
| PAR-10 | Проверить public assembly/resources и downloaded assets | Private overlays не входят в public publish/runtime artifacts |
| PAR-11 | Ресеедить каждый режим 2 раза на наполненной БД | Нет дублей/смены IDs/потерь custom content; изменения built-in обновлены |
| PAR-12 | Одинаковая цепочка create→buy→equip→finish→export в двух стендах | Canonical persisted mechanics равны; допустимы различия текстов |

### CRE — создание персонажа (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| CRE-01 | Создать Core/RoT со всеми active archetypes/careers | Правильная система, стартовые характеристики/XP/skill grants |
| CRE-02 | Переключать систему/вид/карьеру в форме | Несовместимые прежние selections сброшены; valid submit |
| CRE-03 | Retired/foreign/custom другого владельца IDs | Новое создание отклонено; разрешённый own custom доступен |
| CRE-04 | Все варианты видовых стартовых skill choices | Требуемое число и разрешённые skills; нет duplicate grant |
| CRE-05 | Free career skills: 0/required/extra/duplicate/не из списка | Сервер обеспечивает правила выбора и лимит creation rank |
| CRE-06 | Совпадение видового и карьерного skill grant | Ранги/происхождение начислены по текущему resolver |
| CRE-07 | Standard money против career kit | Взаимоисключение; standard 500, kit выдаёт целый комплект |
| CRE-08 | Все 8 career-extras, ветви gear choices, Scout | Обязательные выборы, предметы, quantity и money корректны |
| CRE-09 | Комплект с fractional/negative/custom-foreign choice | Invalid отказ до записи; UI показывает читаемую причину |
| CRE-10 | Двойной submit и сетевой обрыв создания | UI предотвращает случайный дубль; результат можно однозначно проверить |
| CRE-11 | Завершить complete/incomplete character, повторить | Gate enforced; creation false; snapshots зафиксированы один раз |
| CRE-12 | Clone персонажа, reload обеих копий | Независимые instances/ownership; существующий character не изменён |

### XP — характеристики и навыки (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| XP-01 | Buy characteristic 2→3, XP=29/30/31 | Цена 30; при недостатке полная атомарность |
| XP-02 | Creation characteristic до 5 и попытка 6 | 5 допустимо по правилу; 6 creation отклонено |
| XP-03 | После finish buy/refund characteristic | Недопустимая покупка/возврат отклонены |
| XP-04 | Career/non-career skill ранги 1…5 | Цена 5×newRank, +5 non-career; точное списание |
| XP-05 | Creation rank 2→3, active rank 5→6 | Creation cap 2, overall cap 5 enforced |
| XP-06 | Refund платного и бесплатного стартового rank | Возвращается только допустимая платная покупка, не free grant |
| XP-07 | XP Total меньше Spent; отрицательные/overflow values | Нельзя получить invalid available XP или partial update |
| XP-08 | Снизить XP около heroic-point thresholds | Нельзя оставить незаконную progression; корректный reasonCode |
| XP-09 | Два одновременных buy при XP ровно на один | Нет overspend/lost update; итог соответствует допустимому порядку |
| XP-10 | Buy/refund→history→reload→export | Spent/available/ranks/history отражают одно и то же состояние |

### TAL — таланты и пирамида (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| TAL-01 | Все active talent codes, Core/RoT setting filters | Core Any; RoT Any+Fantasy; own custom по правилам visibility |
| TAL-02 | Buy tiers 1…5 на valid/invalid pyramid | После покупки нижний tier строго больше верхнего |
| TAL-03 | Refund lower-tier, нарушающий pyramid | Отказ без изменения XP/талантов; разрешённый refund проходит |
| TAL-04 | Ranked покупки, baseTier+ranks с потолком 5 | Effective tier/cost и tier counts каждой покупки верны |
| TAL-05 | Unranked duplicate и недостаточный XP | Отказ; нет второй строки и неправильного списания |
| TAL-06 | Prerequisite и exclusions, retired/чужой talent | Правила применены до мутации; никакого обхода по ID |
| TAL-07 | Dedication characteristic selection, max 6 | Обязательный выбор; корректная характеристика и бонус |
| TAL-08 | Существующие на master parameter/choice schemas | Требуемые choices сохраняются; отсутствие нового UI не считать уже исправленным |
| TAL-09 | Несколько ranked grants career skill, refund source | Выдача/исчезновение навыка по принятой текущей модели |
| TAL-10 | Passive bonuses + equipment, после buy/refund | Derived stats/dice изменяются и возвращаются ожидаемо |
| TAL-11 | Купленный retired talent на старом character | Сохранён/показан; не предлагается в новых покупках |
| TAL-12 | Двойной click/concurrent buy/refund, reload | Ни дублирования unranked, ни нарушения pyramid/XP |

### HERO — героические способности RoT (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| HERO-01 | Назначить Core и RoT character | Core отклонён; RoT разрешён по правилам |
| HERO-02 | Identity name/origin manual/d100, incomplete | Gate, faces/origin и сохранение результата корректны |
| HERO-03 | StartingXP +49/+50/+99/+100 | Всего очков 0/1/1/2, независимо от Spent XP |
| HERO-04 | Power Improved/Supreme, пропуск уровня | Стоимость 1 затем 2; Supreme без Improved нельзя |
| HERO-05 | Duration/Frequency повторяемо, Story один раз | Стоимость 1/2/1; нет второго Story upgrade |
| HERO-06 | Secondary effects: 0/1/2/3, duplicate, unknown | Не более двух разных, допустимых и оплаченных |
| HERO-07 | Refund при creation и после finish | Creation по контракту; permanent purchases после creation |
| HERO-08 | Paragon/Sixth Sense/Signature Weapon parameters | Полнота/видимость/совместимость и persistent выбор |
| HERO-09 | PublicSafe все 22 улучшения, RU/EN и print | До покупки и после неё есть безопасное объяснение эффекта |
| HERO-10 | Signature profile/material/base/Improved/Supreme attachment | Корректные ограничения, qualities/price и фактический attack profile |
| HERO-11 | Active supported effect против descriptive-only | Реальные effects вычислены; отсутствие runtime не изображает исполнение |
| HERO-12 | Concurrent upgrades/XP reduction, reload/export/import | Нет overspend/config inconsistency; параметры сохранены |

### DER — derived stats, dice pools и здоровье (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| DER-01 | Characteristic/ranks: 0, меньше/равно/больше | proficiency=min, ability=max−min; корректная визуализация |
| DER-02 | Creation wound/strain base + Brawn/Willpower + bonuses | Порог рассчитан из текущего правила |
| DER-03 | Finish→Dedication/другой рост характеристик | Frozen threshold snapshots сохраняются; допустимые бонусы работают |
| DER-04 | Несколько броней, active armor selection и shield | Один разрешённый armor provider, корректный soak/слоты |
| DER-05 | Defense providers/increases, предел PC=4 | Правильная агрегация; NPC не ограничен PC-пределом |
| DER-06 | Equipped armor Enc≤3/>3, quantity; unequip | Нагрузка `max(0,enc−3)×qty`, общие Enc rules применены |
| DER-07 | Load threshold−1/=threshold/+1, cargo on transport | Encumbered только сверх порога; cargo не удваивается |
| DER-08 | Items/material/attachments/damage/modifiers | Пулы и derived одинаковы на sheet, slices, shop и table |
| DER-09 | Wounds/strain current границы, critical injuries add/remove | Persisted здоровье и предупреждения по контракту |
| DER-10 | Invalid health/bonus values и concurrent counters | Нет незаконного числа/потери обновления; error и rollback |

### INV — инвентарь и экипировка (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| INV-01 | Add item: compatible instances, quantity 1/N | Stacking только по правилам экземпляра, корректный createdId |
| INV-02 | Crafted/custom/material/damage/attachment различия | Несовместимые instances не сливаются |
| INV-03 | State carried/equipped/stored и смена quantity | Пересчитаны load/soak/defense, persisted state |
| INV-04 | Conflict equipment slots, duplicate active armor | Server проверяет слоты; UI объясняет отказ |
| INV-05 | Remove item с attachments/cargo links | Нет orphan/неучтённого бонуса; policy зависимостей соблюдена |
| INV-06 | Consumable use: available/insufficient quantity | Точное уменьшение; нельзя расходовать отсутствующее |
| INV-07 | Damage states intact/minor/moderate/major/broken | Profile/check penalties и unusable state соответствуют rules |
| INV-08 | Repair preview/apply, invalid status/reason | Серверный расчёт и записанное состояние согласованы |
| INV-09 | Alternate attack profiles, shield и composite qualities | Правильные skill/damage/crit/range и tooltips |
| INV-10 | Custom item, retired def, unknown/foreign ID | Own visibility; старое instance доступно, новая запрещённая покупка нет |
| INV-11 | Fast +/−/equip/remove с delay/reversed responses | UI не откатывается к старому состоянию; серверный итог виден |
| INV-12 | Reload/print/export после всех состояний | Экземпляры и числа одинаковы во всех поверхностях |

### ECO — магазин и экономика (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| ECO-01 | Категории/поиск/фильтры Core/RoT, пустой результат | Состав и stats корректны; скрытые/retired не появляются |
| ECO-02 | Покупка unit price 151, qty 2, 50/100/150/200% | floor unit×percent, затем qty; цена считается сервером |
| ECO-03 | 49/51/201%, invalid quantity, forged cost | Недопустимые границы/шаг отклонены; client total не авторитетен |
| ECO-04 | Override price с/без reason, одновременно percent | Причина обязательна, взаимоисключение способов соблюдено |
| ECO-05 | Money = цена−1/цена/цена+1, double buy | Нет отрицательного кошелька/overspend; атомарность |
| ECO-06 | Sale successes 0/1/2/3/4, odd unit price/qty | 0/25/50/75/75%, округление вниз за единицу |
| ECO-07 | Condition multiplier/reason, forged proceeds | Параметры валидируются; фактическая выручка серверная |
| ECO-08 | Rarity 0…11+, market conditions, forbidden ownership | Difficulty ladder и upgrades >10, правильные modifiers |
| ECO-09 | Price=null relic против GM grant/custom override | Обычная покупка/продажа закрыта; только поддержанная выдача |
| ECO-10 | Услуги/еда/ночлег против craftable goods | Корректная витрина; недопустимые услуги не становятся crafting target |

### ATT — attachments, craftsmanship и implements (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| ATT-01 | Все active attachment codes и host restrictions | Совместимые доступны, incompatible объяснены |
| ATT-02 | Hardpoints free/exact/insufficient; concurrent install | Нельзя превысить слоты; бонус не применяется дважды |
| ATT-03 | Traits required/all/any/forbidden, enchantment host | Общие predicates совпадают в UI/server |
| ATT-04 | Attach→detach с outcomes, повторный detach | Сохранение/потеря по контракту; никаких orphan bonuses |
| ATT-05 | Base→craftsmanship→attachments→damage pipeline | Порядок и effective properties/profile стабилен |
| ATT-06 | Quality code/rating increase, invalid rating/unknown code | Canonical names и ограничения; invalid не подменяет правило |
| ATT-07 | Signature weapon attachments и обычный inventory | Особый путь не обходит совместимость и rarity limits |
| ATT-08 | Implement materials, fixed material и magical eligibility | Цена/эффекты/доступность по текущему domain |
| ATT-09 | Safe tooltip в shop/inventory/signature/print | Понятная качественная механика и язык; private marker отсутствует |
| ATT-10 | Reload/export/import с attachment graph | Host associations и числовой результат сохранены |

### TRAN — скакуны, повозки и груз (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| TRAN-01 | PublicSafe 5 profiles в магазине RU/EN | Непустая safe подсказка, stats/source/price/capacity |
| TRAN-02 | Купить/выдать, имя, wounds, reload sheet/slices | Сохранённое состояние и safe description во всех проекциях |
| TRAN-03 | Minion/Rival/Nemesis profiles и riding stress | Kind/strain/group skills и необходимость проверки по правилам |
| TRAN-04 | Move cargo character→mount→другой mount→назад | Вес принадлежит одному holder; нет дублей/потерь |
| TRAN-05 | Cargo capacity−1/=capacity/+1, equipped item | Capacity/state rules соблюдены; недопустимое перемещение отклонено |
| TRAN-06 | Wagon без/с разрешённой traction link | Неподвижность/тяга и related warnings корректны |
| TRAN-07 | Цикл/self/foreign/removed traction или holder | Invalid links не сохраняются; graph остаётся целым |
| TRAN-08 | Удаление транспорта с грузом/привязкой | Предсказуемая domain policy; вещи не исчезают молча |
| TRAN-09 | Duplicate/import v4…v8 и legacy inventory mount | Нормализация и cargo provenance, без двойного транспорта |
| TRAN-10 | Concurrent cargo move, reload/export/print | Нет двух holders или лишней нагрузки; canonical итог |

### CRAFT — изготовление, алхимия, зачарование (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| CRAFT-01 | Item/Potion/Enchantment preview, все recipe kinds | Правильный каталог/skill; preview не пишет и не сбрасывает cache |
| CRAFT-02 | Rarity 0/1/2/5, odd price | ceil(rarity/2), time=1+rarity, ceil(price/2); units hours/days |
| CRAFT-03 | Cost percent/override, difficulty/time override без reason | Валидные вычисления; причины и взаимоисключения обязательны |
| CRAFT-04 | 12 potions против обычных items/услуг/priceless | Correct kind/targets; invalid server обход UI отклонён |
| CRAFT-05 | Enchantment основа Superior, неверная основа/skill | Требования enforced; способность записана в исходный instance |
| CRAFT-06 | Успех/провал/cancel/double resolve | Один lifecycle outcome; повторное разрешение отклонено |
| CRAFT-07 | PublicSafe все 21 Item +18 Potion spends | Safe текст объясняет результат каждого выбора |
| CRAFT-08 | Переключить локаль во время выбора | Name/description локализуются; выбранные codes не меняются |
| CRAFT-09 | Symbol budgets, repeatability, exclusive row, required parameter | Сервер не допускает перерасход/несовместимые/неполные choices |
| CRAFT-10 | Quality increase, weapon-only, combine dose rarity | Restrictions enforced; execution только поддержанных effects |
| CRAFT-11 | Wallet/ingredients перед и после, descriptive effects | По принятому scope ничего не списано; descriptive choice записан |
| CRAFT-12 | Crafted instance, history/CraftNote, reload/export/attachments | Выборы сохранены; crafted не стакается с купленным; profile корректен |

### MAG — магия, builder, shards и knowledge (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| MAG-01 | Все spell/effect codes Core/RoT, retired/own custom | Разрешённый состав, source и safe RU/EN |
| MAG-02 | Allowed/restricted skills, untrained magic | Матрица доступности и ошибки совпадают с backend |
| MAG-03 | Basic difficulty + optional effects + implements | Итог difficulty и dice pool по правилам |
| MAG-04 | Repeatable add/remove, максимумы и overflow | Только допустимые counts; нет отрицательной difficulty |
| MAG-05 | Exclusions, Curse combinations, reset skill/action | Invalid combinations blocked; старые effects не протекают |
| MAG-06 | Knowledge rank 0/1/2/5, rated qualities | Rating и tooltip резолвятся по коду, не локализованной строке |
| MAG-07 | Swift/Haste, talent free effects по текущему master | Допустимые бесплатные эффекты/цены, без двойной скидки |
| MAG-08 | Implement материал/качество/damage state | Effects/limits и итоговый damage соответствуют pipeline |
| MAG-09 | Lesser Rune config: action/effect/invalid combination | Обязательная persistent настройка, видимость и ограничения |
| MAG-10 | Runebound shards доступность, rename/import | Stable code linkage и механические поля сохраняются |
| MAG-11 | Conjure dispositions/extra summon, Augment rule hint | Безопасная подсказка отражает принятые правила; extra summon cost 2 advantages |
| MAG-12 | Roller net successes/advantages/triumph, damage spends | UI-calculator и server resolve дают согласованный результат |
| MAG-13 | RU/EN copy/print карточки, длинные effects | Правильный язык/числа/переносы; safe text вместо private full |
| MAG-14 | Reload/смена character/ошибка загрузки reference | Нужный контекст, без stale effects чужого персонажа |

### COMBAT — разрешение атак и качеств (D/A/F)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| COMBAT-01 | Net successes −1/0/1/N | Обычный weapon damage только при попадании |
| COMBAT-02 | Base+successes против soak 0/=damage/>damage | Корректный raw/applied, без отрицательного damage |
| COMBAT-03 | Дополнительные удары с разным target soak | Каждый удар применяет своё поглощение, правильная сумма |
| COMBAT-04 | Pierce/Breach/Reinforced и сочетания ratings | IgnoreSoak и protection по domain rules |
| COMBAT-05 | Advantages/Triumph budgets, requiresHit/throughSoak | Недопустимая трата отклонена; допустимая не оплачена дважды |
| COMBAT-06 | Miss и разрешённые специальные траты | Только явно разрешённые эффекты; не обычный hit damage |
| COMBAT-07 | Unknown/duplicate quality codes, invalid ratings | Неизвестное не превращается в правило; нет crash/overflow |
| COMBAT-08 | Supported automatic против Descriptive quality | Результат обозначает реальный объём автоматизации |

### DICE — роллер и журнал бросков (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| DICE-01 | Все типы dice, 0/1/N, upgrade/downgrade difficulty | Валидный состав pool и отображение symbols |
| DICE-02 | Success/failure, advantage/threat cancellation | Net symbols правильно сокращены; triumph/despair сохраняются |
| DICE-03 | Exhaustive faces и фиксированный RNG | Число/символы граней и алгоритм проверены детерминированно |
| DICE-04 | Увеличить/decrease/custom pool, reset | Нет отрицательных dice/count и stale state |
| DICE-05 | Send campaign roll, private/nonmember access | Correct ownership/visibility, без чужих rolls |
| DICE-06 | Journal limit/order, одинаковые timestamps | Детерминированный вывод, нет пропусков/дублей |
| DICE-07 | Network failure/duplicate submit и realtime event | Поведение повтора явно проверено; результат не притворяется сохранённым |
| DICE-08 | RU/EN labels, symbol colors, accessibility | Символы различимы текстом, не только цветом |

### NOTE — заметки персонажа (A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| NOTE-01 | Create/edit/delete own note, reload | Сохранение ровно нужной заметки |
| NOTE-02 | Foreign character/note ID и child-parent mismatch | Нет чтения/редактирования чужого объекта |
| NOTE-03 | Empty/long/Markdown/emoji и безопасный XSS text | Validation/санитизация, переносы; script не исполняется |
| NOTE-04 | Concurrent edits/delete, delayed response | Нет silent lost update либо явная документированная политика |
| NOTE-05 | Network error при save | Draft не исчезает молча; пользователь понимает несохранённый статус |
| NOTE-06 | New session/character, cache/invalidation | Не показываются прежние notes другого owner/character |

### HIST — история и undo (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| HIST-01 | Каждая поддержанная XP/item/trade/config мутация | Правильные action, value/reason и timestamp, одна запись |
| HIST-02 | Неуспешная мутация и preview | Нет записи успешной покупки или лишнего изменения |
| HIST-03 | Undo допустимой skill/talent покупки при creation | Верный refund и audit, исходное действие помечено корректно |
| HIST-04 | Undo с dependent ranks/pyramid, после finish | Invalid undo отклонён до изменения |
| HIST-05 | Повторный/concurrent undo | Нельзя дважды получить XP/испортить grants |
| HIST-06 | `take` −1/0/1/100/500/501 при 1000 entries | Clamp 1…500, последние записи в правильном порядке; приемлемая latency |
| HIST-07 | Foreign history ID/character | Нет чужого audit/undo |
| HIST-08 | RU/EN, reasonCode сообщения и reload after undo | Интерфейс/XP/slices/history показывают один итог |

### CAMP — кампании и листы участников (A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| CAMP-01 | GM create/list/read campaign, reload | Собственный campaign виден, исходные поля сохранены |
| CAMP-02 | Valid/unknown/empty join code, case/whitespace | Trim/uppercase и validation по текущему handler; правильная кампания |
| CAMP-03 | Join со своим/чужим/уже joined character | Ownership/membership rules, без дублированной связи |
| CAMP-04 | GM/P1/P2/OUT читать overview/members/notes | Role visibility соблюдена, hidden GM text отсутствует у игроков |
| CAMP-05 | GM открыть member sheet все tabs | Read-only/full permitted view; чужая mutating операция запрещена |
| CAMP-06 | Видимое available XP игрока и изменение листа | Правильная сводка/актуализация, без чужого spending |
| CAMP-07 | Leave/remove member/character и активный hub | Доступ после удаления прекращается по server policy |
| CAMP-08 | GM-only notes/member removal через forged request | Сервер запрещает недопустимые действия игрока/OUT по текущему handler |
| CAMP-09 | Campaign notes visibility/edit/reorder/delete | Persisted visibility и ownership во всех views |
| CAMP-10 | Concurrent join/remove member/notes edit, reload | Целостная membership, отсутствие duplicate joins |
| CAMP-11 | GM/P1/P2/OUT читать лист и audit другого участника | Текущая read-only member policy соблюдена; OUT и чужая кампания отклонены |

### CHRON — хроника, revisions и mentions (A/F/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| CHRON-01 | Create/edit chapters, reload | Правильный текст/порядок и доступ участникам по текущему контракту |
| CHRON-02 | GM/player/OUT create/edit/delete chapter | Совместная хроника доступна членам; OUT и foreign campaign отклонены |
| CHRON-03 | Revisions/read/revert, concurrent edits | История корректна, откат не теряет последующие данные молча |
| CHRON-04 | NPC/character mentions, rename и удалённый объект | Stable links, fallback text, доступ проверен |
| CHRON-05 | Mention чужого/hidden NPC, forged ID | Нельзя получить закрытый NPC через ссылку |
| CHRON-06 | Image upload, invalid bytes, storage failure | Validated prefix/URL; ошибка не уничтожает chapter |
| CHRON-07 | Большой Markdown/table/list/image в RU/EN/mobile | Sanitized rendering, удобное чтение, без horizontal break |
| CHRON-08 | Draft и навигация при network failure | Несохранённость понятна; retries не создают случайные revisions |

### NPC — бестиарий и пользовательские NPC (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| NPC-01 | Built-in active/retired catalog, system/filter/search | 77 active RoT по текущей fixture; retired сохранены, но скрыты |
| NPC-02 | Minion/Rival/Nemesis create/update | Kind/strain/group skills соответствуют validator |
| NPC-03 | Defense выше 4, invalid характеристики/threshold | NPC policy соблюдена; PC cap не навязан |
| NPC-04 | Skills/abilities/attacks/qualities/equipment arrays | Корректная canonical quality linkage, names и ratings |
| NPC-05 | Built-in edit/delete против duplicate | Built-in read-only; собственная копия независима |
| NPC-06 | Private/campaign-visible/public-template, все роли | Current cross-campaign visibility policy GM проверена |
| NPC-07 | GM2/OUT подставляет NPC ID в view/update/encounter | Нет обхода visibility через соседний endpoint |
| NPC-08 | Quick draft roles/levels, save generated NPC | Валидный статблок, reproducible правила генератора |
| NPC-09 | Empty/long/duplicate/custom quality text | Validation, безопасный вывод; неизвестный quality не теряет текст |
| NPC-10 | Большая library, print/duplicate/reload | Состав/цифры сохранены; latency и layout приемлемы |

### ENC — столкновения (A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| ENC-01 | GM create/edit/list/filter encounter | Fields/system/type/threat сохранены |
| ENC-02 | GM description/player description, visibility | Hidden GM fields не отдаются игроку |
| ENC-03 | Add NPC/minionGroup/PC, quantity и duplicate | Правильные kinds/ownership/quantity, без silent дублей |
| ENC-04 | Foreign NPC/character/campaign в participant request | Сервер проверяет видимость/членство, отказ атомарен |
| ENC-05 | Send-to-table replace/append, empty/large encounter | Именно выбранная семантика; существующая сцена согласована |
| ENC-06 | Update/remove participant и ссылка на deleted NPC | Предсказуемый fallback/validation, без потерь других участников |
| ENC-07 | Player/OUT пытается CRUD/send/delete | GM-only операции недоступны |
| ENC-08 | Concurrent send/update, reload и hub notifications | Целостный table snapshot, допустимый порядок событий |

### TABLE — игровой стол и range (D/A/F/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| TABLE-01 | Нет session, create/reset/end, reload | Пустое состояние/204 обрабатывается; lifecycle сохранён |
| TABLE-02 | 5/20/50 PC/NPC/minion participants, show statblock | Правильные stats, attack profiles и роли |
| TABLE-03 | Add/edit/remove participant, wounds/strain/status | Server state и derived/group counts согласованы |
| TABLE-04 | Minion quantity/casualties на границах ран | Групповой pool и состав обновляются по текущим rules |
| TABLE-05 | Initiative add/reorder/drag/next turn/round | Порядок и active slot сохранены без duplicate turns |
| TABLE-06 | PC/NPC side slots, tie scores/empty list | Deterministic current policy, корректный пустой стол |
| TABLE-07 | Range drag engaged/short/medium/long/extreme, angle/cells | Geometry и labels совпадают с range utils и persistence |
| TABLE-08 | Hide/show range panel, resize/mobile/touch | Tokens/angle/cursor не прыгают и не теряют persisted position |
| TABLE-09 | GM/player boosts/setbacks, character/equipment modifiers | Разрешённые пользовательские изменения, правильные dice pools |
| TABLE-10 | Hidden participants/GM notes, player и OUT | Никакой скрытой информации в REST/hub payload |
| TABLE-11 | UI preferences на campaign/user/tab переключениях | Настройки нужного контекста, не чужой campaign |
| TABLE-12 | Concurrent drag/next-turn/remove при delay | Нет inconsistent snapshots/lost participants; conflict policy явная |

### RT — SignalR и несколько клиентов (A/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| RT-01 | SubscribeCampaign как GM/member/OUT, invalid JWT | Доступ только членам; чужой group недоступен |
| RT-02 | GM changes table; P1/P2 открыты на нём | Изменение приходит без reload, итог соответствует REST |
| RT-03 | Campaign/roll changes, две кампании | Только правильный channel; нет cross-campaign events |
| RT-04 | WebSocket и fallback transport за nginx/TLS | Успешная negotiation, корректные forwarded headers |
| RT-05 | Unmount/reopen/change campaign | Старый connection остановлен, нет двойных subscriptions |
| RT-06 | Offline client, GM меняет стол, затем reconnect | Обязательный свежий snapshot, пропущенные события не оставляют stale state |
| RT-07 | Reconnect с expired access, revoked membership | Refresh/recovery либо ясный отказ; никакого сохранённого доступа |
| RT-08 | Initial connect failure, server restart, длинный outage | Понятный status/retry policy, восстановление без скрытой рассинхронизации |
| RT-09 | 20 быстрых events и медленный REST response | Нет бесконтрольного GET storm и overwriting свежего старым |
| RT-10 | Remove member при активном socket | Последующие закрытые данные недоступны |
| RT-11 | Last event перед reload/reset/end session | REST истина корректна; нет восстановления законченной сцены |
| RT-12 | Проверить logs и telemetry connection errors | JWT/query token не появляется в доступных логах |

### CUSTOM — пользовательские справочники (A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| CUSTOM-01 | CRUD всех 6 типов через campaign UI/API | Корректный system/context, fields и видимость |
| CUSTOM-02 | U1/U2 одинаковые names/codes | Нет подмены чужой записью; ownership важнее строки имени |
| CUSTOM-03 | Campaign GM/member/OUT create/read/edit/delete | Current ownership policy enforced на каждом endpoint |
| CUSTOM-04 | Custom skill/talent/item/heroic в покупке/создании | Правила и visibility применены, не обходятся custom ID |
| CUSTOM-05 | Custom archetype/career starting grants/gear | Validated структура, нет незаконного XP/skill bonus |
| CUSTOM-06 | Delete используемого def, invalid references | Policy целостности и понятный conflict без orphan state |
| CUSTOM-07 | Reseed PublicSafe, restart и language switch | Custom texts/numbers не перезаписаны builtin projection |
| CUSTOM-08 | Markdown/long/invalid numeric fields и cache refresh | Безопасный вывод; новая запись появляется после успешной мутации |

### PACK — content packs и homebrew (A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| PACK-01 | Campaign content-pack create/edit/delete/entries | Контент и порядок принадлежат правильной кампании |
| PACK-02 | GM/member/OUT visibility и role writes | Политика каждого route enforced, без чужих entries |
| PACK-03 | Homebrew import валидного собственного fixture | Структура импортирована; owner и codes присвоены корректно |
| PACK-04 | Invalid schema/version/duplicate/oversize pack | Validation до частичной записи; понятные warnings/errors |
| PACK-05 | Default enable/disable, character/campaign enable | Состав reference меняется только в нужном контексте |
| PACK-06 | Включить pack U1, прочитать U2/другой character | Никакого ownership/cache bleed |
| PACK-07 | Export→import, names конфликтуют с builtin | Lookup semantics стабильны; не подмена встроенного правила |
| PACK-08 | Share/import, повторная выдача token, unknown/старый token | Новый share работает; прежний hash заменён; unknown/старый token отклонён |
| PACK-09 | Disable default/character/campaign использованного pack | Existing character references не повреждены по принятой policy |
| PACK-10 | Reseed/reload/Concurrent switches | Правильные invalidation и eventual displayed catalog |

### IMP — экспорт, импорт и перенос между версиями (D/A/F/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| IMP-01 | Export Core/RoT, inspect v8 | Текущий format; stable codes/names, без внутренних owner IDs |
| IMP-02 | Каждый поддержанный v1…v8 импортировать | Expected legacy normalization и warnings; не молчаливая потеря |
| IMP-03 | Preview valid/invalid import, сравнить DB до/после | Preview ничего не пишет и не сбрасывает корректный cache |
| IMP-04 | Malformed JSON/unknown version/missing fields/overflow | Управляемая ошибка; нет частичного персонажа |
| IMP-05 | Stable code существует, name изменён/дублирован | Code имеет правильный приоритет, built-in resolution стабилен |
| IMP-06 | Unknown/retired code и foreign custom code | Предсказуемые warnings/errors; чужой def не доступен |
| IMP-07 | Все поля: health/bio/notes/talents/items/damage/material | Canonical round trip, исключения явно задокументированы |
| IMP-08 | Heroic identity/parameters/signature upgrades/shards | Persistent configuration и progression восстановлены |
| IMP-09 | Mounts/cargo/traction/legacy versions | Links перенесены, не двойная нагрузка/duplicated transport |
| IMP-10 | Private→Public→Private с built-in-only fixture | Одинаковая механика, no full private prose в public результатах |
| IMP-11 | Custom/pack fixture через cross-stack перенос | Unresolved dependencies явные; отсутствие shared DB не замалчивается |
| IMP-12 | Большой import/concurrent submit и query count | Batch lookup, нет N+1 по строкам; атомарность результата |

### PRINT — публичные ссылки и печать (A/F/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| PRINT-01 | Create share, U0 открыть; owner data и read-only UI | Доступен только предусмотренный shared sheet; mutation не разрешена |
| PRINT-02 | Revoke/unknown share token, reload и cached tab | Отозванный доступ не восстанавливается через cache |
| PRINT-03 | Full sheet с heroic upgrades/transport/talents RU/EN | Safe тексты и параметры присутствуют; нет private marker |
| PRINT-04 | A4/Letter/PDF preview, маленький/большой sheet | Нет обрезанных sections/кнопок или пустых критических полей |
| PRINT-05 | NPC/magic/item/talent cards, long text и symbols | Корректные поля, pagination, readable symbols |
| PRINT-06 | Print из partial slices и после mutations | Догружается нужный полный лист; не печатается stale state |
| PRINT-07 | Share token в search/robots/referrer/logs | Непредусмотренная индексация/раскрытие credentials исключены |
| PRINT-08 | Shared sheet включает custom/GM content | Только контрактно разрешённое содержимое; скрытые notes/чужие refs не добавлены |

### CACHE — корректность оптимизаций API client (F/A/E)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| CACHE-01 | Параллельный одинаковый reference/sheet/list GET | Single-flight Promise; один сетевой запрос |
| CACHE-02 | Logout A→login B, разные custom catalogs | Memory caches очищены; B не видит A |
| CACHE-03 | A request in-flight→logout→B→A response | Старый ответ не попадает в новую сессию/экран |
| CACHE-04 | Смена system/character/campaign/pack context | Cache key и invalidation учитывают разрешённый контекст |
| CACHE-05 | Failed cached request→retry | Rejected promise удалён, данные можно перечитать |
| CACHE-06 | Mutation возвращает свежие slices, повторное take | Один consume; соответствующие данные актуальны |
| CACHE-07 | Mutation без slices/неуспешная/created new character | Правильный fallback/invalidation; не потерян cache чужого объекта |
| CACHE-08 | base/items/talents/mounts/attachments: null против [] | Null означает не загружено, [] корректно означает пусто |
| CACHE-09 | Navigation sheet→shop→print→sheet и tab return | Нужные части переиспользуются, неявного full reload нет |
| CACHE-10 | Out-of-order mutations и optimistic rollback | Последнее серверное состояние побеждает; чужой optimistic state не откатывается |

### PWA — persistent cache, offline и update (E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| PWA-01 | Install на supported Chrome/Edge/Android/iOS | Manifest/icons/start URL корректны; ожидаемый install flow |
| PWA-02 | Manifest/sw MIME и scope, reload после update | Правильная доставка; не ошибочное HTML тело |
| PWA-03 | A прогрел персональный reference→logout→B→offline | B не получает custom/закрытые данные A |
| PWA-04 | A прогрел→revoke access→timeout/401/403 | Cache не восстанавливает закрытые данные после revoke |
| PWA-05 | Сменить campaign/pack permission, старый SW cache | Видимость не зависит от семидневной stale записи |
| PWA-06 | Проверить разные host origins и multi-account profiles | Public/private origin stores не смешиваются |
| PWA-07 | Offline open ранее посещённых страниц/справочников | Ясное offline поведение; нет фиктивно сохранённых мутаций |
| PWA-08 | SW autoUpdate при открытой форме/таблице | Данные формы/сессии не теряются молча |
| PWA-09 | Old HTML→new chunks и new HTML→old cache | Controlled recovery, cleanup old caches без бесконечного reload |
| PWA-10 | Private mode/storage unavailable/cleared/quota | Приложение graceful fallback; cache errors не блокируют вход |

### PERF — скорость, ресурсы и нагрузка (A/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| PERF-01 | Одинаковый commit/fixtures/network, cold/warm login | Задокументирован baseline, preload и cache состояния |
| PERF-02 | LCP/INP/CLS в desktop/mobile, RU/EN | Принятые budgets ниже; нет ухудшения из-за safe projection fix |
| PERF-03 | First/return character list, normal/BIG-SHEET user | Узкий list query, нет чтения inventory каждой карточки |
| PERF-04 | Cold reference, без/с packs, большие каталоги | Измерены app/db/query/bytes/parse/render, нет N+1 на записи |
| PERF-05 | Покупка/equip/health update с `X-Return-Slices` | Один mutation request, без обязательного повторного reference/full sheet |
| PERF-06 | Первая/повторная вкладка sheet, навигация shop/print | Догружается только missing subset, warm return без лишнего GET |
| PERF-07 | Full print/export/shared sheet, normal/large graph | Query count bounded; нет Include cartesian explosion |
| PERF-08 | 0/100 crafting projects и history/notes | Ownership не тащит полный graph без необходимости |
| PERF-09 | Import 10/100/1000 rows допустимого fixture | Lookup query count не растёт по строкам, измерено CPU/memory |
| PERF-10 | TABLE-S/M/L, drag/next-turn и 20 событий/сек на staging | Click-to-render/event latency в budget, REST refresh не storm |
| PERF-11 | Concurrent 10/25/50/100 virtual users, mix workloads | Error/latency/resource thresholds; bottleneck локализован |
| PERF-12 | Burst 5×normal и 2–8h soak на staging | Нет leak/OOM/unbounded queues; controlled recovery |
| PERF-13 | Slow network/high RTT, CPU throttle, images/lazy chunks | Работа основных действий приемлема, loader/error понятны |
| PERF-14 | Сравнить Public/Private с matched data и без общего contention | Отчёт separates network/server/db/render; p95 основан на достаточном n |

### SEC — целевые проверки изоляции и входных данных (A/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| SEC-01 | Ownership отрицательные проверки всех inventory routes | Нет IDOR по любому parent/child/campaign id |
| SEC-02 | Hidden fields inspect REST/hub/shared/search/export | Обрезка на сервере, не только CSS/UI |
| SEC-03 | Safe XSS probes в custom/bio/notes/chronicle/pack | Scripts/event handlers не исполняются; ссылки безопасны |
| SEC-04 | Invalid SQL-like text в search/filters/names | Не меняет query semantics, не вызывает leak/500 |
| SEC-05 | Cross-origin cookie requests/headers, CORS preflight | Auth action protection соответствует cookie/token контракту |
| SEC-06 | Rate limits per user/IP за proxy; shared-IP users | Рабочие limits без случайной блокировки всех proxy клиентов |
| SEC-07 | Large JSON/collections/files и invalid enum/overflow | Controlled error/resource limits, без partial persistence |
| SEC-08 | Проверить bundle/source maps/public artifact secrets | Нет passwords/JWT/private resources; public OAuth client id допустим |
| SEC-09 | Transport/logging/header policy production | TLS валиден; токены/тела/пароли не логируются; политика headers проверена |
| SEC-10 | Revoke share/session/membership→cached/reconnected read | После отзыва запрещённые данные не возвращаются |

### DEPLOY — seed, миграции, эксплуатация (A/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| DEPLOY-01 | Health DB available/unavailable, оба API prefixes | 200 ok либо 503 degraded, без credentials |
| DEPLOY-02 | Fresh public PostgreSQL17 startup/migrate/seed | Полная схема/public data, ни private resource ни full marker |
| DEPLOY-03 | Upgrade с предыдущего release на DB с users/characters/graphs | Данные/links/IDs/custom content сохранены, backfills корректны |
| DEPLOY-04 | Повторный migrate/seed и рестарт | Идемпотентность, никаких duplicate codes/потерь пользовательских полей |
| DEPLOY-05 | Два mode стенда и перезапуск каждого отдельно | DB/volumes/JWT/storage/env не перепутаны |
| DEPLOY-06 | Invalid config/migration/seed fail на staging | Fail явно; unhealthy release не выдаётся за успешный |
| DEPLOY-07 | Image digest/commit, frontend asset/seed version | Однозначная идентификация релиза, не только latest |
| DEPLOY-08 | Deploy while active tabs/chunks/hub open | Новый вход и старые сессии восстанавливаются без потери данных |
| DEPLOY-09 | Reverse proxy API/hub/schema/static/deep link routing | OpenAPI JSON, рабочий Scalar/WS, SPA только на UI путях |
| DEPLOY-10 | Backup private/public; проверить offsite upload/retention | Обе копии завершены и читаемы, credentials не опубликованы |
| DEPLOY-11 | Restore в disposable clone, compare counts/canonical data | Восстановлены users/graphs/uploads links; измерены RPO/RTO |
| DEPLOY-12 | Rollback compatible release на disposable DB | App совместим с текущей схемой; нет обещания автоматического DB rollback |

### OBS — доказательства и наблюдаемость (A/E/M)

| ID | Шаги / данные | Ожидаемый результат |
|---|---|---|
| OBS-01 | API action success/failure и timing event | Method/path/status/duration/server timing доступны, без payload secrets |
| OBS-02 | Request trace через proxy/API/DB | Можно отделить network/app/db и число запросов |
| OBS-03 | Expected 4xx против unexpected 5xx | Метрики разделены; failed validation не считается outage |
| OBS-04 | Логи refresh/password reset/OAuth/upload/errors | Tokens/password/reset links не попадают в публичные artifacts |
| OBS-05 | Зависший email/storage/hub/backup на staging | Диагностируется правильный subsystem, не общий "успех" |
| OBS-06 | Test report/traces/screenshots/HAR перед публикацией | Секреты и личные данные удалены; verdict воспроизводим |

## 4. Каноническое сравнение и exhaustiveness

Паритет встроенных записей проверять **по stable Code + System**, а не по UUID или NameRu.
Вложенные ссылки также нормализовать до стабильного кода; нельзя просто удалить все GUID и
случайно скрыть неправильную связь. Для сущностей без кода использовать явно описанный
natural key и фиксировать недостаток идентификации, не угадывать похожие названия.

Из сравнения исключаются только: конкретные UUID владельцев/instances, timestamps, private
`Description` и предусмотренные тексты безопасной проекции. Включаются: active/retired,
system/setting, starting characteristics/XP, skill grants, tier/ranked/cost/prerequisites,
activation/trigger/limits, item stats/qualities/profiles/material constraints, heroic costs и
параметры, spell difficulty/skills/exclusions/ratings, mounts/capacity/traction, crafting budgets.
Отдельно сравнивается graph результата действий и корректность его links.

Полные каталожные проходы не заменять несколькими примерами:

- Все 11 heroics/22 Power upgrades; все secondary effects.
- Все 39 crafting spends (21 Item/18 Potion), включая 24 descriptive effects и все 12 potions.
- Все 5 transport profiles; все 22 attachment catalog entries по доступным системам.
- Все 80 quality entries и все spell/effect/talent/archetype/career codes.
- Bestiary: 86 seed rows, из них 77 active RoT и 9 retired по текущей fixture; counts дополнять
  exact Code set. Каталог talents содержит 123 source entries, items — 138, archetypes — 18:
  это **не** число видимых записей каждой системы после expansion/retired filters.
- Все 167 HTTP declarations + health и SignalR; оба API prefixes, применимые роли и errors.

Считать покрытие полей/поверхностей, а не только число тестов. Public text contract включает
reference, full sheet, slices, nested upgrades, shop, crafting, search, shared sheet, print,
homebrew exports и persistent caches. Текст проверять на достаточность механики и отсутствие
synthetic private marker; не сравнивать его с оригинальным текстом книги.

## 5. Матрица браузеров, устройств, сети и ролей

| Измерение | Обязательный базовый прогон | Расширенный прогон |
|---|---|---|
| Browser | Chromium desktop + mobile emulation | Firefox, WebKit; реальные Safari iOS/Chrome Android, Edge |
| Размер | 360×800, 390×844, 768×1024, 1440×900 | 320 px, landscape, 200% zoom, крупный системный шрифт |
| Язык | RU, EN и переключение | Пустые переводы/необычная browser locale |
| Cache | Fresh profile; memory warm; SW warm | Stale release, storage blocked/quota, multi-account |
| Auth | Valid, expired, revoked, absent | Concurrent tabs, Google, delayed refresh |
| Network | Normal; high RTT/slow mobile; offline | DNS/API/storage failures, 429/502/503, interrupted body |
| Role | U0, owner, other user, GM, player, OUT | Removed member, revoked share, second unrelated campaign |
| Data | Empty, ordinary, large valid | Legacy migrated, duplicate names, retired links, invalid graph |
| Interaction | Mouse + keyboard | Touch drag, screen reader, reduced motion |

Использовать pairwise для второстепенных сочетаний; полный набор ownership/режимов обязателен
для всех routes. Critical flows и PUB-01…05 проверить на обоих языках и mobile/desktop.
Emulation не заменяет реальные браузеры для cookies, OAuth, PWA, touch и printing.

## 6. Производительность: методика и предлагаемые бюджеты

Все пороги ниже — **Assumption, предлагаемые критерии**, до согласования и baseline они
не являются действующим SLA и не доказаны live-аудитом. Условия: одинаковые валидные fixtures,
одинаковый commit, идентичные container limits, отдельная фаза без взаимного contention.
В каждом отчёте фиксировать клиентский регион/RTT, hardware/browser, CPU throttle, bandwidth,
cache state, число объектов, concurrency и image digest.

| Метрика | Предлагаемый критерий | Как измерять |
|---|---|---|
| LCP / INP / CLS | ≤2.5 s / ≤200 ms / ≤0.1 | Real browser; lab для диагностики, field percentile отдельно |
| Warm click→видимый подтверждённый итог | p95 ≤800 ms normal RTT | API timing + DOM mark; refresh/retry включить |
| Cold list/sheet | p95 ≤2 s normal desktop | Раздельно app/db/network/parse/render |
| Hot mutation app time | p95 ≤300 ms; 5xx=0 на normal load | Server-Timing/traces, не подмена внешним TTFB |
| Reference повторно | 0 лишних GET на неизменённый тот же контекст | Browser request counts |
| Mutation с slices | 1 mutation; 0 обязательных follow-up sheet/reference GET | Request log, исключения fallback отдельно |
| Realtime convergence | p95 ≤1 s после server commit | Два клиента, matched version/snapshot |
| Public против Private | ≤10% регрессии matched сценария | Достаточная выборка, variance/confidence; единичный health не benchmark |
| Query complexity | bounded, без N+1 на строки | Количество SQL, plan/duration на PostgreSQL |
| Response bytes/JS/CSS | Не больше принятого baseline +10% без причины | Transfer size отдельно от decoded size |
| Resource budget | Нет OOM/restarts; устойчивый plateau | RSS/CPU/DB connections/GC/threadpool/locks/disk |

Сбор: минимум 30 повторов для первичного baseline и не менее 200 завершённых операций на
сценарий для отчёта p95/p99; p99 на малом n не принимать за надёжный показатель. Для Core Web
Vitals не подменять пользовательский p75 одной Lighthouse оценкой.

Предлагаемый нагрузочный профиль (**Assumption**): 45% read sheet/slices, 15% reference/search,
15% inventory/health/XP mutations, 15% campaign/table reads+events, 5% auth refresh, 5% export/import.
Роли/owners раздельные; не расходовать один и тот же XP у всех virtual users. Ramp 10→25→50→100,
по 10 min этап; burst 5×; soak 2 h, затем 8 h при нормальном плато. Stop conditions: реальные
данные затронуты, OOM/рост ошибок/DB connection exhaustion или невозможность контролировать тест.
Сохранить p50/p95/p99/error rate, CPU/RSS/GC, SQL count/locks, event→render и transferred bytes.

Сначала измерить имеющиеся lazy/cache/slices/narrow-query оптимизации. Добавлять server cache,
pagination, virtualization или event coalescing только после локализации bottleneck и тестов
visibility/staleness. PublicSafe не должен становиться быстрее ценой чужого кеша или stale state.

## 7. Текущие тесты и нужное расширение

Статическая инвентаризация на выбранном commit: 37 `.cs` файлов Domain test project,
70 `.cs` файлов API test project, 52 frontend `*.test.ts(x)` файла; приблизительно 410/642/428
test declarations. Это **не** количество реально выполненных test cases: Theory/data-driven
тесты дают дополнительные варианты, regex не является test runner.

Есть E2E smoke: 4 основных сценария + 1 i18n; только Chromium. В smoke многие операции
выполняются напрямую через API, затем проверяется экран. Это не полное покрытие кнопок/forms.
CI запускает restore/build/test frontend/backend, public publish, migrations и default-stack E2E.

| Область | Существующая опора | Требуемое расширение |
|---|---|---|
| Public projection | `ContentSeedTests`, `SpellTests` | Полный nested DTO/locale/surface parity, public-image E2E |
| XP/talents/derived | Domain validators, API purchase/refund tests | PostgreSQL concurrency и UI цепочки PublicSafe |
| Heroics | `RotHeroic*`, components | Safe upgrades RU/EN и print, все 22 улучшения |
| Crafting/transport | `RotCraftingApiTests`, `RotMountApiTests`, RTL | Safe text contract, real public DTO, locale и cross-surface |
| Cache/slices | `client.test.ts`, slices/query-shape tests | Session races, SW, stale network/out-of-order responses |
| Campaign/table | `GameTableTests`, hub/API/RTL | Multi-browser reconnect catch-up, role revocation, burst events |
| Import/export | `CharacterImportExportTests`, smoke | Cross-mode, all legacy versions, large graph и payload isolation |
| Migration/seed | CI populated baseline DB | PostgreSQL17, real user graphs и PublicSafe mode |
| Browser UX | Page/component tests | Firefox/WebKit/mobile/visual/a11y/print/PWA/OAuth |
| Performance | timing instrumentation, query-shape tests | Измеряемый baseline и согласованные budgets бизнес-сценариев |

Первый набор регрессий: PAR-02/03/04, HERO-09, CRAFT-07/08, TRAN-01/02, PRINT-03,
PWA-03/04, CACHE-03, RT-06, API-12. Fixtures обязательно повторяют реальные public DTO,
а не заполняют `description` полным текстом в mock и скрывают проблему fallback.

## 8. Порядок прогонов и release gates

1. **Идентификация:** commit/digests/Content mode/DB version; свежий и legacy fixture набор.
2. **P0 smoke:** health→вход→список→Core/RoT create→sheet→допустимая мутация→reload→logout.
   Параллельно ownership, cross-stack JWT и отсутствие private marker.
3. **Public regressions:** heroics/crafting/transport safe текста, обе локали и print.
4. **Механика:** D/A сценарии XP/pyramid/stats/equipment/heroics/magic/economy/crafting.
5. **Collaboration:** campaign→join→NPC→encounter→table→два clients→offline/reconnect→revocation.
6. **Перенос/контент:** custom/packs/import/export/share/print; SW/memory races.
7. **Нефункциональные:** browser/device/visual/a11y/PWA, performance/load и операции staging.
8. **Production post-deploy:** release identity, public/private health, реальные public reference
   projection и выделенный smoke. Heavy tests остаются на staging.

Критерии выпуска:

- Нет открытых P0/P1 дефектов; исключение требует конкретного принятого решения и остаточного риска.
- PUB-01…03 regression scenarios Passed; safe text непустой и пригодный, не private full.
- Ownership/membership/share/session revoke и user-cache isolation Passed.
- Каждая строка API inventory имеет positive и применимые negative/ownership результаты;
  версии routes проверены. Сценарии, неприменимые к read-only route, отмечены N/A с причиной.
- Public image прошёл CI/integration/E2E с PostgreSQL17/proxy, а не только PrivateFull default.
- Миграция/seed не теряют пользовательские данные, backup restore проверен в clone.
- Принятые performance budgets выполнены; regression >10% имеет измеренное объяснение/решение.
- RU/EN, mobile/desktop, full print и минимум Chromium/Firefox/WebKit smoke без blocker.
- Не закрывать out-of-scope runtime как дефект и не помечать неисполненный кейс Passed.

## 9. Шаблоны протокола и дефекта

```text
Run ID / дата и время (Europe/Moscow):
Public commit / image digest / frontend hash / seed identity:
Reference commit / image digest / Content mode:
DB version / migration baseline / fixture version:
Browser/device/viewport/lang/network/cache state:
Case ID / параметры / роли / normalized object keys:
Expected (ссылка на rule/contract):
Actual / API status / reasonCode / persisted canonical result:
Private comparison (Passed/Failed/Not run):
Verdict: Passed | Failed | Blocked | Not run | N/A (причина)
Trace/screenshot/log/timing artifact без секретов:
Defect ID / severity / reproducibility / remaining work:
```

```text
Defect ID, название и P0/P1/P2/P3:
Public-only / общий / ожидаемое ограничение / Verify:
Версии и fixtures:
Минимальные шаги:
Expected и source:
Actual, затронутые UI/API/print/cache:
Persistence/data impact:
Доказательство private parity либо Not run:
Предлагаемая доработка и regression Case IDs:
```

## 10. Рабочий чеклист тестовой кампании

- [ ] Подготовлены парные стенды одного commit и безопасные fixtures.
- [ ] Есть release/seed identity и журнал доступов/условий замеров.
- [ ] P0 smoke и all-route ownership прогнаны.
- [ ] PublicSafe text regression и канонический parity проверены полностью.
- [ ] Числовые механики, legacy и concurrency прогнаны.
- [ ] GM/player/multi-client и reconnect/revoke проверены.
- [ ] Custom/packs/import/share/print/cache покрыты.
- [ ] Browser/device/RU/EN/PWA/a11y/visual прогон выполнен.
- [ ] Baseline/load/soak и согласованные budgets подтверждены.
- [ ] PostgreSQL17 migrations/reseed/backup restore/rollback проверены на clone.
- [ ] Отчёты очищены от secrets/личных данных; все Failed/Blocked связаны с дефектами.
- [ ] Release gates приняты; непроверенные сценарии явно оставлены Not run.

Код/seed/migrations/dependencies/deploy в рамках составления этого плана не меняются.
Новые проверки и доработки выполняются отдельными задачами/PR по процессу репозитория.

Не заявлены существующими функциями: редактирование/удаление самой кампании, rotation/expiry
join code, private/published chapters и ручной reorder хроники, удаление/revoke homebrew pack,
pagination audit сверх `take`. Их поддержка **Not found in current codebase** данного среза;
общая API-матрица проверяет реальные routes, а не требует выдуманных операций от PublicSafe.
