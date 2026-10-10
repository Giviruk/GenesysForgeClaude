# Членство в кампании, личная библиотека и создание персонажей

**GEN-CONTENT-01.** Пересмотренный план от 07.10.2026, по коду ветки на базе `master` `25ae5c9`.
Файл плана задачи: [account-campaign-content-design.md](../roadmap/tasks/account-campaign-content-design.md).
Документ описывает реализацию; код, API и миграции этим документационным PR не меняются.

**Решение после ревью #268:** мастер подключает исходный набор игрока по shared-ссылке.
Владелец, HomebrewPackId и ID определений сохраняются; копии для кампании не создаются.
После подключения можно развивать уже купленные навыки и таланты с теми же ID и рангами.
Новые покупки до разрешения мастера остаются недоступны. Подключить оригинал по shared-ссылке
можно только если владелец — мастер или текущий участник этой кампании.

**Решение 07.10.2026:** автор может править подключённый исходный набор, правки сразу действуют
в кампании — запрета нет. Вместо него прозрачность: дата последней правки у каждой кастомной записи
и журнал изменений набора, видимый мастеру и участникам кампании (этап 5).

При выходе/исключении автора набор не отключается автоматически. Мастер управляет связью вручную.
Список наборов показывает OwnerName и OwnerIsMember и отмечает, что игрок покинул кампанию.

## Redesign v2 — актуальное дополнение (GEN-RD-01–09)

[ТЗ](redesign-v2-spec.md), [план реализации](../roadmap/tasks/gen-rd-v2-redesign.md).
Это дополнение заменяет нижеописанную v1-логику авто-наборов и единственного `HomebrewPackId`.

- Элемент принадлежит аккаунту и может входить в несколько собственных наборов той же системы.
  Создание и выбор `packIds` сохраняются атомарно; вне наборов элемент доступен своему владельцу.
- Библиотека показывает все собственные записи, включая содержимое выключенных наборов.
  Для персонажей вне кампаний сохраняются default/per-character переключатели наборов.
- Мастер подключает исходный набор или отдельный элемент участника; JSON-импорт создаёт копии.
  Игрок предлагает своё содержимое, отзывает ожидающее предложение и видит решение мастера.
- Active + enabled определяет доступность подключения. Manual оставляет новые элементы Pending;
  Auto разрешает новые элементы. Правки механики уже подключённых оригиналов действуют сразу.
- Разрешения нескольких кампаний объединяются. Собственная библиотека не обходит решения мастера.
  Отключение сохраняет купленные/выданные записи, но закрывает новые покупки.
- Бесплатные стартовые ранги вида/карьеры сохраняются даже при отключённом навыке.
- Закрытая система блокирует создание/добавление персонажей, не блокируя существующие листы.
- Ограничения книг задаются наборами и ручными overrides; явное разрешение мастера отменяет
  исключение. Транспорт и улучшения не являются категориями ограничений книг.
- «Сохранить как набор» переносит эффективные запреты в новый набор и выключает собственные
  наборы только с ограничениями. Разрешающие overrides против оставшихся чужих/смешанных
  наборов сохраняются по решению пользователя: доступность до и после идентична.
- Экспорт v2 содержит exclusions и восемь типов записей; импорт поддерживает v1/v2, пропускает
  неизвестные ключи ограничений с предупреждением и создаёт новые ID.
- Контент кампании — последний раздел только для мастера; старый `/custom` перенаправляется
  на `/content`. Вкладки: сводка, системы, наборы, отдельные элементы; состояние хранится в URL.
- GEN-RD-10 (кастомная магия) согласован как отдельная будущая задача.

## Что получает пользователь

- Игрок вступает в кампанию по коду **без персонажа**, видит контент кампании и создаёт персонажа
  уже внутри неё (в том числе на кастомных архетипе/карьере мастера).
- Снятие или удаление персонажа не лишает игрока членства. Выход и исключение — отдельные действия.
- Кастомный контент создаётся без кампании и без роли мастера (личная библиотека). Мастер подключает
  свой набор либо исходный набор игрока по shared-ссылке.
- **Подтверждено 06.10.2026:** внутри кампании личный контент игрока доступен только после
  разрешения мастера.

## Текущий разрыв (проверено по коду)

- Вступление требует готового персонажа: [JoinCampaignHandler.cs:19](../backend/src/GenesysForge.Application/Features/Campaigns/JoinCampaignHandler.cs).
- Членство выводится из `CampaignCharacters`: [CampaignMapper.GetAccessibleAsync](../backend/src/GenesysForge.Application/Features/Campaigns/CampaignMapper.cs).
  Снятие последнего персонажа = потеря доступа к кампании.
- Создание персонажа берёт только **свой** кастом (`OwnerUserId == userId`):
  [CreateCharacterHandler.cs:26](../backend/src/GenesysForge.Application/Features/Characters/CreateCharacterHandler.cs).
  При этом покупка таланта уже видит наборы кампании через `IsVisibleCustom` — правила расходятся.
- Кастом создаётся только через маршрут кампании и только мастером:
  [CampaignCustomContent.cs:16](../backend/src/GenesysForge.Application/Features/CustomContent/CampaignCustomContent.cs).
- Справочник уже принимает `campaignId` ([ReferenceEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ReferenceEndpoints.cs)),
  но для игрока без персонажа проверка доступа к кампании не проходит.

## Этапы

| # | Этап | Зависит от | PR |
| --- | --- | --- | --- |
| 1 | Членство `CampaignMember` | — | отдельный, с миграцией |
| 2 | Создание персонажа в кампании, добавление существующего | 1 | отдельный |
| 3 | Личная библиотека | — | отдельный, можно параллельно с 1–2 |
| 4 | Изоляция контекста кампании и сохранность листа | 2, 3 | отдельный |
| 5 | Дата правки и журнал изменений кастома | 4 | отдельный, с миграцией |

Фикс листа из этапа 4 (купленные навыки не пропадают) — самостоятельный баг, его можно выкатить сразу.

---

## Этап 1. Членство в кампании

### Модель

```csharp
// GenesysForge.Domain/Entities/CampaignMember.cs
/// <summary>Аккаунт игрока, вступивший в кампанию. Мастер определяется Campaign.GmUserId и строки не получает.</summary>
public class CampaignMember
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
```

- `AppDbContext`: `DbSet<CampaignMember> CampaignMembers` (+ в `IAppDbContext`), уникальный индекс
  `(CampaignId, UserId)`, FK на `Campaign` — Cascade, FK на `User` — Cascade.
- Мастер — только через `GmUserId`, строки членства у него нет, даже если он добавляет своего персонажа.
- **Инвариант:** у каждой `CampaignCharacter` игрока (не мастера) есть `CampaignMember`
  с теми же `(CampaignId, PlayerUserId)`. Держится кодом: все пути создания связи идут через один
  helper (ниже). Составной FK не вводим — тесты идут на InMemory и его всё равно не проверят.

### Миграция `AddCampaignMembers`

Создать таблицу, затем перенести членство из существующих связей (в той же миграции через
`migrationBuilder.Sql`; Postgres 17, `gen_random_uuid()` встроен):

```sql
INSERT INTO "CampaignMembers" ("Id", "CampaignId", "UserId", "JoinedAt")
SELECT gen_random_uuid(), cc."CampaignId", cc."PlayerUserId", MIN(cc."JoinedAt")
FROM "CampaignCharacters" cc
JOIN "Campaigns" c ON c."Id" = cc."CampaignId"
WHERE cc."PlayerUserId" <> c."GmUserId"
GROUP BY cc."CampaignId", cc."PlayerUserId";
```

`Down` — удалить таблицу. Миграция не деструктивна. Обновить [database.md](database.md).

### Общий helper

`Features/Campaigns/CampaignMembership.cs` — единственное место, где создаются членство и связь персонажа:

```csharp
internal static class CampaignMembership
{
    /// <summary>Добавляет членство, если его нет. SaveChanges делает вызывающий.</summary>
    public static async Task EnsureMemberAsync(IAppDbContext db, Campaign campaign, Guid userId, CancellationToken ct)
    {
        if (campaign.GmUserId == userId) return;
        if (!await db.CampaignMembers.AnyAsync(m => m.CampaignId == campaign.Id && m.UserId == userId, ct))
            db.CampaignMembers.Add(new CampaignMember { Id = Guid.NewGuid(), CampaignId = campaign.Id, UserId = userId });
    }

    /// <summary>Членство + связь персонажа. SaveChanges делает вызывающий — одна транзакция.</summary>
    public static async Task AddCharacterAsync(IAppDbContext db, Campaign campaign, Guid userId, Guid characterId, CancellationToken ct)
    {
        if (await db.CampaignCharacters.AnyAsync(cc => cc.CampaignId == campaign.Id && cc.CharacterId == characterId, ct))
            throw new DomainRuleException("Этот персонаж уже участвует в кампании.");
        await EnsureMemberAsync(db, campaign, userId, ct);
        db.CampaignCharacters.Add(new CampaignCharacter
        {
            Id = Guid.NewGuid(), CampaignId = campaign.Id, CharacterId = characterId, PlayerUserId = userId,
        });
    }
}
```

Двойное одновременное вступление упрётся в уникальный индекс и вернёт ошибку вместо дубля — допустимо.

### Проверки доступа: `CampaignCharacters` → `CampaignMembers`

Меняется смысл «пользователь состоит в кампании»:

| Место | Что заменить |
| --- | --- |
| [CampaignMapper.cs:56](../backend/src/GenesysForge.Application/Features/Campaigns/CampaignMapper.cs) `GetAccessibleAsync` | `db.CampaignMembers.AnyAsync(m => m.CampaignId == campaignId && m.UserId == userId)`. Автоматически чинит 14 потребителей: SignalR-хаб, заметки, хронику, игровой стол, столкновения, справочник с `campaignId`. |
| [GetCampaignsHandler.cs:16](../backend/src/GenesysForge.Application/Features/Campaigns/GetCampaignsHandler.cs) | Список кампаний игрока — по `CampaignMembers`. `CharacterCount` (строка 22) оставить по персонажам. |
| [NpcMapper.cs:67](../backend/src/GenesysForge.Application/Features/Npcs/NpcMapper.cs) `CanViewAsync` | Видимость `CampaignVisible` NPC — по членству. |
| [GetNpcsHandler.cs:23, :33](../backend/src/GenesysForge.Application/Features/Npcs/GetNpcsHandler.cs) | То же. |
| [BuyTalentHandler.cs:240](../backend/src/GenesysForge.Application/Features/Characters/BuyTalentHandler.cs) | Спутник из NPC кампании — по членству. |

Остаются на `CampaignCharacters`, потому что речь о связи именно персонажа:
`HomebrewVisibility` (наборы кампаний персонажа), `GetCampaignMemberSheetHandler`, `GetCampaignMemberAuditHandler`,
`ParticipantFactory`, `EncounterParticipantFactory`, `AddCampaignCharacters`, `RemoveCampaignCharacterHandler`.

Член без персонажа на игровом столе может смотреть и бросать кубы (как мастер — без персонажа),
но не управлять участниками: `UpdateParticipant`/`ActivateAbility` уже проверяют владение персонажем.

### API

| Метод и маршрут | Поведение |
| --- | --- |
| `POST /api/campaigns/join` `{ joinCode, characterId? }` | `CharacterId` становится необязательным (`Guid?`). Без него — `EnsureMemberAsync`; повторное вступление не ошибка, просто возвращает кампанию. С ним — `AddCharacterAsync`, как раньше. Один `SaveChanges`. Старые клиенты не ломаются. |
| `POST /api/campaigns/{id}/characters` `{ characterId }` | **Новый.** Добавить своего существующего персонажа: `GetAccessibleAsync` → `db.GetOwnedAsync(character)` → `AddCharacterAsync`. Нужен, потому что игрок не видит `JoinCode` (его отдают только мастеру). |
| `DELETE /api/campaigns/{id}/members/{userId}` | **Новый.** Выход (`userId` — свой) или исключение (вызывает мастер). Мастера удалить нельзя. Удаляет членство и **явно** все `CampaignCharacters` этого игрока в кампании — не полагаться на cascade, на InMemory его нет. |
| `DELETE /api/campaigns/{id}/characters/{characterId}` | Без изменений, но членство теперь остаётся. **Изменение поведения:** раньше снятие последнего персонажа означало выход. |

DTO: в `CampaignDetailDto` добавить последним параметром `List<CampaignPlayerDto> Players`,
`CampaignPlayerDto(Guid UserId, string DisplayName, string? AvatarUrl, bool IsMe, DateTime JoinedAt)` —
без email. Существующий `Members` (это персонажи) не переименовывать ради совместимости клиента.
Обновить [api.md](api.md).

### SignalR

`CampaignHub.SubscribeCampaign` проверяет доступ через `GetAccessibleAsync` — член без персонажа подпишется
без изменений в хабе. После исключения соединение остаётся в группе до переподключения. События «тонкие»
(только `campaignId`, [SignalRCampaignNotifier.cs](../backend/src/GenesysForge.Api/Realtime/SignalRCampaignNotifier.cs)):
клиент перечитывает REST и получает отказ, данные не утекают. Отдельный отзыв подписки не делаем;
понадобится, если события начнут нести данные — тогда хранить соединения пользователя и вызывать
`RemoveFromGroupAsync` при исключении.

### Frontend

- `api/client.ts`: `joinCampaign(code, characterId?)`, `addCampaignCharacter(campaignId, characterId)`,
  `removeCampaignMember(campaignId, userId)`; `api/types.ts`: `CampaignDetail.players`.
- `CampaignsPage` → `JoinCampaignForm`: выбор персонажа необязателен (вариант «без персонажа»), кнопка
  активна при введённом коде; после вступления открыть кампанию.
- `CampaignDetailView`: блок «Игроки» — у мастера кнопка «Исключить», у игрока «Покинуть кампанию».
  У игрока — «Добавить существующего» (свои персонажи, которых нет среди `members` с `isMine`)
  и «Создать персонажа» (этап 2).
- Если перечитывание кампании вернуло отказ (исключили) — вернуть на список кампаний.
- `client.test.ts`: тело `join` без `characterId`, новые методы.

### Тесты (`CampaignTests`)

- Вступление без персонажа: кампания в списке, `GET /api/campaigns/{id}` 200, справочник с `campaignId` 200.
- Вступление с персонажем (старый сценарий) работает, членство создаётся.
- Снятие последнего персонажа — доступ к кампании сохраняется.
- Выход — доступ пропал, персонажи сняты. Мастер исключает игрока — то же; игрок не может исключить
  другого; мастера исключить нельзя.
- Добавление существующего: только своего, только члену; повтор — ошибка.
- `CampaignVisible` NPC мастера видны члену без персонажа.

---

## Этап 2. Создание персонажа в кампании

- `CreateCharacterRequest`: последним параметром `Guid? CampaignId = null` — совместимо со старыми клиентами.
- `CreateCharacterHandler`: если `CampaignId` задан — `campaign = CampaignMapper.GetAccessibleAsync(...)`
  (член или мастер), видимость — `GetVisiblePackIdsAsync(db, userId, req.System, campaignId: req.CampaignId)`.
- Фильтры архетипа, карьеры и навыков ([CreateCharacterHandler.cs:26, :33, :86](../backend/src/GenesysForge.Application/Features/Characters/CreateCharacterHandler.cs))
  заменить с «только свой» на правило, уже используемое листом и покупками
  ([SheetBuilder.cs:114](../backend/src/GenesysForge.Application/Common/SheetBuilder.cs)):
  `x.OwnerUserId == null || (x.HomebrewPackId == null ? x.OwnerUserId == userId : visiblePackIds.Contains(x.HomebrewPackId.Value))`.
  Выражение остаётся inline — EF не транслирует вызов `IsVisibleCustom`.
- Перед единственным `SaveChangesAsync` (строка 192) — `CampaignMembership.AddCharacterAsync`. Все проверки
  выполняются до записи, персонаж и связь сохраняются одним `SaveChanges`: при ошибке ничего не записано.
- Frontend: `CreateCharacterForm` получает необязательный `campaignId`, грузит `api.reference(system, { campaignId })`
  и передаёт `campaignId` в `createCharacter`; после создания возвращает в кампанию. Кнопка «Создать персонажа»
  в `CampaignDetailView` открывает эту форму.

Тесты: член без персонажа создаёт героя на кастомных архетипе и карьере мастера внутри кампании — 200,
персонаж в кампании; тот же запрос без `CampaignId` — 400; не член с `CampaignId` — 400; контент мастера
из другой его кампании — 400.

---

## Этап 3. Личная библиотека

- `CampaignCustomContent.GetOrCreatePackIdAsync` принимает `Guid? campaignId`. При `null` — без проверки мастера
  создаёт/находит личный набор пользователя «Моя библиотека (system)» с маркером в `Description`
  (так же, как сейчас помечается набор кампании). Обработчики создания меняются только типом `CampaignId` в команде.
- Маршруты: новая группа `/api/custom` с шестью `POST` без кампании. `PUT`/`DELETE` уже не используют
  `campaignId` ([CustomContentEndpoints.cs:39–99](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs)) —
  вынести их регистрацию в функцию и повесить на обе группы.
- Подключение к кампании — существующий `PUT /api/campaigns/{id}/homebrew-packs/{packId}`
  (`SetCampaignHomebrewPackHandler` проверяет роль мастера; первое подключение по ID — только своего
  набора, управление ранее разрешённым shared-набором — по существующей связи кампании).
- `POST /api/campaigns/{id}/homebrew-packs/shared/{token}/import` подключает исходный shared-набор
  к кампании без копирования. `GET /api/campaigns/{id}/homebrew-packs` позволяет мастеру видеть
  и включать/отключать все подключённые наборы, включая принадлежащие игрокам.
- Frontend: `CustomTab` с необязательным `campaignId`; раздел «Моя библиотека» — `CustomTab` без кампании.

Тесты: пользователь без кампаний создаёт кастомный архетип и персонажа на нём вне кампании; другой
пользователь этот контент не видит.

---

## Этап 4. Изоляция контекста кампании

- `HomebrewVisibility.GetVisiblePackIdsAsync`: если контекст — кампания (`campaignId` задан или персонаж
  связан с кампаниями), личные наборы пользователя не добавляются; видны только включённые подключения кампании.
  Так выполняется подтверждённое требование.
- Определения без набора (`HomebrewPackId == null`) правило `IsVisibleCustom` по-прежнему отдаёт владельцу.
  После этапа 3 новые такие не появляются. **Assumption:** legacy-записей без набора нет — перед этапом
  проверить на проде; если есть, перенести их в личный набор миграцией данных.
- **Изменение поведения:** у персонажей в кампаниях личные наборы игрока перестанут быть доступны для новых
  покупок. Перед выкаткой предупредить игроков: нужные исходные наборы мастер подключает к кампании
  по shared-ссылке; повторная покупка навыков/талантов и перенос рангов на копии не нужны.
- Сохранность листа: [SheetBuilder.cs:111–116](../backend/src/GenesysForge.Application/Common/SheetBuilder.cs)
  фильтрует навыки по видимости, и навык с купленными рангами пропадает с листа, если его набор отключён.
  Добавить `ownedSkillIds.Contains(s.Id) ||` в условие видимости. Таланты и предметы лист по видимости
  не фильтрует. Это уже существующий баг — фикс можно выкатить сразу.

Тесты: персонаж в кампании не видит личный набор владельца, видит после подключения мастером; ранги
навыка остаются на листе после отключения набора.

---

## Этап 5. Дата правки и журнал изменений кастомного контента

Следует из решения 07.10.2026: автор подключённого набора может менять механику после разрешения мастера
(тир и бонусы таланта, характеристики архетипа и т. д.), правка сразу действует на персонажей кампании.
Мастер и участники должны видеть, что и когда изменилось.

### Модель

```csharp
// GenesysForge.Domain/Entities/CustomContentChange.cs
public class CustomContentChange
{
    public Guid Id { get; set; }
    public Guid? HomebrewPackId { get; set; }            // null только у legacy-записей без набора
    public required string DefinitionType { get; set; }  // skill | talent | item | heroicAbility | archetype | career
    public Guid DefinitionId { get; set; }               // без FK: запись журнала переживает удаление определения
    public required string DefinitionName { get; set; } // имя на момент правки
    public Guid UserId { get; set; }                     // автор правки (= владелец определения)
    public CustomContentChangeAction Action { get; set; } // Created, Updated, Deleted
    public string ChangesJson { get; set; } = "";        // для Updated: [{ "field": "tier", "from": "1", "to": "2" }]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- Индексы `(HomebrewPackId, CreatedAt)` и `(DefinitionId, CreatedAt)`.
- Миграция `AddCustomContentChanges` — только новая таблица, без backfill: для существующих записей дата
  правки неизвестна, UI показывает её отсутствие.
- Отдельные `UpdatedAt` в шести таблицах определений не вводим: дата последней правки = последняя запись журнала.

### Запись

`Common/CustomContentAudit.cs` по образцу [CharacterAudit](../backend/src/GenesysForge.Application/Common/CharacterAudit.cs):
добавляет запись в контекст, фиксация — в общем `SaveChangesAsync`, атомарно с самой правкой.

- Create-обработчики (6): `Created` после создания определения.
- Update-обработчики (6): `before = def.ToDto()` до присваивания полей, `after = def.ToDto()` после.
  Все шесть уже загружают вложенные данные и возвращают `ToDto()`, поэтому diff получается сравнением
  JSON-свойств без кода под каждый тип. Вложенные списки (стартовые навыки, снаряжение, способности)
  сравниваются целиком. Пустой diff — записи нет: повторное сохранение формы журнал не засоряет.
- Delete-обработчики (6): `Deleted` с именем определения.
- Импорт набора и миграции данных журнал не пишут: импорт создаёт новый независимый набор.

```csharp
public static void Updated(IAppDbContext db, string type, Guid id, Guid? packId, string name,
    Guid userId, object before, object after)
{
    var a = JsonSerializer.SerializeToElement(before, JsonOptions);
    var b = JsonSerializer.SerializeToElement(after, JsonOptions);
    var changes = b.EnumerateObject()
        .Where(p => p.Name != "id")
        .Select(p => new
        {
            field = p.Name,
            from = a.TryGetProperty(p.Name, out var old) ? old.GetRawText() : null,
            to = p.Value.GetRawText(),
        })
        .Where(x => x.from != x.to)
        .ToList();
    if (changes.Count == 0) return;
    db.CustomContentChanges.Add(new CustomContentChange
    {
        Id = Guid.NewGuid(), HomebrewPackId = packId, DefinitionType = type, DefinitionId = id,
        DefinitionName = name, UserId = userId, Action = CustomContentChangeAction.Updated,
        ChangesJson = JsonSerializer.Serialize(changes, JsonOptions),
    });
}
```

### Чтение

- Справочник: `ReferenceResponse.CustomLastEditedAt` — `Dictionary<Guid, DateTime>` только для кастомных
  записей ответа, одним grouped-запросом по журналу (по образцу `EditableCustomIds`).
- `CampaignHomebrewPackDto` + `LastChangedAt` (последняя запись журнала набора) и `ConnectedAt`
  (`HomebrewPackCampaign.UpdatedAt`). `ChangedAfterConnection` вычисляет сервер:
  владелец набора не является мастером этой кампании и `LastChangedAt > ConnectedAt`.
  UI показывает метку «изменён после подключения» по этому флагу, одинаково для мастера и участников.
- `GET /api/homebrew-packs/{packId}/changes?campaignId=&take=` — журнал набора, новые сверху, `take` до 200.
  Доступ: владелец набора; иначе нужен `campaignId`, `GetAccessibleAsync` (мастер или участник)
  и подключение набора к этой кампании. Набор без подключения к кампании вызывающего — отказ.

### Frontend

- `CustomTab`: у каждой кастомной записи — «изменено DD.MM.YYYY», если дата есть.
  Если даты нет, подпись и разделитель не выводятся; пояснение про старые записи/импорт остаётся в истории.
- Панель «Наборы кампании»: дата последней правки, пометка «изменён после подключения», кнопка «История» —
  дата, автор, действие, запись и поля «было → стало». Названия полей — словарь меток на клиенте,
  неизвестное поле показывается как есть.
- «Моя библиотека»: та же «История» у своих наборов.
- Новые методы — в `api/client.ts`, с тестом клиента.

### Тесты

- Правка тира таланта игрока → запись `Updated` с `tier` 1 → 2. Мастер и участник кампании, где набор
  подключён, видят журнал; посторонний и мастер другой кампании — отказ.
- Сохранение без изменений не создаёт запись. Удаление — запись `Deleted` с именем.
- Справочник отдаёт дату правки изменённой записи; после правки подключённого набора `LastChangedAt > ConnectedAt`.
- Создание/правка контента мастера кампании не устанавливает `ChangedAfterConnection`, даже когда
  событие новее связи; правка исходного набора игрока после подключения устанавливает флаг.

---

## Сознательно не делаем до реального запроса

- Версии наборов и снимки определений в листе: персонажи ссылаются на определения по ID, а используемые
  определения удалить нельзя ([DeleteCustomTalentHandler.cs:15](../backend/src/GenesysForge.Application/Features/CustomContent/DeleteCustomTalentHandler.cs) и аналоги).
- Отдельную очередь предложений набора игроком мастеру: достаточно shared-ссылки и прямого
  подключения исходного набора мастером. Обычный импорт JSON в личную библиотеку остаётся
  копированием; служебные маркеры Personal custom:/Campaign custom: при нём не переносятся.
- `Character.RulesCampaignId`, отдельный сервис `ContentAccessPolicy`, стабильные ключи вместо ссылок
  по имени, отзыв SignalR-подписки при исключении.

## Риски

- Снятие последнего персонажа больше не выводит игрока из кампании (этап 1).
- Этап 4 закрывает игрокам доступ к личному кастому в кампаниях до подключения мастером.
- Автор подключённого набора может менять механику после разрешения мастера. Это принято сознательно:
  мастер узнаёт о правках по дате и журналу (этап 5), а не по запрету.
- XP, dice pool, purchase/refund и формулы не меняются.

**Copyright:** seed и справочники не меняются; оригинальные тексты книг не добавляются.
