using GenesysForge.Application.Abstractions;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ExternalAuthIdentity> ExternalAuthIdentities => Set<ExternalAuthIdentity>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SkillDef> SkillDefs => Set<SkillDef>();
    public DbSet<TalentDef> TalentDefs => Set<TalentDef>();
    public DbSet<ItemDef> ItemDefs => Set<ItemDef>();
    public DbSet<HeroicAbilityDef> HeroicAbilityDefs => Set<HeroicAbilityDef>();
    public DbSet<HeroicAbilityUpgradeDef> HeroicAbilityUpgradeDefs => Set<HeroicAbilityUpgradeDef>();
    public DbSet<ArchetypeDef> ArchetypeDefs => Set<ArchetypeDef>();
    public DbSet<CareerDef> CareerDefs => Set<CareerDef>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterSkill> CharacterSkills => Set<CharacterSkill>();
    public DbSet<CharacterTalent> CharacterTalents => Set<CharacterTalent>();
    public DbSet<CharacterItem> CharacterItems => Set<CharacterItem>();
    public DbSet<CharacterNote> CharacterNotes => Set<CharacterNote>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignCharacter> CampaignCharacters => Set<CampaignCharacter>();
    public DbSet<CampaignNote> CampaignNotes => Set<CampaignNote>();
    public DbSet<SpellDef> SpellDefs => Set<SpellDef>();
    public DbSet<Npc> Npcs => Set<Npc>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GameParticipant> GameParticipants => Set<GameParticipant>();
    public DbSet<InitiativeSlot> InitiativeSlots => Set<InitiativeSlot>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<EncounterParticipant> EncounterParticipants => Set<EncounterParticipant>();
    public DbSet<ContentPack> ContentPacks => Set<ContentPack>();
    public DbSet<ContentPackEntry> ContentPackEntries => Set<ContentPackEntry>();
    public DbSet<RollLogEntry> RollLogEntries => Set<RollLogEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        b.Entity<PasswordResetToken>(e =>
        {
            e.HasIndex(t => t.TokenHash);
            e.HasIndex(t => t.UserId);
            e.Property(t => t.TokenHash).HasMaxLength(64);
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ExternalAuthIdentity>(e =>
        {
            e.HasIndex(i => new { i.Provider, i.ProviderUserId }).IsUnique();
            e.HasIndex(i => i.UserId);
            e.Property(i => i.Provider).HasMaxLength(40);
            e.Property(i => i.ProviderUserId).HasMaxLength(255);
            e.Property(i => i.Email).HasMaxLength(255);
            e.HasOne<User>().WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.FamilyId);
            e.HasIndex(t => t.UserId);
            e.Property(t => t.TokenHash).HasMaxLength(64);
            e.Property(t => t.UserAgent).HasMaxLength(400);
            e.Property(t => t.CreatedByIp).HasMaxLength(64);
            e.Ignore(t => t.IsActive);
            e.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Character>(e =>
        {
            e.HasOne(c => c.Archetype).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Career).WithMany().OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.HeroicAbility).WithMany().OnDelete(DeleteBehavior.SetNull);
            e.HasMany(c => c.Skills).WithOne().HasForeignKey(s => s.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Talents).WithOne().HasForeignKey(t => t.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(c => c.Characteristics);
        });

        b.Entity<CharacterSkill>(e =>
        {
            e.HasOne(s => s.SkillDef).WithMany().OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => new { s.CharacterId, s.SkillDefId }).IsUnique();
        });
        b.Entity<CharacterTalent>(e =>
        {
            e.HasOne(t => t.TalentDef).WithMany().OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.CharacterId, t.TalentDefId }).IsUnique();
        });
        b.Entity<CharacterItem>()
            .HasOne(i => i.ItemDef).WithMany().OnDelete(DeleteBehavior.Cascade);

        b.Entity<CharacterNote>(e =>
        {
            e.HasOne<Character>().WithMany().HasForeignKey(n => n.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(n => n.CharacterId);
            e.Property(n => n.Title).HasMaxLength(200);
        });

        b.Entity<Campaign>(e =>
        {
            e.HasIndex(c => c.GmUserId);
            e.HasIndex(c => c.JoinCode).IsUnique();
            e.Property(c => c.Name).HasMaxLength(200);
            e.Property(c => c.JoinCode).HasMaxLength(16);
            e.HasMany(c => c.Characters).WithOne().HasForeignKey(cc => cc.CampaignId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Notes).WithOne().HasForeignKey(n => n.CampaignId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<CampaignCharacter>(e =>
        {
            // удаление персонажа убирает его из кампаний
            e.HasOne(cc => cc.Character).WithMany().HasForeignKey(cc => cc.CharacterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(cc => new { cc.CampaignId, cc.CharacterId }).IsUnique();
        });
        b.Entity<CampaignNote>().Property(n => n.Title).HasMaxLength(200);

        b.Entity<SpellDef>(e =>
        {
            e.HasIndex(s => new { s.System, s.MagicSkill, s.Kind });
            e.Property(s => s.MagicSkill).HasMaxLength(40);
            e.Property(s => s.ParentEffect).HasMaxLength(120);
            e.Property(s => s.NameRu).HasMaxLength(120);
            e.Property(s => s.NameEn).HasMaxLength(120);
            e.Property(s => s.Source).HasMaxLength(120);
        });

        b.Entity<HeroicAbilityDef>(e =>
        {
            e.HasMany(h => h.Upgrades).WithOne()
                .HasForeignKey(u => u.HeroicAbilityDefId).OnDelete(DeleteBehavior.Cascade);
            e.Property(h => h.Requirement).HasMaxLength(200);
            e.Property(h => h.ActivationCost).HasMaxLength(80);
            e.Property(h => h.Activation).HasMaxLength(120);
            e.Property(h => h.Duration).HasMaxLength(200);
            e.Property(h => h.Frequency).HasMaxLength(200);
        });

        b.Entity<ItemDef>(e =>
        {
            e.Property(i => i.SkillName).HasMaxLength(40);
            e.Property(i => i.Damage).HasMaxLength(20);
            e.Property(i => i.Crit).HasMaxLength(20);
            e.Property(i => i.RangeBand).HasMaxLength(40);
            e.Property(i => i.Properties).HasMaxLength(400);
        });

        b.Entity<Npc>(e =>
        {
            e.HasIndex(n => n.OwnerUserId);
            e.HasIndex(n => n.CampaignId);
            e.Property(n => n.Name).HasMaxLength(200);
            e.Property(n => n.Source).HasMaxLength(160);
            e.Property(n => n.Talents).HasMaxLength(2000);
            e.Property(n => n.Equipment).HasMaxLength(2000);
            e.Property(n => n.Tags).HasMaxLength(1000);
            e.HasMany(n => n.Skills).WithOne().HasForeignKey(s => s.NpcId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(n => n.Abilities).WithOne().HasForeignKey(a => a.NpcId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<NpcSkill>().Property(s => s.Name).HasMaxLength(80);
        b.Entity<NpcAbility>(e =>
        {
            e.Property(a => a.Name).HasMaxLength(120);
            e.Property(a => a.Description).HasMaxLength(2000);
        });

        b.Entity<GameSession>(e =>
        {
            e.HasIndex(s => s.CampaignId);
            e.Property(s => s.Name).HasMaxLength(200);
            e.HasMany(s => s.Participants).WithOne().HasForeignKey(p => p.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.Slots).WithOne().HasForeignKey(sl => sl.SessionId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<GameParticipant>(e =>
        {
            e.HasIndex(p => p.SessionId);
            e.Property(p => p.DisplayName).HasMaxLength(200);
            e.Property(p => p.Notes).HasMaxLength(2000);
        });
        b.Entity<InitiativeSlot>(e =>
        {
            e.HasIndex(s => s.SessionId);
            e.Property(s => s.Notes).HasMaxLength(400);
        });

        b.Entity<Encounter>(e =>
        {
            e.HasIndex(en => en.CampaignId);
            e.Property(en => en.Name).HasMaxLength(200);
            e.Property(en => en.Location).HasMaxLength(200);
            e.Property(en => en.Environment).HasMaxLength(400);
            e.Property(en => en.Tags).HasMaxLength(1000);
            e.HasMany(en => en.Participants).WithOne()
                .HasForeignKey(p => p.EncounterId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<EncounterParticipant>(e =>
        {
            e.HasIndex(p => p.EncounterId);
            e.Property(p => p.DisplayName).HasMaxLength(200);
            e.Property(p => p.Notes).HasMaxLength(2000);
        });

        b.Entity<ContentPack>(e =>
        {
            e.HasIndex(p => p.CampaignId);
            e.HasIndex(p => p.OwnerUserId);
            e.Property(p => p.Name).HasMaxLength(200);
            e.HasMany(p => p.Entries).WithOne()
                .HasForeignKey(en => en.ContentPackId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ContentPackEntry>(e =>
        {
            e.HasIndex(en => en.ContentPackId);
            e.Property(en => en.Title).HasMaxLength(200);
            e.Property(en => en.Source).HasMaxLength(200);
            e.Property(en => en.PageRef).HasMaxLength(80);
            e.Property(en => en.SafeSummary).HasMaxLength(2000);
            e.Property(en => en.GmNotes).HasMaxLength(2000);
            e.Property(en => en.PlayerNotes).HasMaxLength(2000);
            e.Property(en => en.Tags).HasMaxLength(1000);
        });

        b.Entity<RollLogEntry>(e =>
        {
            e.HasIndex(r => new { r.CampaignId, r.CreatedAt });
            e.Property(r => r.ActorName).HasMaxLength(200);
            e.Property(r => r.Label).HasMaxLength(200);
            e.Property(r => r.PoolJson).HasMaxLength(2000);
            e.Property(r => r.ResultJson).HasMaxLength(4000);
            e.Property(r => r.Summary).HasMaxLength(400);
        });

        // Content-model (Code/NameRu/Source) у справочных сущностей.
        ConfigureContent<SkillDef>(b);
        ConfigureContent<TalentDef>(b);
        ConfigureContent<ItemDef>(b);
        ConfigureContent<ArchetypeDef>(b);
        ConfigureContent<CareerDef>(b);
        ConfigureContent<HeroicAbilityDef>(b);
    }

    private static void ConfigureContent<T>(ModelBuilder b) where T : class, IContentDef
    {
        var e = b.Entity<T>();
        e.Property(d => d.Code).HasMaxLength(80);
        e.Property(d => d.NameRu).HasMaxLength(160);
        e.Property(d => d.Source).HasMaxLength(160);
    }
}
