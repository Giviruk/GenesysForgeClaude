using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Auth;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Application.Features.CustomContent;
using GenesysForge.Application.Features.Notes;
using GenesysForge.Application.Features.Reference;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<ICommandHandler<RegisterUserCommand, AuthResponse>, RegisterUserHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthResponse>, LoginHandler>();

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
        services.AddScoped<ICommandHandler<AddItemCommand, Guid>, AddItemHandler>();
        services.AddScoped<ICommandHandler<UpdateItemCommand, Unit>, UpdateItemHandler>();
        services.AddScoped<ICommandHandler<RemoveItemCommand, Unit>, RemoveItemHandler>();

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

        return services;
    }
}
