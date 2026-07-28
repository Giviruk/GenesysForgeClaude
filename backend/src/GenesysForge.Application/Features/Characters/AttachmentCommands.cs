using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Покупка улучшения в запас персонажа (ROT-EQP-ATT-01).</summary>
public record BuyAttachmentCommand(Guid UserId, Guid CharacterId, BuyAttachmentRequest Request)
    : ICommand<Guid>;

/// <summary>Установка улучшения на предмет.</summary>
public record InstallAttachmentCommand(Guid UserId, Guid CharacterId, InstallAttachmentRequest Request)
    : ICommand<Unit>;

/// <summary>Снятие улучшения с предмета с явным исходом.</summary>
public record DetachAttachmentCommand(
    Guid UserId, Guid CharacterId, Guid AttachmentId, DetachAttachmentRequest Request) : ICommand<Unit>;

/// <summary>Удаление улучшения из запаса без выручки.</summary>
public record RemoveAttachmentCommand(Guid UserId, Guid CharacterId, Guid AttachmentId) : ICommand<Unit>;
