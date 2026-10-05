# Инвентаризация API для тестирования публичной версии

**Срез:** `origin/master`, `4da4636`, 05.10.2026. Статическая инвентаризация, не журнал выполненных проверок.

Найдено 167 объявлений HTTP-маршрутов в `Endpoints/*.cs`; дополнительно health в `Program.cs` и SignalR.

Основной план: [public-version-test-plan.md](public-version-test-plan.md). У каждого маршрута следует проверить применимые сценарии API-01…API-12 из плана; семейство указывает специализированные сценарии. Наличие маршрута не доказывает тестовое покрытие.

Таблица показывает legacy `/api/*`. Middleware в `Program.cs` также принимает `/api/v1/*`: каждый маршрут прогнать через оба префикса, включая ошибки и авторизацию. Ограничения `:guid` проверяются неверным GUID и отсутствующим объектом. Точный статус каждого use case сверять с endpoint/handler, не заменять все ошибки на 403/404.

| Источник | Метод | Legacy route | Семейство тестов |
|---|---|---|---|
| [AccountEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AccountEndpoints.cs) (строка 15) | GET | `/api/account/` | PROF |
| [AccountEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AccountEndpoints.cs) (строка 19) | PATCH | `/api/account/` | PROF |
| [AccountEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AccountEndpoints.cs) (строка 25) | POST | `/api/account/avatar` | PROF |
| [AccountEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AccountEndpoints.cs) (строка 34) | POST | `/api/account/change-password` | PROF |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 16) | POST | `/api/auth/register` | AUTH |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 27) | POST | `/api/auth/login` | AUTH |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 38) | POST | `/api/auth/password-reset/request` | AUTH |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 45) | POST | `/api/auth/password-reset/confirm` | AUTH |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 53) | POST | `/api/auth/google` | AUTH |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 63) | GET | `/api/auth/providers` | AUTH |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 68) | POST | `/api/auth/refresh` | AUTH |
| [AuthEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/AuthEndpoints.cs) (строка 78) | POST | `/api/auth/logout` | AUTH |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 28) | GET | `/api/campaigns/` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 32) | POST | `/api/campaigns/` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 39) | GET | `/api/campaigns/{id:guid}` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 44) | GET | `/api/campaigns/{id:guid}/characters/{characterId:guid}/sheet` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 49) | GET | `/api/campaigns/{id:guid}/characters/{characterId:guid}/audit` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 56) | POST | `/api/campaigns/join` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 65) | DELETE | `/api/campaigns/{id:guid}/characters/{characterId:guid}` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 73) | POST | `/api/campaigns/{id:guid}/notes` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 77) | PUT | `/api/campaigns/{id:guid}/notes/{noteId:guid}` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 81) | DELETE | `/api/campaigns/{id:guid}/notes/{noteId:guid}` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 89) | GET | `/api/campaigns/{id:guid}/chronicle` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 94) | POST | `/api/campaigns/{id:guid}/chronicle/chapters` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 102) | POST | `/api/campaigns/{id:guid}/chronicle/images` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 111) | PUT | `/api/campaigns/{id:guid}/chronicle/chapters/{chapterId:guid}` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 118) | DELETE | `/api/campaigns/{id:guid}/chronicle/chapters/{chapterId:guid}` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 126) | GET | `/api/campaigns/{id:guid}/chronicle/chapters/{chapterId:guid}/history` | CAMP/CHRON |
| [CampaignEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CampaignEndpoints.cs) (строка 133) | POST | `/api/campaigns/{id:guid}/chronicle/chapters/{chapterId:guid}/restore/{revisionId:guid}` | CAMP/CHRON |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 14) | GET | `/api/share/{token}` | PRINT |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 23) | GET | `/api/characters/` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 27) | POST | `/api/characters/` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 34) | GET | `/api/characters/{id:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 41) | GET | `/api/characters/{id:guid}/slices` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 46) | POST | `/api/characters/{id:guid}/duplicate` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 55) | POST | `/api/characters/{id:guid}/portrait` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 63) | POST | `/api/characters/{id:guid}/share` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 67) | DELETE | `/api/characters/{id:guid}/share` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 75) | GET | `/api/characters/{id:guid}/export` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 80) | POST | `/api/characters/import` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 88) | POST | `/api/characters/import/preview` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 92) | PATCH | `/api/characters/{id:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 99) | DELETE | `/api/characters/{id:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 106) | POST | `/api/characters/{id:guid}/complete-creation` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 114) | GET | `/api/characters/{id:guid}/audit` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 119) | POST | `/api/characters/{id:guid}/audit/{entryId:guid}/undo` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 127) | POST | `/api/characters/{id:guid}/xp-awards` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 134) | POST | `/api/characters/{id:guid}/activate-ability` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 139) | POST | `/api/characters/{id:guid}/characteristics/{type}/buy` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 149) | POST | `/api/characters/{id:guid}/characteristics/{type}/refund` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 158) | POST | `/api/characters/{id:guid}/skills/{skillDefId:guid}/refund-rank` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 165) | POST | `/api/characters/{id:guid}/talents/refund` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 172) | POST | `/api/characters/{id:guid}/skills/{skillDefId:guid}/buy-rank` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 179) | POST | `/api/characters/{id:guid}/talents/buy` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 187) | PUT | `/api/characters/{id:guid}/items/{itemId:guid}/thrown` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 195) | PUT | `/api/characters/{id:guid}/heroic-ability` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 202) | PUT | `/api/characters/{id:guid}/heroic-identity` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 209) | POST | `/api/characters/{id:guid}/heroic-identity/roll-origin` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 216) | PUT | `/api/characters/{id:guid}/heroic-configuration` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 223) | POST | `/api/characters/{id:guid}/heroic-configuration/signature-weapon` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 233) | POST | `/api/characters/{id:guid}/heroic-configuration/signature-weapon/upgrades` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 241) | PUT | `/api/characters/{id:guid}/heroic-upgrade` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 248) | PUT | `/api/characters/{id:guid}/heroic-upgrades` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 255) | POST | `/api/characters/{id:guid}/items` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 262) | POST | `/api/characters/{id:guid}/services` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 273) | POST | `/api/characters/{id:guid}/mounts` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 280) | PATCH | `/api/characters/{id:guid}/mounts/{mountId:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 288) | POST | `/api/characters/{id:guid}/mounts/{mountId:guid}/sell` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 296) | DELETE | `/api/characters/{id:guid}/mounts/{mountId:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 306) | GET | `/api/characters/{id:guid}/crafting` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 312) | POST | `/api/characters/{id:guid}/crafting/preview` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 317) | POST | `/api/characters/{id:guid}/crafting` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 326) | POST | `/api/characters/{id:guid}/crafting/{projectId:guid}/resolve` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 332) | DELETE | `/api/characters/{id:guid}/crafting/{projectId:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 342) | PATCH | `/api/characters/{id:guid}/items/{itemId:guid}/location` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 350) | PATCH | `/api/characters/{id:guid}/items/{itemId:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 357) | POST | `/api/characters/{id:guid}/items/{itemId:guid}/sell` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 364) | DELETE | `/api/characters/{id:guid}/items/{itemId:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 373) | POST | `/api/characters/{id:guid}/attachments` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 380) | POST | `/api/characters/{id:guid}/attachments/install` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 387) | POST | `/api/characters/{id:guid}/attachments/{attachmentId:guid}/detach` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 395) | DELETE | `/api/characters/{id:guid}/attachments/{attachmentId:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 404) | PUT | `/api/characters/{id:guid}/items/{itemId:guid}/damage-state` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 412) | POST | `/api/characters/{id:guid}/items/{itemId:guid}/repair` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 421) | PUT | `/api/characters/{id:guid}/items/{itemId:guid}/implement` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 429) | PUT | `/api/characters/{id:guid}/items/{itemId:guid}/lesser-rune` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 439) | PUT | `/api/characters/{id:guid}/attachments/{attachmentId:guid}/damage-state` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 448) | POST | `/api/characters/{id:guid}/attachments/{attachmentId:guid}/repair` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 458) | POST | `/api/characters/{id:guid}/critical-injuries` | CHAR/XP/TAL/HERO/INV/IMP |
| [CharacterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CharacterEndpoints.cs) (строка 465) | DELETE | `/api/characters/{id:guid}/critical-injuries/{injuryId:guid}` | CHAR/XP/TAL/HERO/INV/IMP |
| [CombatEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CombatEndpoints.cs) (строка 18) | POST | `/api/combat/resolve-attack` | COMBAT |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 14) | GET | `/api/campaigns/{campaignId:guid}/content-packs/` | PACK |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 18) | POST | `/api/campaigns/{campaignId:guid}/content-packs/` | PACK |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 27) | GET | `/api/content-packs/{id:guid}` | PACK |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 31) | PATCH | `/api/content-packs/{id:guid}` | PACK |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 35) | DELETE | `/api/content-packs/{id:guid}` | PACK |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 43) | POST | `/api/content-packs/{id:guid}/entries` | PACK |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 47) | PUT | `/api/content-packs/{id:guid}/entries/{entryId:guid}` | PACK |
| [ContentPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ContentPackEndpoints.cs) (строка 52) | DELETE | `/api/content-packs/{id:guid}/entries/{entryId:guid}` | PACK |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 14) | POST | `/api/campaigns/{campaignId:guid}/custom/skills` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 18) | POST | `/api/campaigns/{campaignId:guid}/custom/talents` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 22) | POST | `/api/campaigns/{campaignId:guid}/custom/items` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 26) | POST | `/api/campaigns/{campaignId:guid}/custom/heroic-abilities` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 30) | POST | `/api/campaigns/{campaignId:guid}/custom/archetypes` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 34) | POST | `/api/campaigns/{campaignId:guid}/custom/careers` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 39) | PUT | `/api/campaigns/{campaignId:guid}/custom/skills/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 43) | PUT | `/api/campaigns/{campaignId:guid}/custom/talents/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 47) | PUT | `/api/campaigns/{campaignId:guid}/custom/items/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 51) | PUT | `/api/campaigns/{campaignId:guid}/custom/heroic-abilities/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 55) | PUT | `/api/campaigns/{campaignId:guid}/custom/archetypes/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 59) | PUT | `/api/campaigns/{campaignId:guid}/custom/careers/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 64) | DELETE | `/api/campaigns/{campaignId:guid}/custom/skills/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 71) | DELETE | `/api/campaigns/{campaignId:guid}/custom/talents/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 78) | DELETE | `/api/campaigns/{campaignId:guid}/custom/items/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 85) | DELETE | `/api/campaigns/{campaignId:guid}/custom/heroic-abilities/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 92) | DELETE | `/api/campaigns/{campaignId:guid}/custom/archetypes/{id:guid}` | CUSTOM |
| [CustomContentEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/CustomContentEndpoints.cs) (строка 99) | DELETE | `/api/campaigns/{campaignId:guid}/custom/careers/{id:guid}` | CUSTOM |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 15) | GET | `/api/campaigns/{campaignId:guid}/encounters/` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 21) | POST | `/api/campaigns/{campaignId:guid}/encounters/` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 30) | GET | `/api/encounters/{id:guid}` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 34) | PUT | `/api/encounters/{id:guid}` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 38) | DELETE | `/api/encounters/{id:guid}` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 46) | POST | `/api/encounters/{id:guid}/participants` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 51) | POST | `/api/encounters/{id:guid}/participants/characters` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 56) | PATCH | `/api/encounters/{id:guid}/participants/{participantId:guid}` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 62) | DELETE | `/api/encounters/{id:guid}/participants/{participantId:guid}` | ENC |
| [EncounterEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/EncounterEndpoints.cs) (строка 71) | POST | `/api/encounters/{id:guid}/send-to-table` | ENC |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 28) | GET | `/api/campaigns/{campaignId:guid}/session/` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 36) | POST | `/api/campaigns/{campaignId:guid}/session/` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 43) | PATCH | `/api/campaigns/{campaignId:guid}/session/` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 47) | POST | `/api/campaigns/{campaignId:guid}/session/reset` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 51) | POST | `/api/campaigns/{campaignId:guid}/session/next-turn` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 55) | DELETE | `/api/campaigns/{campaignId:guid}/session/` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 63) | POST | `/api/campaigns/{campaignId:guid}/session/participants` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 67) | PATCH | `/api/campaigns/{campaignId:guid}/session/participants/{participantId:guid}` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 73) | POST | `/api/campaigns/{campaignId:guid}/session/participants/{participantId:guid}/activate` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 79) | DELETE | `/api/campaigns/{campaignId:guid}/session/participants/{participantId:guid}` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 87) | POST | `/api/campaigns/{campaignId:guid}/session/slots` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 91) | PATCH | `/api/campaigns/{campaignId:guid}/session/slots/{slotId:guid}` | TABLE/RT |
| [GameTableEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/GameTableEndpoints.cs) (строка 95) | DELETE | `/api/campaigns/{campaignId:guid}/session/slots/{slotId:guid}` | TABLE/RT |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 14) | GET | `/api/homebrew-packs/` | PACK |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 18) | GET | `/api/homebrew-packs/{id:guid}/export` | PACK |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 22) | POST | `/api/homebrew-packs/import` | PACK |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 29) | POST | `/api/homebrew-packs/{id:guid}/share` | PACK |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 33) | POST | `/api/homebrew-packs/shared/{token}/import` | PACK |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 40) | PUT | `/api/homebrew-packs/{id:guid}/default` | PACK |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 47) | PUT | `/api/characters/{characterId:guid}/homebrew-packs/{packId:guid}` | PACK |
| [HomebrewPackEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/HomebrewPackEndpoints.cs) (строка 55) | PUT | `/api/campaigns/{campaignId:guid}/homebrew-packs/{packId:guid}` | PACK |
| [NoteEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NoteEndpoints.cs) (строка 14) | GET | `/api/characters/{id:guid}/notes/` | NOTE |
| [NoteEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NoteEndpoints.cs) (строка 18) | POST | `/api/characters/{id:guid}/notes/` | NOTE |
| [NoteEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NoteEndpoints.cs) (строка 25) | PUT | `/api/characters/{id:guid}/notes/{noteId:guid}` | NOTE |
| [NoteEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NoteEndpoints.cs) (строка 29) | DELETE | `/api/characters/{id:guid}/notes/{noteId:guid}` | NOTE |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 17) | GET | `/api/npcs/` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 28) | GET | `/api/npcs/{id:guid}` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 32) | POST | `/api/npcs/` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 39) | POST | `/api/npcs/quick-draft` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 47) | POST | `/api/npcs/quick-draft/preview` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 51) | POST | `/api/npcs/apply-template` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 55) | POST | `/api/npcs/{id:guid}/duplicate` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 62) | PUT | `/api/npcs/{id:guid}` | NPC |
| [NpcEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/NpcEndpoints.cs) (строка 66) | DELETE | `/api/npcs/{id:guid}` | NPC |
| [ReferenceEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ReferenceEndpoints.cs) (строка 16) | GET | `/api/reference/{system}` | REF/PAR |
| [ReferenceEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/ReferenceEndpoints.cs) (строка 26) | GET | `/api/reference/rules` | REF/PAR |
| [RollEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/RollEndpoints.cs) (строка 14) | GET | `/api/campaigns/{campaignId:guid}/rolls/` | DICE/RT |
| [RollEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/RollEndpoints.cs) (строка 18) | POST | `/api/campaigns/{campaignId:guid}/rolls/` | DICE/RT |
| [SearchEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/SearchEndpoints.cs) (строка 16) | GET | `/api/search/` | REF |
| [SpellEndpoints.cs](../backend/src/GenesysForge.Api/Endpoints/SpellEndpoints.cs) (строка 15) | GET | `/api/spells/{system}` | MAG |

## Дополнительные поверхности

| Поверхность | Проверки |
|---|---|
| `GET /api/health`, `/api/v1/health` | DB ok → 200; DB unavailable → 503; без секретов; DEPLOY-01 |
| `/hubs/campaign` и negotiation | RT-01…RT-12; transport, JWT, членство, isolation, reconnect; не менять `/hubs` на `/api/v1/hubs` |
| `/openapi/v1.json`, `/api/docs` | API-12, DEPLOY-09: валидная схема за reverse proxy, MIME и редиректы |
| `/assets/*`, HTML, `/sw.js`, manifest, robots/sitemap | WEB/PWA/PERF/DEPLOY; это HTTP-поверхности frontend, не бизнес-API |

## Требования к журналу покрытия

- Для каждой строки: тестовый ID, fixture, роль, результат positive/negative/ownership, оба route prefixes, trace/report, дефект или принятое исключение.
- Для операций персонажа дополнительно проверить `X-Return-Slices`, отсутствие лишних round trips и недопустимость запроса чужих срезов.
- Новая конечная точка добавляет строку и тесты в эту матрицу; цель — 100% маршрутов с хотя бы positive и применимыми negative/ownership проверками.
- План не предполагает, что все маршруты подходят для анонимного доступа; публичность сайта не отменяет JWT и ownership.
