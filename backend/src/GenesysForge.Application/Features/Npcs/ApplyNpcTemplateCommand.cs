using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Npcs;

/// <summary>Применить шаблон типа существа к форме NPC (ручной режим). Результат не сохраняется в БД.</summary>
public record ApplyNpcTemplateCommand(Guid UserId, ApplyTemplateRequest Request) : ICommand<NpcDetailDto>;
