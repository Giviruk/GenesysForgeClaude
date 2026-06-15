using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

/// <summary>Сгенерировать и сохранить быстрый детерминированный черновик NPC.</summary>
public record QuickDraftNpcCommand(Guid UserId, QuickDraftRequest Request) : ICommand<NpcDetailDto>;
