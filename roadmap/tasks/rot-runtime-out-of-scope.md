# Вне скопа: правила, которым нужен исполняющий движок столкновения

Сюда вынесены задачи разбора правил, которые нельзя выполнить без движка, ведущего само
столкновение. Разбор книги по ним сохранён целиком — если движок когда-нибудь появится, задачи
возвращаются в [rot-rules-remediation-tasks.md](rot-rules-remediation-tasks.md) переносом текста
обратно. Прогресс по ним не ведётся: в
[rot-rules-remediation-progress.md](rot-rules-remediation-progress.md) их нет.

Ссылки вида «см. ROT-MIN-05», «полное правило ROT-HA-08» или «§6» из оставшихся в скопе задач
ведут сюда: текст задач не переписывался, переехал только он сам.

## Критерий

Задача уходит из скопа, если для неё нужно хоть что-то из пяти:

1. **выбор цели и разрешение попадания как состояние** — кто по кому бьёт и применение хита
   участнику, а не расчёт по присланным числам;
2. **авторитетный серверный бросок** — сейчас кубы бросает клиент, а сервер только пишет результат
   в лог стола (`CreateRoll`);
3. **автоматическое списание ран, усталости и состояний** участникам;
4. **траты символов, привязанные к конкретному броску** — гарантия «одни и те же преимущества
   нельзя потратить дважды»;
5. **жизненный цикл хода и раунда** — длительности, «раз за ход/раунд/столкновение», периодические
   тики, инициатива.

## Что скоп сохраняет

Лист персонажа и вся его арифметика, справочники и каталоги, экономика и инвентарь, конструктор
NPC, стол ведущего с участниками и логом бросков, калькуляторы без состояния — например
`POST /api/combat/resolve-attack`, который считает попадание и урон по присланным успехам, урону и
поглощению, ничего никому не записывая.

## Частично вынесенные задачи

Эти задачи остались в основном файле: их структурная половина в скопе, исполняющая — нет. Здесь
записано, что именно из них не делается.

| Задача | Остаётся в скопе | Вне скопа |
|---|---|---|
| **ROT-SPECIES-01** | каталог 14 вариантов, характеристики, пороги (сделано) | активируемые способности видов |
| **ROT-TAL-06** | тексты и метаданные эффектов | `Shapeshifter` с автотриггером формы, `Counterattack`, `Crushing Blow`, `Can't We Talk About This?` |
| **ROT-HA-05** | 11 primary effects, ранги Power, тексты | Duration lifecycle, периодические тики, счётчики применений |
| **ROT-WPN-01** | профили оружия и alternate attacks (сделано) | серверный расчёт урона при разрешении атаки: `BaseDamage` остаётся клиентским |
| **ROT-EQP-ATT-01…03** | каталог из 21 улучшения, слоты, совместимость (сделано) | Шипы, Скверна Инфернаэля, Гарда дуэлянта, Руна клинков — реакция на чужой бросок и фиксированный крит |
| **GEN-EQP-DMG-01** | состояние экземпляра, последствия, ремонт (сделано) | автоматическая активация Sunder в разрешении атаки |
| **GEN-EQP-QUAL-01** | метаданные всех качеств и исполнение двенадцати (сделано) | эффекты со счётчиками раундов и статусами: Взрыв, Жжение, Ошеломление, Дезориентация, Захват, Нокдаун, Оглушение, Залповое |
| **ROT-MAG-10** | резолвер рейтингов из `Knowledge (Lore)` и `Dark Insight` — это расчёт по листу | применение рейтинга при разрешении атаки |
| **ROT-MAG-11** | каталог 17 runebound shards, экономика (`Price=null`), implement-скидки и обязательные эффекты | активации-оружие, периодические события, атомарные раны, once/encounter |
| **ROT-MAG-12** | состав Conjure и уровни призыва, запрет стака Augment как правило каталога | временные инстансы с истечением, Concentrate, длительности |
| **ROT-MITEM-01** | каталог 17 предметов, экономика, инстансы | боевые эффекты и длительности |
| **ROT-ALCH-01** | каталог 12 расходников, экономика, дозы как инстансы | боевые эффекты и длительности доз |

## Следствие для магии

Вместе с **ROT-MAG-08** исчезает единственная поверхность, где сервер мог бы принять состав
заклинания. Поэтому остатки **ROT-MAG-01**, **ROT-MAG-02** и **ROT-MAG-03** — «сервер отклоняет
вручную подставленный effect», серверная ошибка на дубликат неповторяемого эффекта и атомарный
отказ несочетаемого набора — закрыты как **не будет**, а не отложены. Правило остаётся доменным
(`MagicMatrix`) и клиентским: сборщик по нему фильтрует, справочник объясняет, тесты стерегут.
Талантовая половина ROT-MAG-03 при этом работает на сервере — она живёт в покупке талантов
(`TalentDef.ExcludesTalentCodes`) и от рантайма не зависит.

## Вынесенные задачи целиком

## Магия

### ROT-MAG-05. Additional Summon и post-roll Advantage

Conjure `Additional Summon` повышает difficulty на 1 и добавляет один summon.
После успешного cast можно получить ещё по одному summon за **2 Advantage**,
повторяя трату при наличии symbols. Один Advantage недостаточен. Все дополнительные
summons соответствуют выбранному уровню Conjure и ограничениям цели.

Effect выбирается в builder один раз; количество post-roll additions хранится в
result allocation. Сервер не позволяет потратить одни symbols дважды. Проверить
0/1/2/4 Advantage, несколько видов summon, failed cast и atomic allocation.

### ROT-MAG-07. Точный Curse effect Misfortune

`Misfortune`, difficulty +1: после того как затронутая цель бросила check, но до
resolution, заклинатель может повернуть **одну Setback die** на грань, содержащую
Failure. Если в реально брошенном pool нет Setback die, эффект применить нельзя.
Эффект не добавляет готовый Failure, не меняяет Ability/Difficulty/Challenge dice и
не позволяет выбрать произвольную грань.

Roll сохраняет отдельные dice faces, чтобы authoritative resolver проверял тип die,
timing и новую грань. UI предлагает только допустимые Setback; event log фиксирует
исходную/новую грань. Тесты: нет Setback, несколько Setback, грань с несколькими
symbols, запрос после resolution, concurrent resolution.

### ROT-MAG-08. Итоговая difficulty, предел Formidable и стоимость заклинания

Pipeline строится строго в таком порядке:

1. взять base difficulty action;
2. добавить стоимость каждого валидного выбранного effect с допустимой
   multiplicity;
3. применить все действующие снижения difficulty от implement, rune, talent и
   Signature Spell в определённом порядке; downgrade/upgrade dice не смешивать с
   простым числовым снижением;
4. получить final difficulty;
5. отклонить cast, если final difficulty выше Formidable (5 Difficulty dice).

Именно **итоговое** значение после законных reductions должно быть ≤ 5. Нельзя
сначала отрезать raw difficulty до 5 и затем ещё уменьшить её. Difficulty не ниже
Simple (0), если конкретный эффект не говорит иначе. Любая magic action после
попытки cast наносит caster 2 strain независимо от успеха; preview strain не
списывает.

`SpellCalculationBreakdown` показывает каждую строку base/add/reduction/final,
invalid reason и dice upgrades. Server пересчитывает его по stable codes.
Использовать один обязательный двухфазный protocol:

1. `StartMagicCheck` до mutation валидирует actor/action/targets/slot, spell
   multiset, final pool, implement и cost. В одной транзакции он занимает Action,
   наносит 2 strain (Rival — wounds; Minion без special override rejected),
   выполняет authoritative server roll и создаёт immutable `PendingMagicResult`.
2. `ResolveMagicCheck` принимает только pending result ID и допустимое распределение
   symbols/targets. В одной транзакции применяет hit/heal/conditions/summons,
   закрывает result и публикует log. Повтор с тем же idempotency key возвращает тот
   же ответ.

Невалидный `Start` не списывает ничего. После успешного `Start` отказ игрока от
resolution и failed check не возвращают Action/strain; GM может завершить
зависший result только как audited resolution. Клиент не передаёт готовые dice,
symbols или final difficulty. Для физических кубов существует отдельный GM-only
manual roll path с reason, как в ROT-SOC-09.

Тесты: raw 5/6/7 с reductions 0/1/2, floor 0, Signature Spell, implement,
tampered final, failed check и retry. `PrivateFull` объясняет sequence.

## Таланты и героика

### ROT-TAL-05. Общий lifecycle активных талантов

Различать `Session`, `Encounter`, `Round`, `Turn`; once/session не сбрасывается при
новом encounter, once/encounter сбрасывается только на `StartEncounter`,
once/round — на новый round.

- `TalentDef/RuleEffectDef`: activation, out-of-turn flag, trigger, typed costs,
  frequency и expiry.
- `TalentUse`: session/encounter/round/turn, participant, talent, targets, costs,
  outcome, idempotency key.
- `ActiveTalentEffect`: typed expiry
  (`NextCheck`, `StartOwnerNextTurn`, `EndOwnerNextTurn`, `EncounterEnd`).
  Временный эффект не изменяет base stat.
- Story cost переворачивает point `player → GM`. Cost, use log и effect —
  одна транзакция с concurrency control.
- Активировать можно только owned talent при валидных choices и trigger. Pool,
  target, turn/timing и limit проверяет сервер. Произвольное применение чужого
  таланта к NPC запрещено; допустим отдельный audited GM override.
- Roll-dependent эффект принимает authoritative roll/result allocation, а не
  клиентское число успехов. Lifecycle hooks снимают modifiers и сбрасывают scopes.

UI не показывает `Activate` у Passive, показывает точную стоимость/trigger/limit,
valid targets, counters и expiry. Вне Game Session разрешён только reference либо
явный manual tracker — не фиктивная автоматизация.

Тесты: дублированная доставка, гонка за последнюю Story Point, out-of-turn,
incapacitation, все expiry, encounter/session reset, retired historic talent.
**Миграция:** use/effect persistence. **Зависимости:** ROT-TAL-01—04 и Game Table
lifecycle.

### ROT-HA-08 (heroic 8). Полная Miraculous Recovery

- Base: при activation и в начале каждого собственного хода, пока ability active,
  heal ровно 3 wounds, не ниже 0.
- Improved: при activation вместо частичного activation-heal убрать все current
  wounds; start-turn ticks всё равно лечат по 3.
- Supreme: при activation дополнительно убрать ровно одну выбранную current
  Critical Injury без Medicine/Resilience check.

Typed strategy имеет `OnActivate` и `OnOwnerTurnStarted`. Для Power 2 request
содержит принадлежащий персонажу `CriticalInjuryId`, когда игрок применяет Supreme.
В одной транзакции: validate cost/use/choice, flip Story, increment use, heal,
remove selected injury, создать active state. После heal-all не запускать ещё раз
base heal 3. Один turn hook срабатывает ровно один раз при retry.

Если wounds 0 или injuries нет, соответствующая часть даёт 0, но ability можно
активировать ради других эффектов. Healing wounds не отменяет terminal state само
по себе; policy лечения `Dead` должна совпадать с общей Critical Injury системой и
быть покрыта явным тестом. Concurrent removal выбранной injury отменяет всю
activation до Story spend.

Acceptance examples:

- Power 0: wounds 10 → 7 при activation → 4 в начале следующего owner turn;
- Power 1: wounds 10 → 0 при activation; следующий tick остаётся 0;
- Power 2: то же и удаляется только выбранная injury;
- Duration 0 всё равно даёт activation и один next-turn tick; каждый Duration rank
  даёт ещё один tick.

UI до подтверждения показывает прогноз и picker injury, затем отдельные log events
activation/critical/periodic. **Дополнительной миграции сверх ROT-HA-05 нет.**

### ROT-HA-10 (heroic 10). Unleash: activation и defeat action

Activation Heroic Ability всегда остаётся общей Incidental. Пока ability active:

- Base: once/round в собственный ход потратить Maneuver и без check defeat ровно
  одну live minion group в Short;
- Improved: заменить этот Maneuver на Incidental; остальные ограничения те же;
- Supreme: непосредственно при activation defeat **всех minions во всех minion
  groups** в Short.

Rival, Nemesis, PC и более дальние цели не получают damage. После Supreme
Base/Improved остаётся доступен в последующих rounds активности. Пустая Short area
не запрещает activation ради secondary effects.

- Разделить `ActivateHeroicAbility` и `UseUnleashActiveEffect`. Activation не
  списывает Maneuver.
- `UnleashUsedRound` хранится в active state; target и range определяет
  authoritative GameTable, не клиент.
- Defeat устанавливает group count 0/defeated, не создаёт Critical Injury и не
  симулирует произвольные wounds.
- UI: общая кнопка `Activate — Incidental`, после неё отдельная кнопка с ценой
  Maneuver/Incidental и picker только допустимых групп; Power 2 preview перечисляет
  все затронутые группы.
- Проверить once/round/reset, movement target до commit, mixed participant types,
  activation+use в один ход, expiry и атомарность.
- **Миграция:** только зависимые поля minion group/ROT-HA-05. **Зависимость:**
  полноценные Minion Groups, range и round/action economy.

## Бой и minion groups

### ROT-CMB-05. Strain у Rival

- Rival не имеет ST и отдельного current strain.
- Любой strain, включая social strain, превращается для Rival в равное количество
  wounds.
- Rival, в отличие от Minion, **может** добровольно оплачивать способность strain;
  цена становится wounds.
- Rival получает Critical Injuries обычным способом. При превышении WT GM решает,
  смерть это, потеря сознания или иной исход.
- Recover strain к Rival неприменим и не лечит wounds.

`GameParticipant` хранит immutable rules profile
`PC|Minion|Rival|Nemesis|Hazard`/`NpcKind`, а не только общий `Npc`.
Единый `ApplyStrain(amount,cause,voluntary)` маршрутизирует:

- PC/Nemesis → current strain;
- Rival → current wounds;
- Minion → current wounds, но `voluntary=true` запрещён.

Soak внутри `ApplyStrain` не применяется повторно: combat strain damage уже
разрешён combat pipeline, social strain/voluntary cost soak не используют.
Rival DTO не позволяет задавать ST/current strain. UI скрывает strain bar и
показывает preview преобразования.

Legacy Rival ST очистить; current strain активного Rival прибавить к current wounds
и обнулить. Для manual legacy NPC без kind **не** выводить kind из наличия ST,
имени, count или набора skills. Сохранить оба current value и original snapshot,
поставить `NpcKindReviewRequired`; до явного выбора GM разрешены просмотр/печать,
но не mechanical damage/strain/cast.

Тесты: 3 social strain → 3 wounds; добровольная цена 2 → эффект +2 wounds;
Minion та же цена → atomic reject; recover no-op; Rival critical обычный.

### ROT-CMB-06. MinionGroup только для Minion

В одну группу входят только minions одного типа. Rival/Nemesis всегда отдельны.

- `ParticipantType.MinionGroup` требует `Npc.Kind=Minion`, count ≥ 1 и per-member
  profile. Один Minion допустим с group-skill rank 0.
- Rival/Nemesis participant имеет count 1. Удобная `bulk add N` создаёт N разных
  participant IDs с независимыми wounds/criticals/turn state.
- Manual group требует rules profile Minion, per-member WT и group skills; имени и
  количества недостаточно.
- Проверка применяется в encounter factory, Game Table factory, update/import и
  SendToGameTable. Нельзя выводить kind только из `count > 1`.
- UI показывает group size только Minion; несовместимый request →
  `participant.minion_group.requires_minion`.

Legacy valid Minion groups сохраняются. Illegal Rival/Nemesis group нельзя
автоматически разворачивать: aggregated wounds, criticals, conditions и turn state
не имеют единственного правильного распределения. Сохранить locked snapshot,
поставить `NeedsGmReview` и блокировать mechanical apply. GM repair wizard явно
создаёт N отдельных records, просит распределить все четыре вида состояния,
показывает preview и только после подтверждения делает одну audited conversion.

Тесты: Minion 1/4, единый Rival 2 rejected, bulk 3 → три IDs, prepared encounter
guard, migration review без потери исходного snapshot.

---

### ROT-MIN-01. Профиль и состав

- Minion не имеет ST и индивидуальных skill ranks. Профиль содержит множество
  `GroupSkillDefIds`; старое `NpcSkill.Ranks=0` может быть переходным DTO, но runtime
  никогда не читает его как реальный rank.
- Один Minion может действовать один; effective rank каждого group skill тогда 0.
- Группа содержит только minions одного immutable source profile/version.
  Одинаковое display name не разрешает смешать разные statblocks.
- Rival, Nemesis, PC и Hazard не входят в MinionGroup.
- `InitialCount >= 1`. Крупный размер может дать GM-warning, но Core не задаёт
  hard cap 5.
- При смене kind на Minion очистить ST и явно выбрать group-skill membership; при
  смене обратно потребовать ranks, не угадывать их из старого размера группы.

### ROT-MIN-02. Общий WT, wounds и casualties

Для `N` одинаковых minions и индивидуального `T`:

```text
GroupWoundThreshold = N × T
ComputedDefeatedByWounds =
    min(N - ExplicitDefeatedCount,
        floor(max(0, GroupWoundsCurrent - 1) / T))
DefeatedByWounds =
    max(PersistedDefeatedByWounds, ComputedDefeatedByWounds)
DefeatedCount =
    min(N, DefeatedByWounds + ExplicitDefeatedCount)
RemainingCount = N - DefeatedCount
GroupDefeated when DefeatedCount = N
```

Wounds копятся в одном tracker; остаток после границы не теряется.
Обязательный пример для docs/UI/tests: `N=3, T=4`, total threshold 12:

| Current wounds | Осталось |
|---:|---:|
| 0–4 | 3 |
| 5–8 | 2 |
| 9–12 | 1 |
| 13+ | 0 |

Чтобы healing не «воскрешал» уже выбывших, хранить два монотонных счётчика:
`PersistedDefeatedByWounds` и `ExplicitDefeatedCount`. Второй используется
Unleash/GM defeat без симуляции wounds. После damage/critical сервер сначала меняет
`GroupWoundsCurrent`, затем повышает первый счётчик до computed value. Обычное
wound healing уменьшает `GroupWoundsCurrent`, но не ниже
`PersistedDefeatedByWounds × T` и не меняет ни один count.

Возврат одного члена — только audited GM command `RestoreMinionMember` с
обязательным `defeatKind=Wounds|Explicit`. Для `Explicit` уменьшить только
`ExplicitDefeatedCount`. Для `Wounds` уменьшить `PersistedDefeatedByWounds` ровно
на 1 и ограничить current wounds сверху новым
`PersistedDefeatedByWounds × T`, чтобы восстановленный член не выбыл немедленно
из-за старого damage. Команда не может дать count выше InitialCount и показывает,
сколько wounds будет снято как часть восстановления.

### ROT-MIN-03. Damage, strain и social outcome

- Attack механически выбирает всю группу. Один hit проходит group soak один раз,
  затем все прошедшие wounds попадают в общий tracker и могут пересечь несколько
  границ.
- При нескольких hits soak применяется к каждому hit отдельно; прошедшие значения
  добавляются последовательно, а log сообщает casualties.
- Любой incoming strain преобразуется в столько же wounds. Minion не может
  добровольно получить strain за maneuver/talent/ability; вся операция отклоняется
  до эффекта.
- Поражение от social strain означает механическую капитуляцию/выбывание; GM
  выбирает «уступили, разбежались, потеряли волю», а не автоматическую смерть.

### ROT-MIN-04. Group skills

Только для skill, указанного в profile:

```text
effectiveRank = RemainingCount - 1
```

У неперечисленного skill rank всегда 0. Pool строится заново непосредственно перед
check из характеристики профиля и effective rank; casualty сразу уменьшает все
group skills на 1. Artificial cap 5 не добавлять: группа из шести имеет rank 5,
из семи — rank 6, если GM создал такую группу.

Пример: count 5 → rank 4; после потери двух → rank 2; неперечисленный Athletics
остаётся 0.

### ROT-MIN-05. Critical Injuries

- Одиночный Minion при Critical Injury сразу выбывает; d100 не бросается.
- MinionGroup при Critical Injury не создаёт `CriticalInjury` row и не бросает
  таблицу. Она получает прямо в общий tracker `T + 1` wounds без soak, после чего
  применяется обычная casualty formula. Это гарантирует минимум одну потерю и при
  уже накопленном damage может пересечь дополнительные границы.
- Право на обычный critical всё равно требует successful hit, минимум 1 damage
  после soak и оплаты Critical rating/Triumph. Miss или полностью поглощённый hit
  не создаёт group critical без отдельного явного правила.

### ROT-MIN-06. Snapshot и server operations

GameParticipant хранит:

- rules profile Minion, source/version;
- Initial/Remaining count, per-member WT, current wounds и computed total WT;
- характеристики, soak, defense contributions, silhouette;
- group skills, attacks/qualities, talents, abilities, equipment/conditions.

Display name, count и total WT без per-member WT недостаточны.

Авторитетные idempotent/versioned команды:

- `ApplyDamageToParticipant`;
- `ApplyStrainToParticipant`;
- `ApplyCriticalToParticipant`;
- `RestoreMinionMember` (GM only);
- отдельный audited GM override, после которого derived state пересчитывает server.

SignalR публикует уже разрешённый result. UI показывает `осталось/было`, wounds/total,
per-member WT, следующий casualty threshold, текущие group ranks; у группы нет
strain bar и d100 critical dialog.

### ROT-MIN-07. Encounter, legacy и тесты

- Encounter хранит kind/source, quantity и
  `PerMemberWoundThresholdOverride`; `StartingWoundsOverride` не может
  двусмысленно означать individual WT.
- SendToGameTable атомарно создаёт snapshot и повторяет CMB-06 guard.
- Для legacy source Minion: N = old count, T = source WT, current wounds сохраняются;
  несовпадение старого total с N×T → GM review.
- Для manual legacy без source частное `oldTotal/count` допустимо только при
  положительном значении и делении без остатка; иначе GM обязан задать T до
  механической операции.

Обязательная матрица:

- `T=4,N=3`: wounds 4/5/8/9/12/13 → remaining 3/2/2/1/1/0;
- один hit пересекает две границы; overflow сохраняется;
- multi-hit soak отдельно;
- critical добавляет T+1, без d100/entity;
- group/unlisted ranks и pool после casualty;
- incoming/voluntary strain;
- healing без revive и explicit restore;
- guards во всех API/import/encounter paths;
- repeated damage/critical idempotent;
- snapshot работает после изменения исходного custom NPC;
- frontend использует новый rank и блокирует unresolved legacy group.

**Миграция:** явные group skills и расширенный participant/encounter snapshot.

---

## Социальные столкновения

### ROT-SOC-01. Три режима и явные цели

`EncounterType.Social` имеет ровно один из трёх resolution modes:

1. `Narrative/MutuallyAgreeable`: стороны нашли приемлемое решение; GM фиксирует
   outcome без check.
2. `SingleCheck`: одна opposed check либо fixed difficulty против аудитории. Success
   достигает простой заявленной цели; failure не достигает. Автоматические 2 strain
   актёру за failure здесь **не применяются**.
3. `ComplexStrain`: серия социальных действий создаёт pressure через strain до
   compromise/capitulation.

До начала каждая заинтересованная сторона имеет конкретную цель. Сложную цель
разбить на `SocialSubgoal` со статусом
`Pending|Succeeded|Failed|Compromise|Capitulated`, player-visible summary,
GM-only principles/limits и condition завершения. Сцена заканчивается при достижении
цели либо когда в ней цель уже недостижима.

Complex mode требует механическую цель с WT/ST. Таблица размера аудитории определяет
difficulty, но не создаёт общий threshold толпы; для длительного давления на толпу
GM задаёт statblock/threshold или subgoals.

### ROT-SOC-02. Freeform по умолчанию, rounds опционально

- Обычная social scene не имеет initiative, turn order и action economy.
- GM включает rounds только для длинной/сложной/многосторонней сцены. Initiative
  всё равно не бросается.
- В каждом social round каждый участник получает одну возможность в narrative
  order: действует либо пасует.
- За возможность: максимум одна Action-cost ability и одна Maneuver-cost ability.
  Skill check занимает Action slot; Maneuver ability остаётся возможной.
- Длительность раунда повествовательная.

Хранить `HasActed`, `ActionSlotUsed`, `ManeuverSlotUsed`, `Passed`; GM явно
завершает round/override с log. Freeform не создаёт slots, но
once-per-encounter/session limits продолжают работать.

### ROT-SOC-03. Opposed skills и аудитория

| Acting skill | Default opposing skill |
|---|---|
| Charm | Cool |
| Coercion | Discipline |
| Leadership | Discipline |
| Deception | Vigilance |
| Negotiation | Negotiation |

Особенно важно: Deception противостоит Vigilance, не Discipline. Mapping основан на
stable IDs. GM может выбрать иной opposing skill только для конкретной ситуации с
reason/audit; глобальная таблица не меняется.

Для одновременного обращения к аудитории без individual statblock:

| Число целей | Difficulty |
|---:|---|
| 2–5 | Average (2) |
| 6–15 | Hard (3) |
| 16–50 | Daunting (4) |
| 51+ | Formidable (5) |

Одна цель всегда использует opposed check. Отношение и обстоятельства добавляются
видимыми Setback/Boost/upgrades, не скрытой заменой difficulty.

### ROT-SOC-04. Pressure action в ComplexStrain

- Актёр выбирает уместный social skill и описывает подход; несоответствие требует
  явного GM override.
- Success: цель получает `1 + netSuccesses` strain, минимум 2.
- Failure: цель не получает base social strain, актёр получает 2 strain.
- Symbols тратятся отдельно и могут изменить результат.
- Weapon/combat check не наносит social strain. При насилии GM завершает/переводит
  сцену в combat.

Маршрутизация:

- PC/Nemesis → current strain;
- Rival → wounds;
- Minion/MinionGroup → wounds и casualties, но social defeat означает уступку/
  бегство/потерю воли, не automatic death;
- soak social strain не уменьшает.

Используется текущий общий resource, включая накопленный до сцены; отдельную
«социальную HP-полоску» не создавать. Failure с Advantage всё равно наносит актёру
2; разрешённый spend может отдельно затронуть цель. Если обе стороны пересекли
threshold в одном resolution, сохранить оба outcome events.

### ROT-SOC-05. Critical Remark

На social check один раз потратить **1 Triumph или 4 Advantage**, чтобы нанести цели
5 strain. Обязателен короткий собственный текст реплики/аргумента. Успех проверки
не требуется: failure может одновременно дать актёру 2 strain, а цели — 5.

Повтор на одной check запрещён, кроме структурного эффекта, который прямо разрешает
несколько применений, например Improved Influential; каждое оплачивается отдельно.
Strain маршрутизируется по SOC-04 без soak.

Тесты: success net2 + remark → 3+5=8; failure +4 Advantage → actor2/target5;
3 Advantage недостаточно; symbols не тратятся дважды; пустой narrative отклоняет
resolution до mutations.

### ROT-SOC-06. Компромисс и капитуляция

```text
CompromiseAvailable when current × 2 > threshold
Capitulation/Failure when current > threshold
```

Равенство половине или threshold недостаточно.

- Для NPC compromise только открывает возможность: GM формулирует допустимую часть
  цели, другая сторона предлагает встречную уступку; она не ломает core principles.
- При превышении threshold NPC уступает в пределах заранее допустимой цели.
  Capitulation не mind control и не делает физически невозможное возможным.
- PC при current > ST проваливает свою цель; GM может потребовать подходящую уступку.
  На половине только игрок решает, готов ли PC к compromise.
- Rival/Minion используют WT; outcome помечается social, не `Killed`.

Границы: ST10 → 5 ничего, 6 compromise, 10 ещё не capitulation, 11 capitulation;
ST9 → 4 ничего, 5 compromise, 10 capitulation.

### ROT-SOC-07. Motivations

Поддержать `Strength`, `Flaw`, `Desire`, `Fear` с отдельной player visibility:

- сыграть на Strength/Flaw → один Boost;
- сыграть на Desire/Fear → два Boost;
- идти против Strength/Flaw → один Setback;
- идти против Desire/Fear → два Setback.

Каждый facet заявляется максимум один раз на check, только после подтверждения GM и
с объяснением. Разные facets могут складываться. Не определять релевантность
ключевыми словами.

Minion motivations обычно отсутствуют; Strength/Flaw для разового и все четыре для
важного NPC — guidance, не hard validation. Optional GM check
`Perception vs Cool` может раскрыть один facet, обычно один раз на пару
observer/target; это не автоматическое обязательное правило.

GM видит все NPC facets; игрок — только раскрытые. Владелец PC видит свои.

### ROT-SOC-08. Структурированные symbol spends

Стандартные positive options:

- 1 Advantage: heal actor 1 strain (repeatable); следующему действующему ally один
  Boost; заметить важную деталь;
- 2 Advantage: узнать Strength/Flaw; дать target один Boost к следующей check, если
  уместно; дать любому ally, включая себя, один Boost к следующей check;
- 3 Advantage: узнать Fear/Desire; скрыть свою истинную цель; узнать истинную цель
  собеседника;
- Triumph может оплатить Advantage option либо с разрешения GM: узнать любой один
  facet; upgrade difficulty следующей check target; upgrade ability следующей
  check ally; создать крупную возможность/отвлечение.

Negative options:

- 1 Threat: actor получает 1 strain (repeatable); временно сбивается;
- 2 Threat: раскрывает свой Strength/Flaw; даёт target Boost; даёт себе/ally Setback
  к следующему действию;
- 3 Threat: раскрывает свой Desire/Fear; истинную цель; один facet ally;
- Despair может оплатить Threat option либо: дать правдоподобную ложную информацию о
  facet до опровержения; upgrade difficulty следующей social check себя/ally;
  лишить actor значимого действия в следующем social round.

Кроме explicitly repeatable strain options и особого talent, один option максимум
один раз/check. `PendingEffect` хранит target/source/expiry/consumption. GM может
добавить custom narrative spend, но он логируется как manual, не fake automation.

### ROT-SOC-09. Model, API, безопасность и UI

Модель: EncounterType, resolution/timing modes, goals/subgoals/outcome/NeedsSetup.
Participant snapshot содержит stable social skills/ranks/characteristics, rules
profile, WT/ST/current и motivations visibility.

Use cases:

1. `PreviewSocialCheck`: actor, skill, target либо audience count, approach,
   proposed facets/modifiers → authoritative pool breakdown;
2. `RollSocialCheck`: preview/version → server roll с неизменяемыми dice faces и
   итоговым symbol budget;
3. `ResolveSocialCheck`: consumed `rollId`, spends, narrative,
   idempotency key → одна транзакция base result + spends + thresholds + log;
4. `ResolveManualSocialCheck`: GM-only полный ручной result для физических кубов,
   reason и confirmation; server всё равно проверяет cancellation/spend budget;
5. social round pass/advance без initiative;
6. offer/accept/reject compromise, mark capitulation, end encounter;
7. reveal motivation facet.

Обычный resolve не принимает от клиента произвольные Success/Advantage/Triumph.
`rollId` связан с actor, target/audience, pool hash, encounter, action slot и
rowVersion, используется ровно один раз. Manual result помечается во всех логах и
SignalR как `AuthoritativeSource=GmManual`; игрок не может подтвердить его за GM.

Точный lifecycle стандартных pending modifiers:

- «следующий allied active character» выбирается сервером в момент, когда следующий
  союзник реально начинает действие; Boost расходуется на его первую check этого
  действия и истекает в конце encounter, если check не было;
- modifier к `target next check` и `any ally next check` привязывается к явно
  выбранному target и расходуется его следующей skill check, после чего удаляется;
- `upgrade target next check` и `upgrade ally next check` работают так же;
- Setback «на следующем action» помечает следующее действие выбранного персонажа:
  если оно содержит check, die добавляется к ней; если check нет, penalty всё равно
  считается израсходованным по завершении action;
- пропуск social round не расходует `next action/check`;
- все неиспользованные `next` effects истекают в конце encounter;
- reveal/conceal/false facet и goal не являются dice modifiers: они живут до
  окончания encounter либо до явного narrative опровержения, которое фиксирует GM.

Из стандартной таблицы повторяемы только `heal actor 1 strain` за каждый отдельный
Advantage и `actor suffers 1 strain` за каждый отдельный Threat. Все остальные
listed options — максимум один раз на check. Improved Influential является явным
исключением только для Critical Remark и оплачивает каждое повторение отдельно.

Игрок управляет своим PC; GM — NPC и обязательными NPC outcomes. PC compromise
подтверждает player. GM-only goals/unrevealed facets не попадают REST/SignalR.
UI показывает mode, goal tree, exact opposing source, pool breakdown, strict
threshold markers и acted/passed без initiative.

Legacy text goals превращаются в pending player goal/GM-only counter-goal с
сохранением summary; пустые → NeedsSetup. Default legacy mode Narrative+Freeform,
чтобы не включить complex mechanics неожиданно. Existing vitals/logs не терять.

### ROT-SOC-10. Acceptance matrix

- Все пять skill pairs и границы audience 2/5/6/15/16/50/51.
- Complex net1/net3 → target2/4; failure → actor2; SingleCheck failure → 0.
- Rival/Minion routing, no soak; weapon rejected.
- Critical Remark на success/failure.
- Чётный/нечётный strict thresholds, reciprocal compromise, PC voluntary,
  impossible goal.
- Motivation ±1/±2, secret projection.
- В round каждый действует/пасует один раз; slots и reset; freeform без slots.
- Idempotent apply, concurrent thresholds, REST/SignalR secrecy.
- При завершении предложить PC Simple Cool или Discipline recovery: heal 1 strain
  за каждый uncanceled Success; это отдельный Core recovery flow, не полное лечение.

**Миграция:** structured social encounter/goals/motivations/spends and safe
legacy projection.

---

## Снаряжение

### ROT-EQP-AMMO-01. Narrative ammo, Limited Ammo и thrown instances

1. Обычные ranged weapons используют narrative ammo: атаки не уменьшают стрелы/
   болты поштучно. GM может потратить Despair ranged combat check на
   `OutOfAmmo`; до пополнения weapon не атакует.
2. Sling — исключение RoT: Threat/Despair не делают его OutOfAmmo.
3. Extra Quiver за Maneuver снимает OutOfAmmo обычного ranged weapon; Limited Ammo
   не пополняет.
4. `Limited Ammo N`: до reload N attacks; после исчерпания Maneuver reload/draw и
   реальный spare ammo unit. Narrative OutOfAmmo и Extra Quiver здесь не работают.
5. Thrown dagger/light spear/throwing axe с Limited Ammo1 переходят
   `Thrown/Unavailable` до recovery. Они не уничтожаются. Другой physical instance
   можно draw/reload Maneuver.
6. Только `ConsumedOnFire` удаляет unit; LimitedAmmo1 само по себе не означает
   расходуемость.
7. Optional Core fantasy `TrackIndividualArrows` (default off): пять arrows/bolts
   стоят 1 currency и расходуются поштучно.
8. Optional `LastArrowOnOutOfAmmo` (default off): при OutOfAmmo bow/crossbow
   потратить 1 Advantage текущей check, чтобы оставить одну последнюю missile и
   получить ещё одну атаку.
9. Prepare N независимо требует N preparation maneuvers; disruption может reset
   по GM. Pike сохраняет свой Short/Average/not Engaged/Prepare1.

Instance хранит AmmoMode, OutOfAmmo, shots/reload units, prepared maneuvers,
thrown state и selected profile. Resolve attack атомарно меняет state. UI различает
out-of-ammo, limited, prepare и thrown; даёт prepare/reload/quiver/recover.

Тесты: bow Despair, sling immunity, quiver, границы Limited N, thrown vs consumed,
две копии, optional toggles, prepare/reset и round-trip.

