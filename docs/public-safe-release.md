# PublicSafe: подготовка публичной версии

Задача: [GEN-PUB-SAFE](../roadmap/tasks/public-safe-book-references.md). Основа — актуальный
`origin/master` 4da4636 и документы аудита из PR #257. Этот документ описывает изменения
следующей версии; результаты обследования уже работающих сайтов остаются в
[аудите 05.10.2026](public-private-audit-2026-10-05.md).

## Политика контента

Для встроенных талантов, героических способностей, Power upgrades и Secondary Effects
публичный режим отдаёт название и библиографическую ссылку. `Description`, `SafeDescription`,
`DescriptionEn`, заметки и текстовый trigger очищаются. У героик также очищаются текстовые
requirement/activation/duration/frequency; структурные эффекты, стоимость покупок, типы
выборов, prerequisite/exclusion, числовые бонусы и серверные расчёты сохраняются.
Инструкции к форме и результаты автоматизации остаются частью интерфейса.

PrivateFull сохраняет прежние тексты и параметры. Существующие тексты перемещены из
общих JSON-каталогов и класса Secondary Effects в `backend/private-content/rule-text.ru.json`.
Ресурс включается только при `IncludePrivateContent != false`; public Docker target его
не копирует и задаёт `Content__Mode=PublicSafe`. Новые тексты из книг не добавлялись.
PDF и извлечённые страницы не входят в git. Изменение не переписывает историю репозитория.

Повторный seed обновляет встроенные записи по стабильным кодам, сохраняет ID, покупки
и пользовательский контент. Даже устаревшие встроенные записи, отсутствующие в каталоге,
очищаются в PublicSafe: они могут оставаться на купленном листе. Базы публичного и
приватного сайтов по-прежнему должны быть отдельными. Миграции не требуются.

## Книги и страницы

Подтверждены 120 из 123 исходных талантов, все 11 героик, 22 Power upgrades и 8 Secondary
Effects. Улучшения Power используют страницы родительской способности; общие улучшения
и Secondary Effects ссылаются на Terrinoth, с. 79. Номера печатные, не индексы PDF.

Для Core ссылки помечены `RU translation`: русский перевод переставляет таланты,
поэтому его страницы нельзя выдавать за номера английского издания. Страницы Terrinoth
сверены по имеющемуся переводу с сохранённой пагинацией. Ссылка ведёт на официальную
страницу [Genesys Core Rulebook](https://www.edge-studio.net/games/genesys-core-rulebook/)
или [Realms of Terrinoth](https://www.edge-studio.net/games/realms-of-terrinoth/).
Публичное приложение не размещает сканы книг; указанную страницу читатель открывает
в собственной копии. Ссылки видны в каталоге, пирамиде, купленных талантах, героиках,
печатных карточках и полном печатном листе. DTO купленного таланта и Power upgrade
получили дополнительное необязательное поле `source`; старые поля сохранены.

| Legacy talent | Подтверждённый источник | Текущее поведение |
| --- | --- | --- |
| Attuned | Not found in current codebase; не найден в двух доступных книгах | Название, «страница не подтверждена»; без описаний |
| Counterspell | Not found in current codebase; манёвр Core с таким именем не подтверждает этот талант | Название, «страница не подтверждена»; без описаний |
| Empowered Casting | Not found in current codebase; не найден в двух доступных книгах | Название, «страница не подтверждена»; без описаний |

`Assumption`: до ответа владельца сохраняем эти три записи в Genesys Core и явно отмечаем
неподтверждённую страницу. Они уже retired в Terrinoth. Ни страницы, ни принадлежность
к другой книге не выдумываются. Вопрос о сохранении/исключении из публичных покупок задан.

## Закрытие пунктов аудита

| Пункт | Реализация / проверка | Остаток приёмки |
| --- | --- | --- |
| PUB-01 | Героические описания заменены ссылками по решению владельца, включая улучшения и печать | Нет потерянных подсказок: подсказки больше не входят в публичную политику |
| PUB-02 | Crafting DTO использует SafeDescription при пустом Description; UI выбирает RU/EN | Полная ручная матрица всех исходов крафта |
| PUB-03 | Mount DTO восстанавливает безопасное описание; числа и управление транспортом сохранены | Полная ручная матрица транспорта |
| PUB-04 | SW NetworkOnly для API; удаление legacy caches при activate и перед API-запросами; HTTP no-store | Safari/iOS и обновление старых установленных PWA на production |
| PUB-05 | Тесты public API, reseed, structural parity и сборки без private resources; CI E2E обоих Docker targets | [CI commit 008d370 прошёл](https://github.com/Giviruk/GenesysForgeClaude/actions/runs/37377173867); финальные docs/tooling обновления проверяются отдельно |
| PUB-06 | Единая политика названий/ссылок RU/EN; 120 подтверждённых талантов | Решение по трём legacy-записям / подтверждённые страницы |
| PUB-07 | Health показывает contentMode/version; Docker получает commit от deploy workflow | После развёртывания сверить оба health с release SHA |
| PUB-08 | Добавлен read-only scripts/measure-api.mjs; сохранён локальный baseline двух Release-сборок; план сравнительной нагрузки | Профиль PostgreSQL/320 MB и репрезентативные production данные не измерялись локально |
| PUB-09 | Crafting list проверяет ownership через EXISTS без загрузки всего персонажа | SQL/latency под PostgreSQL 17 и production объёмом |
| PUB-10 | После SubscribeCampaign перечитываются campaign/table/roll snapshots; failure не выдаётся за connected | Длительный настоящий сетевой разрыв и ручная матрица участников |
| PUB-11 | nginx/Vite проксируют /openapi/ на backend; E2E проверяет JSON schema | nginx Docker path проверяет CI |
| PUB-12 | CI migrations PostgreSQL 17, матрица PrivateFull/PublicSafe, непустой каталог | Расширенная миграционная матрица пользовательских графов из тест-плана |
| PUB-13 | Исправлены docs/ai-context.md, testing.md и database.md | Продолжать синхронизацию документов при новых изменениях |
| PUB-14 | manifest MIME application/manifest+json, SW scripts no-cache; E2E manifest/PWA | Ручные мобильные/доступность/визуальные сценарии |

## Фактическая проверка

- Backend: полный suite 744 domain + 880 API tests; финальная повторная проверка прошла.
- Public artifact: 4 targeted tests при `IncludePrivateContent=false` и `VERIFY_PUBLIC_ARTIFACT=1`.
- Frontend: после разрешённого `npm ci` прошли lint, build и все 456 tests на актуальных lock-версиях.
- Chromium: все 8 сценариев прошли после восстановления lock-зависимостей: PublicSafe
  book-only API/купленный лист/ссылки, PWA legacy-cache/offline, OpenAPI/manifest/release identity
  и пять основных smoke-сценариев. Локально Playwright 1.63.0 запущен с уже имеющимся Chromium
  1228 через `E2E_CHROMIUM_EXECUTABLE`: macOS 13 не поддерживается браузером текущего Playwright.
  CI Linux использует штатный браузер lock-версии без этого override.
- Локальный browser run использовал public publish без private resources, Vite preview и
  InMemory DB. Docker daemon недоступен; локально PostgreSQL/nginx container checks не выполнены.
- До восстановления локальные node_modules отличались от текущего lock-файла (например, Vitest 4.1.8 вместо 5.0.3).
  Владелец разрешил `npm ci`; зависимости восстановлены без изменения lock-файла. Финальные
  проверки выполняются на Vitest 5.0.3 / Vite 8.3.2 / React 19.3.0 / TypeScript 6.0.3 / Playwright 1.63.0.
- CI commit 008d370 полностью прошёл: backend/frontend, PublicSafe/PrivateFull migrations
  на PostgreSQL 17 и Chromium smoke обоих Docker targets с nginx
  ([run](https://github.com/Giviruk/GenesysForgeClaude/actions/runs/37377173867)).

Полный [план тестирования](public-version-test-plan.md) содержит 373 сценария. Этот документ
не объявляет их выполненными: автоматизированный smoke покрывает только часть матрицы.

## Локальный baseline

[Результаты](public-safe-local-baseline.json): один commit 008d370, две Release/net10.0 сборки,
macOS 13, loopback, отдельные InMemory базы, свежие одинаковые каталоги, пустые списки
персонажей. На каждый маршрут — 3 прогрева и 20 последовательных замеров. Все 160
учтённых ответов успешны. Токены и тела ответов не сохраняются в отчёт.

| Reference | PrivateFull, байт | PublicSafe, байт | Изменение размера | Median TTFB private / public, мс | Total p95 private / public, мс |
| --- | ---: | ---: | ---: | ---: | ---: |
| Genesys Core | 250778 | 156637 | −37,5% | 12,33 / 12,84 | 26,82 / 21,21 |
| Realms of Terrinoth | 581251 | 384739 | −33,8% | 17,89 / 19,36 | 32,53 / 39,68 |

Размер — декодированное тело JSON, без учёта сжатия и HTTP-заголовков. Уменьшение
объёма связано с удалением прозы. Эти короткие последовательные замеры не подтверждают
ускорение публичной версии и не заменяют нагрузочный тест PostgreSQL/nginx с лимитами
production. Полный лист и крафт на репрезентативном персонаже остаются в нагрузочной матрице.

Воспроизведение: задать `GENESYS_BENCH_TOKEN` вне отчёта и запустить
`node scripts/measure-api.mjs --base-url http://localhost:8080 --samples 20`.
Опция `--character-id <uuid>` добавляет чтение полного листа и списка крафта. Скрипт
выполняет только GET, не создаёт пользователей и не изменяет данные; не запускать два
профиля одновременно при сравнении ограниченных ресурсов.

## Приёмка релиза

- [ ] Решить судьбу трёх legacy-талантов или добавить подтверждённые библиографические данные.
- [x] Первый CI прошёл: backend/frontend, обе PostgreSQL 17 migration jobs и оба Docker E2E jobs ([run](https://github.com/Giviruk/GenesysForgeClaude/actions/runs/37377173867)).
- [ ] Пройти ручные P0/P1 из полного плана (ownership, share, мобильные страницы, восстановление соединения).
- [ ] Выполнить нагрузочный профиль и сравнение private/public при одинаковых данных и лимитах.
- [ ] После отдельного решения о deploy проверить health SHA/mode, OpenAPI JSON, manifest MIME,
  public reference/лист/печать, PrivateFull тексты и отсутствие private resources в public image.
- [ ] Merge и развёртывание выполняются после проверки PR; в текущей задаче сайты не изменялись.
