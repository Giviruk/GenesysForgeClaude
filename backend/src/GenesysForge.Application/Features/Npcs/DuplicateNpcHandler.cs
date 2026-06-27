using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Npcs;

public class DuplicateNpcHandler(IAppDbContext db) : ICommandHandler<DuplicateNpcCommand, NpcDetailDto>
{
    public async Task<NpcDetailDto> Handle(DuplicateNpcCommand command, CancellationToken ct = default)
    {
        // Дублировать можно любой видимый NPC; копия принадлежит текущему пользователю.
        var src = await NpcMapper.GetViewableAsync(db, command.UserId, command.Id, ct);
        var copy = new Npc
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.UserId,
            CampaignId = src.OwnerUserId == command.UserId ? src.CampaignId : null,
            System = src.System,
            Name = $"{src.Name} (копия)",
            Kind = src.Kind,
            Role = src.Role,
            Description = src.Description,
            Source = src.Source,
            Brawn = src.Brawn,
            Agility = src.Agility,
            Intellect = src.Intellect,
            Cunning = src.Cunning,
            Willpower = src.Willpower,
            Presence = src.Presence,
            WoundThreshold = src.WoundThreshold,
            StrainThreshold = src.StrainThreshold,
            Soak = src.Soak,
            MeleeDefense = src.MeleeDefense,
            RangedDefense = src.RangedDefense,
            Silhouette = src.Silhouette,
            Tactics = src.Tactics,
            Visibility = src.OwnerUserId == command.UserId ? src.Visibility : Domain.NpcVisibility.Private,
            Talents = [.. src.Talents],
            Equipment = [.. src.Equipment],
            Tags = [.. src.Tags],
        };
        copy.Skills = src.Skills.Select(s => new NpcSkill { NpcId = copy.Id, Name = s.Name, Ranks = s.Ranks }).ToList();
        copy.Abilities = src.Abilities.Select(a => new NpcAbility { NpcId = copy.Id, Name = a.Name, Description = a.Description }).ToList();
        copy.Attacks = src.Attacks.Select(a => new NpcAttack
        {
            NpcId = copy.Id,
            Name = a.Name, SkillName = a.SkillName, Damage = a.Damage,
            Critical = a.Critical, RangeBand = a.RangeBand, Notes = a.Notes,
            Qualities = a.Qualities.Select(q => new NpcAttackQuality
            {
                QualityDefId = q.QualityDefId, QualityCode = q.QualityCode, NameRu = q.NameRu, Rating = q.Rating,
            }).ToList(),
        }).ToList();

        db.Npcs.Add(copy);
        await db.SaveChangesAsync(ct);
        return NpcMapper.ToDetail(copy, command.UserId);
    }
}
