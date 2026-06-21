using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Auth;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Application.Features.ContentPacks;
using GenesysForge.Application.Features.CustomContent;
using GenesysForge.Application.Features.Encounters;
using GenesysForge.Application.Features.GameTable;
using GenesysForge.Application.Features.Notes;
using GenesysForge.Application.Features.Npcs;
using GenesysForge.Application.Features.Reference;
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

        // Reference
        services.AddScoped<IQueryHandler<GetReferenceQuery, ReferenceResponse>, GetReferenceHandler>();

        // Characters
        services.AddScoped<IQueryHandler<GetCharactersQuery, List<CharacterListItemDto>>, GetCharactersHandler>();
        services.AddScoped<IQueryHandler<GetCharacterSheetQuery, CharacterSheetDto>, GetCharacterSheetHandler>();
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

        // Custom content
        services.AddScoped<ICommandHandler<CreateCustomSkillCommand, SkillDefDto>, CreateCustomSkillHandler>();
        services.AddScoped<ICommandHandler<CreateCustomTalentCommand, TalentDefDto>, CreateCustomTalentHandler>();
        services.AddScoped<ICommandHandler<CreateCustomItemCommand, ItemDefDto>, CreateCustomItemHandler>();
        services.AddScoped<ICommandHandler<CreateCustomHeroicAbilityCommand, HeroicAbilityDto>, CreateCustomHeroicAbilityHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomSkillCommand, SkillDefDto>, UpdateCustomSkillHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomTalentCommand, TalentDefDto>, UpdateCustomTalentHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomItemCommand, ItemDefDto>, UpdateCustomItemHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomHeroicAbilityCommand, HeroicAbilityDto>, UpdateCustomHeroicAbilityHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomSkillCommand, Unit>, DeleteCustomSkillHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomTalentCommand, Unit>, DeleteCustomTalentHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomItemCommand, Unit>, DeleteCustomItemHandler>();
        services.AddScoped<ICommandHandler<DeleteCustomHeroicAbilityCommand, Unit>, DeleteCustomHeroicAbilityHandler>();

        // Notes
        services.AddScoped<IQueryHandler<GetCharacterNotesQuery, List<CharacterNoteDto>>, GetCharacterNotesHandler>();
        services.AddScoped<ICommandHandler<CreateCharacterNoteCommand, CharacterNoteDto>, CreateCharacterNoteHandler>();
        services.AddScoped<ICommandHandler<UpdateCharacterNoteCommand, CharacterNoteDto>, UpdateCharacterNoteHandler>();
        services.AddScoped<ICommandHandler<DeleteCharacterNoteCommand, Unit>, DeleteCharacterNoteHandler>();

        // Campaigns
        services.AddScoped<IQueryHandler<GetCampaignsQuery, List<CampaignListItemDto>>, GetCampaignsHandler>();
        services.AddScoped<IQueryHandler<GetCampaignQuery, CampaignDetailDto>, GetCampaignHandler>();
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

        // Game Table / GM Cockpit
        services.AddScoped<IQueryHandler<GetSessionQuery, GameSessionDto?>, GetSessionHandler>();
        services.AddScoped<ICommandHandler<CreateSessionCommand, GameSessionDto>, CreateSessionHandler>();
        services.AddScoped<ICommandHandler<UpdateSessionCommand, GameSessionDto>, UpdateSessionHandler>();
        services.AddScoped<ICommandHandler<ResetSessionCommand, GameSessionDto>, ResetSessionHandler>();
        services.AddScoped<ICommandHandler<EndSessionCommand, Unit>, EndSessionHandler>();
        services.AddScoped<ICommandHandler<NextTurnCommand, GameSessionDto>, NextTurnHandler>();
        services.AddScoped<ICommandHandler<AddParticipantCommand, GameSessionDto>, AddParticipantHandler>();
        services.AddScoped<ICommandHandler<UpdateParticipantCommand, GameSessionDto>, UpdateParticipantHandler>();
        services.AddScoped<ICommandHandler<RemoveParticipantCommand, Unit>, RemoveParticipantHandler>();
        services.AddScoped<ICommandHandler<AddSlotCommand, GameSessionDto>, AddSlotHandler>();
        services.AddScoped<ICommandHandler<UpdateSlotCommand, GameSessionDto>, UpdateSlotHandler>();
        services.AddScoped<ICommandHandler<RemoveSlotCommand, Unit>, RemoveSlotHandler>();

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

        return services;
    }
}
