using GenesysForge.Application.Abstractions;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<SkillDef> SkillDefs => Set<SkillDef>();
    public DbSet<TalentDef> TalentDefs => Set<TalentDef>();
    public DbSet<ItemDef> ItemDefs => Set<ItemDef>();
    public DbSet<HeroicAbilityDef> HeroicAbilityDefs => Set<HeroicAbilityDef>();
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

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

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
            e.Property(s => s.NameRu).HasMaxLength(120);
            e.Property(s => s.NameEn).HasMaxLength(120);
            e.Property(s => s.Source).HasMaxLength(120);
        });
    }
}
