using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Abstractions;

/// <summary>Абстракция персистентности для Application-слоя.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<SkillDef> SkillDefs { get; }
    DbSet<TalentDef> TalentDefs { get; }
    DbSet<ItemDef> ItemDefs { get; }
    DbSet<HeroicAbilityDef> HeroicAbilityDefs { get; }
    DbSet<ArchetypeDef> ArchetypeDefs { get; }
    DbSet<CareerDef> CareerDefs { get; }
    DbSet<Character> Characters { get; }
    DbSet<CharacterSkill> CharacterSkills { get; }
    DbSet<CharacterTalent> CharacterTalents { get; }
    DbSet<CharacterItem> CharacterItems { get; }
    DbSet<CharacterNote> CharacterNotes { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
