using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Account;
using GenesysForge.Application.Features.Auth;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Application.Features.ContentPacks;
using GenesysForge.Application.Features.CustomContent;
using GenesysForge.Application.Features.Encounters;
using GenesysForge.Application.Features.GameTable;
using GenesysForge.Application.Features.HomebrewPacks;
using GenesysForge.Application.Features.Notes;
using GenesysForge.Application.Features.Npcs;
using GenesysForge.Application.Features.Reference;
using GenesysForge.Application.Features.Search;
using GenesysForge.Application.Features.Spells;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<ICommandHandler<RegisterUserCommand, AuthResponse>, RegisterUserHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginHandler>();
        services.AddScoped<ICommandHandler<RequestPasswordResetCommand, Unit>, RequestPasswordResetHandler>();
        services.AddScoped<ICommandHandler<ConfirmPasswordResetCommand, Unit>, ConfirmPasswordResetHandler>();
        services.AddScoped<ICommandHandler<GoogleSignInCommand, AuthResponse>, GoogleSignInHandler>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Account / профиль
        services.AddScoped<IQueryHandler<GetAccountQuery, AccountDto>, GetAccountHandler>();
        services.AddScoped<ICommandHandler<UpdateAccountCommand, AccountDto>, UpdateAccountHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, Unit>, ChangePasswordHandler>();
        services.AddScoped<ICommandHandler<UploadAvatarCommand, AccountDto>, UploadAvatarHandler>();

        // Reference
        services.AddScoped<IQueryHandler<GetReferenceQuery, ReferenceResponse>, GetReferenceHandler>();
        services.AddScoped<IQueryHandler<GetRulesQuery, RulesResponse>, GetRulesHandler>();

        // Search
        services.AddScoped<IQueryHandler<GlobalSearchQuery, SearchResponse>, GlobalSearchHandler>();

        // Characters
        services.AddScoped<ICommandHandler<UploadCharacterPortraitCommand, string>, UploadCharacterPortraitHandler>();
        services.AddScoped<IQueryHandler<GetCharactersQuery, List<CharacterListItemDto>>, GetCharactersHandler>();
        services.AddScoped<IQueryHandler<GetCharacterSheetQuery, CharacterSheetDto>, GetCharacterSheetHandler>();
        services.AddScoped<IQueryHandler<GetSharedCharacterSheetQuery, CharacterSheetDto>, GetSharedCharacterSheetHandler>();
        services.AddScoped<IQueryHandler<ExportCharacterQuery, CharacterExportDto>, ExportCharacterHandler>();
        services.AddScoped<IQueryHandler<PreviewImportCharacterQuery, ImportPreviewDto>, PreviewImportCharacterHandler>();
        services.AddScoped<ICommandHandler<ImportCharacterCommand, ImportCharacterResult>, ImportCharacterHandler>();
        services.AddScoped<ICommandHandler<DuplicateCharacterCommand, Guid>, DuplicateCharacterHandler>();
        services.AddScoped<ICommandHandler<CreateCharacterShareCommand, CharacterShareResponse>, CreateCharacterShareHandler>();
        services.AddScoped<ICommandHandler<RevokeCharacterSharesCommand, Unit>, RevokeCharacterSharesHandler>();
        services.AddScoped<ICommandHandler<CreateCharacterCommand, Guid>, CreateCharacterHandler>();
        services.AddScoped<ICommandHandler<UpdateCharacterCommand, Unit>, UpdateCharacterHandler>();
        services.AddScoped<ICommandHandler<DeleteCharacterCommand, Unit>, DeleteCharacterHandler>();
        services.AddScoped<ICommandHandler<CompleteCreationCommand, Unit>, CompleteCreationHandler>();
        services.AddScoped<ICommandHandler<BuyCharacteristicCommand, Unit>, BuyCharacteristicHandler>();
        services.AddScoped<ICommandHandler<BuySkillRankCommand, Unit>, BuySkillRankHandler>();
        services.AddScoped<ICommandHandler<BuyTalentCommand, Unit>, BuyTalentHandler>();
        services.AddScoped<ICommandHandler<RefundCharacteristicCommand, Unit>, RefundCharacteristicHandler>();
        services.AddScoped<ICommandHandler<RefundSkillRankCommand, Unit>, RefundSkillRankHandler>();
        services.AddScoped<ICommandHandler<RefundTalentCommand, Unit>, RefundTalentHandler>();
        services.AddScoped<ICommandHandler<SetHeroicAbilityCommand, Unit>, SetHeroicAbilityHandler>();
        services.AddScoped<ICommandHandler<SetHeroicUpgradeRankCommand, Unit>, SetHeroicUpgradeRankHandler>();
        services.AddScoped<ICommandHandler<AddItemCommand, Guid>, AddItemHandler>();
        services.AddScoped<ICommandHandler<UpdateItemCommand, Unit>, UpdateItemHandler>();
        services.AddScoped<ICommandHandler<RemoveItemCommand, Unit>, RemoveItemHandler>();
        services.AddScoped<ICommandHandler<SellItemCommand, Unit>, SellItemHandler>();
        services.AddScoped<ICommandHandler<AddCriticalInjuryCommand, Guid>, AddCriticalInjuryHandler>();
        services.AddScoped<ICommandHandler<RemoveCriticalInjuryCommand, Unit>, RemoveCriticalInjuryHandler>();
        services.AddScoped<IQueryHandler<GetCharacterAuditQuery, IReadOnlyList<CharacterAuditEntryDto>>, GetCharacterAuditHandler>();
        services.AddScoped<ICommandHandler<AwardXpCommand, Unit>, AwardXpHandler>();
        services.AddScoped<ICommandHandler<ActivateCharacterAbilityCommand, ActivateCharacterAbilityResult>,
            ActivateCharacterAbilityHandler>();

        // Custom content
        services.AddScoped<ICommandHandler<CreateCustomSkillCommand, SkillDefDto>, CreateCustomSkillHandler>();
        services.AddScoped<ICommandHandler<CreateCustomTalentCommand, TalentDefDto>, CreateCustomTalentHandler>();
        services.AddScoped<ICommandHandler<CreateCustomItemCommand, ItemDefDto>, CreateCustomItemHandler>();
        services.AddScoped<ICommandHandler<CreateCustomHeroicAbilityCommand, HeroicAbilityDto>, CreateCustomHeroicAbilityHandler>();
        services.AddScoped<ICommandHandler<CreateCustomArchetypeCommand, ArchetypeDto>, CreateCustomArchetypeHandler>();
        services.AddScoped<ICommandHandler<CreateCustomCareerCommand, CareerDto>, CreateCustomCareerHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomSkillCommand, SkillDefDto>, UpdateCustomSkillHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomTalentCommand, TalentDefDto>, UpdateCustomTalentHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomItemCommand, ItemDefDto>, UpdateCustomItemHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomHeroicAbilityCommand, HeroicAbilityDto>, UpdateCustomHeroicAbilityHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomArchetypeCommand, ArchetypeDto>, UpdateCustomArchetypeHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomCareerCommand, CareerDto>, UpdateCustomCareerHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomSkillCommand, Unit>, DeleteCustomSkillHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomTalentCommand, Unit>, DeleteCustomTalentHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomItemCommand, Unit>, DeleteCustomItemHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomHeroicAbilityCommand, Unit>, DeleteCustomHeroicAbilityHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomArchetypeCommand, Unit>, DeleteCustomArchetypeHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomCareerCommand, Unit>, DeleteCustomCareerHandler>();

        // Notes
        services.AddScoped<IQueryHandler<GetCharacterNotesQuery, List<CharacterNoteDto>>, GetCharacterNotesHandler>();
        services.AddScoped<ICommandHandler<CreateCharacterNoteCommand, CharacterNoteDto>, CreateCharacterNoteHandler>();
        services.AddScoped<ICommandHandler<UpdateCharacterNoteCommand, CharacterNoteDto>, UpdateCharacterNoteHandler>();
        services.AddScoped<ICommandHandler<DeleteCharacterNoteCommand, Unit>, DeleteCharacterNoteHandler>();

        // Campaigns
        services.AddScoped<IQueryHandler<GetCampaignsQuery, List<CampaignListItemDto>>, GetCampaignsHandler>();
        services.AddScoped<IQueryHandler<GetCampaignQuery, CampaignDetailDto>, GetCampaignHandler>();
        services.AddScoped<IQueryHandler<GetCampaignMemberSheetQuery, CharacterSheetDto>, GetCampaignMemberSheetHandler>();
        services.AddScoped<ICommandHandler<CreateCampaignCommand, CampaignDetailDto>, CreateCampaignHandler>();
        services.AddScoped<ICommandHandler<JoinCampaignCommand, CampaignDetailDto>, JoinCampaignHandler>();
        services.AddScoped<ICommandHandler<RemoveCampaignCharacterCommand, Unit>, RemoveCampaignCharacterHandler>();
        services.AddScoped<ICommandHandler<CreateCampaignNoteCommand, CampaignNoteDto>, CreateCampaignNoteHandler>();
        services.AddScoped<ICommandHandler<UpdateCampaignNoteCommand, CampaignNoteDto>, UpdateCampaignNoteHandler>();
        services.AddScoped<ICommandHandler<DeleteCampaignNoteCommand, Unit>, DeleteCampaignNoteHandler>();

        // Spells
        services.AddScoped<IQueryHandler<GetSpellsQuery, List<SpellDto>>, GetSpellsHandler>();

        // NPCs / Bestiary
        services.AddScoped<IQueryHandler<GetNpcsQuery, List<NpcListItemDto>>, GetNpcsHandler>();
        services.AddScoped<IQueryHandler<GetNpcQuery, NpcDetailDto>, GetNpcHandler>();
        services.AddScoped<ICommandHandler<CreateNpcCommand, NpcDetailDto>, CreateNpcHandler>();
        services.AddScoped<ICommandHandler<UpdateNpcCommand, NpcDetailDto>, UpdateNpcHandler>();
        services.AddScoped<ICommandHandler<DeleteNpcCommand, Unit>, DeleteNpcHandler>();
        services.AddScoped<ICommandHandler<DuplicateNpcCommand, NpcDetailDto>, DuplicateNpcHandler>();
        services.AddScoped<ICommandHandler<QuickDraftNpcCommand, NpcDetailDto>, QuickDraftNpcHandler>();
        services.AddScoped<IQueryHandler<PreviewQuickDraftNpcQuery, NpcDetailDto>, PreviewQuickDraftNpcHandler>();
        services.AddScoped<ICommandHandler<ApplyNpcTemplateCommand, NpcDetailDto>, ApplyNpcTemplateHandler>();

        // Game Table / GM Cockpit
        services.AddScoped<IQueryHandler<GetSessionQuery, GameSessionDto?>, GetSessionHandler>();
        services.AddScoped<ICommandHandler<CreateSessionCommand, GameSessionDto>, CreateSessionHandler>();
        services.AddScoped<ICommandHandler<UpdateSessionCommand, GameSessionDto>, UpdateSessionHandler>();
        services.AddScoped<ICommandHandler<ResetSessionCommand, GameSessionDto>, ResetSessionHandler>();
        services.AddScoped<ICommandHandler<EndSessionCommand, Unit>, EndSessionHandler>();
        services.AddScoped<ICommandHandler<NextTurnCommand, GameSessionDto>, NextTurnHandler>();
        services.AddScoped<ICommandHandler<AddParticipantCommand, GameSessionDto>, AddParticipantHandler>();
        services.AddScoped<ICommandHandler<UpdateParticipantCommand, GameSessionDto>, UpdateParticipantHandler>();
        services.AddScoped<ICommandHandler<ActivateAbilityCommand, ActivateAbilityResult>, ActivateAbilityHandler>();
        services.AddScoped<ICommandHandler<RemoveParticipantCommand, Unit>, RemoveParticipantHandler>();
        services.AddScoped<ICommandHandler<AddSlotCommand, GameSessionDto>, AddSlotHandler>();
        services.AddScoped<ICommandHandler<UpdateSlotCommand, GameSessionDto>, UpdateSlotHandler>();
        services.AddScoped<ICommandHandler<RemoveSlotCommand, Unit>, RemoveSlotHandler>();
        services.AddScoped<ICommandHandler<CreateRollCommand, RollLogEntryDto>, CreateRollHandler>();
        services.AddScoped<IQueryHandler<GetRollsQuery, IReadOnlyList<RollLogEntryDto>>, GetRollsHandler>();

        // Encounter Builder
        services.AddScoped<IQueryHandler<GetEncountersQuery, List<EncounterListItemDto>>, GetEncountersHandler>();
        services.AddScoped<IQueryHandler<GetEncounterQuery, EncounterDetailDto>, GetEncounterHandler>();
        services.AddScoped<ICommandHandler<CreateEncounterCommand, EncounterDetailDto>, CreateEncounterHandler>();
        services.AddScoped<ICommandHandler<UpdateEncounterCommand, EncounterDetailDto>, UpdateEncounterHandler>();
        services.AddScoped<ICommandHandler<DeleteEncounterCommand, Unit>, DeleteEncounterHandler>();
        services.AddScoped<ICommandHandler<AddEncounterParticipantCommand, EncounterDetailDto>, AddEncounterParticipantHandler>();
        services.AddScoped<ICommandHandler<UpdateEncounterParticipantCommand, EncounterDetailDto>, UpdateEncounterParticipantHandler>();
        services.AddScoped<ICommandHandler<RemoveEncounterParticipantCommand, Unit>, RemoveEncounterParticipantHandler>();
        services.AddScoped<ICommandHandler<AddCampaignCharactersCommand, EncounterDetailDto>, AddCampaignCharactersHandler>();
        services.AddScoped<ICommandHandler<SendToGameTableCommand, GameSessionDto>, SendToGameTableHandler>();

        // Campaign Handbook / Content Packs
        services.AddScoped<IQueryHandler<GetContentPacksQuery, List<ContentPackListItemDto>>, GetContentPacksHandler>();
        services.AddScoped<IQueryHandler<GetContentPackQuery, ContentPackDetailDto>, GetContentPackHandler>();
        services.AddScoped<ICommandHandler<CreateContentPackCommand, ContentPackDetailDto>, CreateContentPackHandler>();
        services.AddScoped<ICommandHandler<UpdateContentPackCommand, ContentPackDetailDto>, UpdateContentPackHandler>();
        services.AddScoped<ICommandHandler<DeleteContentPackCommand, Unit>, DeleteContentPackHandler>();
        services.AddScoped<ICommandHandler<AddContentPackEntryCommand, ContentPackDetailDto>, AddContentPackEntryHandler>();
        services.AddScoped<ICommandHandler<UpdateContentPackEntryCommand, ContentPackDetailDto>, UpdateContentPackEntryHandler>();
        services.AddScoped<ICommandHandler<RemoveContentPackEntryCommand, Unit>, RemoveContentPackEntryHandler>();

        // Homebrew JSON packs
        services.AddScoped<IQueryHandler<GetHomebrewPacksQuery, List<HomebrewPackListItemDto>>, GetHomebrewPacksHandler>();
        services.AddScoped<IQueryHandler<ExportHomebrewPackQuery, HomebrewPackExportDto>, ExportHomebrewPackHandler>();
        services.AddScoped<ICommandHandler<ImportHomebrewPackCommand, HomebrewPackImportResult>, ImportHomebrewPackHandler>();
        services.AddScoped<ICommandHandler<ShareHomebrewPackCommand, HomebrewPackShareDto>, ShareHomebrewPackHandler>();
        services.AddScoped<ICommandHandler<ImportSharedHomebrewPackCommand, HomebrewPackImportResult>, ImportSharedHomebrewPackHandler>();
        services.AddScoped<ICommandHandler<SetHomebrewPackDefaultCommand, Unit>, SetHomebrewPackDefaultHandler>();
        services.AddScoped<ICommandHandler<SetCharacterHomebrewPackCommand, Unit>, SetCharacterHomebrewPackHandler>();
        services.AddScoped<ICommandHandler<SetCampaignHomebrewPackCommand, Unit>, SetCampaignHomebrewPackHandler>();

        return services;
    }
}
