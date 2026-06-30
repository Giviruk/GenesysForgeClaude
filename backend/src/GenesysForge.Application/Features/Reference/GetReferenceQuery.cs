using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Reference;

/// <summary>Справочник системы: встроенный контент + видимый кастомный/homebrew контент пользователя.</summary>
public record GetReferenceQuery(Guid UserId, GameSystem System, Guid? CharacterId = null, Guid? CampaignId = null)
    : IQuery<ReferenceResponse>;
