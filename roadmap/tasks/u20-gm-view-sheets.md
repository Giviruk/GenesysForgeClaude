# U-20 · GM видит полные листы персонажей игроков

**Статус:** 🚧 In progress · ветка `feature/u20-gm-view-sheets` (от master) · PR: _TBD_
**Roadmap:** [unified-roadmap.md](../unified-roadmap.md) U-20 · Источник: Аудит §6

## Зачем
Мастеру нужно видеть полные листы персонажей игроков (характеристики, навыки, таланты,
инвентарь) для ведения игры. Сейчас лист доступен только владельцу (`GetOwnedAsync` —
owner-only). Нужен read-only GM-доступ к листу участника кампании.

## Что сделано

### Backend
- `CharacterLoader.LoadWithRelationsAsync(characterId, tracking, ct)` — загрузка персонажа со
  всеми связями **без** проверки владельца (вызывающий сам авторизует доступ). `GetOwnedAsync`
  переиспользует её и добавляет owner-check.
- `GetCampaignMemberSheetQuery(GmUserId, CampaignId, CharacterId)` + `GetCampaignMemberSheetHandler`:
  1. `CampaignMapper.GetAsGmAsync` — доступ только GM кампании;
  2. персонаж должен состоять в кампании (`CampaignCharacter`), заодно берём `PlayerUserId`;
  3. лист строится `SheetBuilder.BuildAsync(db, playerUserId, character)` — под владельца-игрока,
     чтобы его кастомный контент (навыки) разрешался корректно.
- Эндпоинт `GET /api/campaigns/{id}/characters/{characterId}/sheet` (GET → realtime-нотификатор
  не дёргается), регистрация хендлера в `DependencyInjection`.

### Frontend
- `api.campaignMemberSheet(campaignId, characterId)` → `CharacterSheet`.
- `CampaignsPage`: в списке участников у GM — кнопка «Лист»; открывает read-only лист участника
  через `PrintPreview` + `CharacterSheetPrint` (переиспользование печатного листа U-04, без правок).

## Тесты
- Api `CampaignTests`: GM открывает лист участника (полный, не заглушка); игрок/посторонний через
  GM-эндпоинт → BadRequest; GM не видит лист персонажа, не состоящего в кампании.
- Front `CampaignsPage.test.tsx`: GM видит «Лист» и открывает read-only лист; игрок кнопку не видит.
- Итог: backend Domain 102 / Api 196, frontend 96 — зелёные; `npm run build` (tsc -b) чист.

## DoD
- [x] GM открывает лист игрока read-only.
- [x] Игрок чужие листы не видит (backend enforce + кнопка только у GM).
