using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

public record ContentDefinition(CustomEntryType Type, Guid Id, GameSystem System, Guid? OwnerUserId,
    string Name, string NameRu, string Code, string Meta, bool Retired);

/// <summary>Metadata only. Complete forms and mechanics still use the typed definition handlers.</summary>
public static class ContentDefinitions
{
    public static async Task<List<ContentDefinition>> LoadAsync(IAppDbContext db, GameSystem? system = null, Guid? owner = null, CancellationToken ct = default, bool builtinOnly = false, IReadOnlyCollection<Guid>? ids = null)
    {
        List<ContentDefinition> rows = [];
        rows.AddRange((await db.SkillDefs.AsNoTracking().Where(x => (system == null || x.System == system) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Select(x => new ContentDefinition(CustomEntryType.Skill, x.Id, x.System, x.OwnerUserId, x.Name, x.NameRu, x.Code, $"{x.Characteristic} · {x.Kind}", x.Retired)));
        rows.AddRange((await db.TalentDefs.AsNoTracking().Where(x => (system == null || x.System == system) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Where(x => x.OwnerUserId != null || (x.Setting & (x.System == GameSystem.GenesysCore ? GenesysSetting.Any : GenesysSetting.Any | GenesysSetting.Fantasy)) != 0)
            .Select(x => new ContentDefinition(CustomEntryType.Talent, x.Id, x.System, x.OwnerUserId, x.Name, x.NameRu, x.Code, $"{x.Tier} · {x.Activation}" + (x.IsRanked ? " · ranked" : ""), x.Retired)));
        rows.AddRange((await db.ItemDefs.AsNoTracking().Where(x => (system == null || x.System == system) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Select(x => new ContentDefinition(CustomEntryType.Item, x.Id, x.System, x.OwnerUserId, x.Name, x.NameRu, x.Code, $"{x.Kind} · {x.Encumbrance}", x.Retired)));
        rows.AddRange((await db.ArchetypeDefs.AsNoTracking().Where(x => (system == null || x.System == system) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Select(x => new ContentDefinition(CustomEntryType.Archetype, x.Id, x.System, x.OwnerUserId, x.Name, x.NameRu, x.Code, $"{x.StartingXp} XP · {x.Brawn}/{x.Agility}/{x.Intellect}/{x.Cunning}/{x.Willpower}/{x.Presence}", x.Retired)));
        rows.AddRange((await db.CareerDefs.AsNoTracking().Where(x => (system == null || x.System == system) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Select(x => new ContentDefinition(CustomEntryType.Career, x.Id, x.System, x.OwnerUserId, x.Name, x.NameRu, x.Code, string.Join(" · ", x.CareerSkillNames), x.Retired)));
        rows.AddRange((await db.HeroicAbilityDefs.AsNoTracking().Where(x => (system == null || system == GameSystem.RealmsOfTerrinoth) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Select(x => new ContentDefinition(CustomEntryType.HeroicAbility, x.Id, GameSystem.RealmsOfTerrinoth, x.OwnerUserId, x.Name, x.NameRu, x.Code, x.Activation, x.Retired)));
        rows.AddRange((await db.AttachmentDefs.AsNoTracking().Where(x => (system == null || x.System == system) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Select(x => new ContentDefinition(CustomEntryType.Attachment, x.Id, x.System, x.OwnerUserId, x.Name, x.NameRu, x.Code, "", x.Retired)));
        rows.AddRange((await db.MountDefs.AsNoTracking().Where(x => (system == null || x.System == system) && (owner == null || x.OwnerUserId == owner) && (!builtinOnly || x.OwnerUserId == null) && (ids == null || ids.Contains(x.Id))).ToListAsync(ct))
            .Select(x => new ContentDefinition(CustomEntryType.Mount, x.Id, x.System, x.OwnerUserId, x.Name, x.NameRu, x.Code, "", x.Retired)));
        return rows;
    }
}
