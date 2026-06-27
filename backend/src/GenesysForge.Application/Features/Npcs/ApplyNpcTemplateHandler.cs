using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Npcs;

/// <summary>
/// Накладывает шаблон типа существа (теги/способности/природная атака/soak/силуэт) на текущую форму NPC,
/// переиспользуя <see cref="NpcDraftGenerator.ApplyTemplate"/> — единый источник правды с генератором.
/// Идемпотентно (повторное применение того же типа ничего не дублирует). NPC не сохраняется.
/// </summary>
public class ApplyNpcTemplateHandler : ICommandHandler<ApplyNpcTemplateCommand, NpcDetailDto>
{
    public Task<NpcDetailDto> Handle(ApplyNpcTemplateCommand command, CancellationToken ct = default)
    {
        var npc = new Npc { Id = Guid.NewGuid(), OwnerUserId = command.UserId, Name = command.Request.Input.Name };
        NpcMapper.Apply(npc, command.Request.Input);
        // Ручной режим без уровня силы — базовый уровень 1 (Standard).
        NpcDraftGenerator.ApplyTemplate(npc, command.Request.Template, level: 1);
        return Task.FromResult(NpcMapper.ToDetail(npc, command.UserId));
    }
}
