using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Abstractions;

/// <summary>Абстракция персистентности для Application-слоя.</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SkillDef> SkillDefs { get; }
    DbSet<TalentDef> TalentDefs { get; }
    DbSet<ItemDef> ItemDefs { get; }
    DbSet<HeroicAbilityDef> HeroicAbilityDefs { get; }
    DbSet<HeroicAbilityUpgradeDef> HeroicAbilityUpgradeDefs { get; }
    DbSet<ArchetypeDef> ArchetypeDefs { get; }
    DbSet<CareerDef> CareerDefs { get; }
    DbSet<Character> Characters { get; }
    DbSet<CharacterSkill> CharacterSkills { get; }
    DbSet<CharacterTalent> CharacterTalents { get; }
    DbSet<CharacterItem> CharacterItems { get; }
    DbSet<CharacterNote> CharacterNotes { get; }
    DbSet<Campaign> Campaigns { get; }
    DbSet<CampaignCharacter> CampaignCharacters { get; }
    DbSet<CampaignNote> CampaignNotes { get; }
    DbSet<SpellDef> SpellDefs { get; }
    DbSet<Npc> Npcs { get; }
    DbSet<GameSession> GameSessions { get; }
    DbSet<GameParticipant> GameParticipants { get; }
    DbSet<InitiativeSlot> InitiativeSlots { get; }
    DbSet<Encounter> Encounters { get; }
    DbSet<EncounterParticipant> EncounterParticipants { get; }
    DbSet<ContentPack> ContentPacks { get; }
    DbSet<ContentPackEntry> ContentPackEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
