using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
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
    }
}
