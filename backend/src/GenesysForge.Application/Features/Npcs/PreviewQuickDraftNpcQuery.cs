using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

/// <summary>Собрать быстрый черновик NPC без сохранения — live preview формы быстрого создания.</summary>
public record PreviewQuickDraftNpcQuery(Guid UserId, QuickDraftRequest Request) : IQuery<NpcDetailDto>;
