using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Reference;

/// <summary>Справочник системы: встроенный контент + кастомный контент пользователя.</summary>
public record GetReferenceQuery(Guid UserId, GameSystem System) : IQuery<ReferenceResponse>;
