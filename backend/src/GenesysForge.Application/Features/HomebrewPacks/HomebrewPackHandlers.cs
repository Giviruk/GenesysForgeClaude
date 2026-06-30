using System.Text.RegularExpressions;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.HomebrewPacks;

public class GetHomebrewPacksHandler(IAppDbContext db)
    : IQueryHandler<GetHomebrewPacksQuery, List<HomebrewPackListItemDto>>
{
    public async Task<List<HomebrewPackListItemDto>> Handle(GetHomebrewPacksQuery q, CancellationToken ct = default)
    {
        var packs = await db.HomebrewPacks.AsNoTracking()
            .Where(p => p.OwnerUserId == q.UserId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);
        var ids = packs.Select(p => p.Id).ToHashSet();
        var counts = await HomebrewPackMapper.CountEntriesAsync(db, ids, ct);
        return packs.Select(p => HomebrewPackMapper.ToListItem(p, counts.GetValueOrDefault(p.Id))).ToList();
    }
}

public class ExportHomebrewPackHandler(IAppDbContext db)
    : IQueryHandler<ExportHomebrewPackQuery, HomebrewPackExportDto>
{
    public async Task<HomebrewPackExportDto> Handle(ExportHomebrewPackQuery q, CancellationToken ct = default)
    {
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, q.UserId, q.PackId, ct);
        return await HomebrewPackMapper.ToExportAsync(db, pack, ct);
    }
}

public class ImportHomebrewPackHandler(IAppDbContext db)
    : ICommandHandler<ImportHomebrewPackCommand, HomebrewPackImportResult>
{
    public async Task<HomebrewPackImportResult> Handle(ImportHomebrewPackCommand command, CancellationToken ct = default) =>
        await HomebrewPackImporter.ImportAsync(db, command.UserId, command.Document, ct);
}

public class ShareHomebrewPackHandler(IAppDbContext db)
    : ICommandHandler<ShareHomebrewPackCommand, HomebrewPackShareDto>
{
    public async Task<HomebrewPackShareDto> Handle(ShareHomebrewPackCommand command, CancellationToken ct = default)
    {
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, command.UserId, command.PackId, ct, tracking: true);
        var token = HomebrewPackTokens.NewRawToken();
        pack.ShareTokenHash = HomebrewPackTokens.Hash(token);
        pack.IsShared = true;
        pack.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new HomebrewPackShareDto(token, $"/homebrew/import/{token}");
    }
}

public class ImportSharedHomebrewPackHandler(IAppDbContext db)
    : ICommandHandler<ImportSharedHomebrewPackCommand, HomebrewPackImportResult>
{
    public async Task<HomebrewPackImportResult> Handle(ImportSharedHomebrewPackCommand command, CancellationToken ct = default)
    {
        var hash = HomebrewPackTokens.Hash(command.Token);
        var pack = await db.HomebrewPacks.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsShared && p.ShareTokenHash == hash, ct)
            ?? throw new DomainRuleException("Homebrew-набор не найден.");
        var document = await HomebrewPackMapper.ToExportAsync(db, pack, ct);
        return await HomebrewPackImporter.ImportAsync(db, command.UserId, document, ct);
    }
}

public class SetHomebrewPackDefaultHandler(IAppDbContext db)
    : ICommandHandler<SetHomebrewPackDefaultCommand, Unit>
{
    public async Task<Unit> Handle(SetHomebrewPackDefaultCommand command, CancellationToken ct = default)
    {
        var pack = await HomebrewPackMapper.GetOwnedAsync(db, command.UserId, command.PackId, ct, tracking: true);
        pack.IsEnabledByDefault = command.IsEnabled;
        pack.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class SetCharacterHomebrewPackHandler(IAppDbContext db)
    : ICommandHandler<SetCharacterHomebrewPackCommand, Unit>
{
    public async Task<Unit> Handle(SetCharacterHomebrewPackCommand command, CancellationToken ct = default)
    {
        await HomebrewPackMapper.GetOwnedAsync(db, command.UserId, command.PackId, ct);
        var owns = await db.Characters.AnyAsync(c => c.Id == command.CharacterId && c.OwnerUserId == command.UserId, ct);
        if (!owns) throw new DomainRuleException("Персонаж не найден.");

        var row = await db.HomebrewPackCharacters.FirstOrDefaultAsync(
            x => x.HomebrewPackId == command.PackId && x.CharacterId == command.CharacterId, ct);
        if (row is null)
        {
            db.HomebrewPackCharacters.Add(new HomebrewPackCharacter
            {
                Id = Guid.NewGuid(),
                HomebrewPackId = command.PackId,
                CharacterId = command.CharacterId,
                IsEnabled = command.IsEnabled,
            });
        }
        else
        {
            row.IsEnabled = command.IsEnabled;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

public class SetCampaignHomebrewPackHandler(IAppDbContext db)
    : ICommandHandler<SetCampaignHomebrewPackCommand, Unit>
{
    public async Task<Unit> Handle(SetCampaignHomebrewPackCommand command, CancellationToken ct = default)
    {
        await HomebrewPackMapper.GetOwnedAsync(db, command.UserId, command.PackId, ct);
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);

        var row = await db.HomebrewPackCampaigns.FirstOrDefaultAsync(
            x => x.HomebrewPackId == command.PackId && x.CampaignId == command.CampaignId, ct);
        if (row is null)
        {
            db.HomebrewPackCampaigns.Add(new HomebrewPackCampaign
            {
                Id = Guid.NewGuid(),
                HomebrewPackId = command.PackId,
                CampaignId = command.CampaignId,
                IsEnabled = command.IsEnabled,
            });
        }
        else
        {
            row.IsEnabled = command.IsEnabled;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

internal static class HomebrewPackMapper
{
    public const string Format = "genesysforge.homebrew-pack.v1";

    public static async Task<HomebrewPack> GetOwnedAsync(
        IAppDbContext db, Guid userId, Guid packId, CancellationToken ct, bool tracking = false)
    {
        var query = db.HomebrewPacks.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == packId && p.OwnerUserId == userId, ct)
            ?? throw new DomainRuleException("Homebrew-набор не найден.");
    }

    public static HomebrewPackListItemDto ToListItem(HomebrewPack p, int count) =>
        new(p.Id, p.Name, p.Description, p.System, p.IsShared, p.IsEnabledByDefault, count, p.UpdatedAt);

    public static async Task<Dictionary<Guid, int>> CountEntriesAsync(IAppDbContext db, HashSet<Guid> packIds, CancellationToken ct)
    {
        var counts = packIds.ToDictionary(id => id, _ => 0);
        async Task AddCounts<T>(IQueryable<T> query, Func<T, Guid?> getPackId) where T : class
        {
            var ids = await query.ToListAsync(ct);
            foreach (var id in ids.Select(getPackId).Where(id => id is not null).Select(id => id!.Value))
                counts[id] = counts.GetValueOrDefault(id) + 1;
        }
        await AddCounts(db.SkillDefs.AsNoTracking().Where(x => x.HomebrewPackId != null && packIds.Contains(x.HomebrewPackId.Value)), x => x.HomebrewPackId);
        await AddCounts(db.TalentDefs.AsNoTracking().Where(x => x.HomebrewPackId != null && packIds.Contains(x.HomebrewPackId.Value)), x => x.HomebrewPackId);
        await AddCounts(db.ItemDefs.AsNoTracking().Where(x => x.HomebrewPackId != null && packIds.Contains(x.HomebrewPackId.Value)), x => x.HomebrewPackId);
        await AddCounts(db.HeroicAbilityDefs.AsNoTracking().Where(x => x.HomebrewPackId != null && packIds.Contains(x.HomebrewPackId.Value)), x => x.HomebrewPackId);
        await AddCounts(db.ArchetypeDefs.AsNoTracking().Where(x => x.HomebrewPackId != null && packIds.Contains(x.HomebrewPackId.Value)), x => x.HomebrewPackId);
        await AddCounts(db.CareerDefs.AsNoTracking().Where(x => x.HomebrewPackId != null && packIds.Contains(x.HomebrewPackId.Value)), x => x.HomebrewPackId);
        return counts;
    }

    public static async Task<HomebrewPackExportDto> ToExportAsync(IAppDbContext db, HomebrewPack pack, CancellationToken ct)
    {
        var skills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.HomebrewPackId == pack.Id)
            .Select(s => new HomebrewSkillDto(s.Code, s.Name, s.NameRu, s.Characteristic, s.Kind, s.Description, s.SafeDescription, s.Source))
            .ToListAsync(ct);
        var talents = await db.TalentDefs.AsNoTracking()
            .Where(t => t.HomebrewPackId == pack.Id)
            .Select(t => new HomebrewTalentDto(t.Code, t.Name, t.NameRu, t.Tier, t.IsRanked, t.Activation, t.Description,
                t.SafeDescription, t.Source, t.WoundBonus, t.StrainBonus, t.SoakBonus, t.MeleeDefenseBonus, t.RangedDefenseBonus))
            .ToListAsync(ct);
        var items = await db.ItemDefs.AsNoTracking()
            .Where(i => i.HomebrewPackId == pack.Id)
            .Select(i => new HomebrewItemDto(i.Code, i.Name, i.NameRu, i.Kind, i.Encumbrance, i.SoakBonus, i.MeleeDefense,
                i.RangedDefense, i.EncumbranceThresholdBonus, i.Description, i.SafeDescription, i.Source, i.Price, i.Rarity,
                i.SkillName, i.Damage, i.Crit, i.RangeBand, i.Properties))
            .ToListAsync(ct);
        var heroics = await db.HeroicAbilityDefs.AsNoTracking()
            .Where(h => h.HomebrewPackId == pack.Id)
            .Select(h => new HomebrewHeroicAbilityDto(h.Code, h.Name, h.NameRu, h.Description, h.SafeDescription, h.Source,
                h.Requirement, h.ActivationCost, h.Activation, h.Duration, h.Frequency, h.Notes))
            .ToListAsync(ct);
        var archetypes = await db.ArchetypeDefs.AsNoTracking().Include(a => a.Abilities)
            .Where(a => a.HomebrewPackId == pack.Id)
            .Select(a => new HomebrewArchetypeDto(a.Code, a.Name, a.NameRu, a.Brawn, a.Agility, a.Intellect, a.Cunning,
                a.Willpower, a.Presence, a.WoundBase, a.StrainBase, a.StartingXp, a.Description, a.SafeDescription, a.Source,
                a.Abilities.Select(x => new HomebrewArchetypeAbilityDto(x.Code, x.NameRu, x.NameEn, x.SafeDescription)).ToList()))
            .ToListAsync(ct);
        var careers = await db.CareerDefs.AsNoTracking()
            .Where(c => c.HomebrewPackId == pack.Id)
            .Select(c => new HomebrewCareerDto(c.Code, c.Name, c.NameRu, c.Description, c.SafeDescription, c.Source,
                c.CareerSkillNames, c.StartingMoneyFixed, c.StartingMoneyDice))
            .ToListAsync(ct);
        return new HomebrewPackExportDto(Format, pack.Name, pack.Description, pack.System, skills, talents, items, heroics, archetypes, careers);
    }
}

internal static partial class HomebrewPackImporter
{
    public static async Task<HomebrewPackImportResult> ImportAsync(
        IAppDbContext db, Guid userId, HomebrewPackExportDto doc, CancellationToken ct)
    {
        if (!string.Equals(doc.Format, HomebrewPackMapper.Format, StringComparison.Ordinal))
            throw new DomainRuleException("Неподдерживаемый формат homebrew-набора.");
        if (string.IsNullOrWhiteSpace(doc.Name))
            throw new DomainRuleException("Название homebrew-набора не может быть пустым.");

        var pack = new HomebrewPack
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = doc.Name.Trim(),
            Description = doc.Description?.Trim() ?? "",
            System = doc.System,
            IsEnabledByDefault = true,
        };
        db.HomebrewPacks.Add(pack);

        var count = 0;
        foreach (var s in doc.Skills ?? [])
        {
            RequireName(s.Name, "навыка");
            db.SkillDefs.Add(new SkillDef
            {
                Id = Guid.NewGuid(), System = doc.System, Code = Code(s.Code, "skill", s.Name), Name = s.Name.Trim(),
                NameRu = Clean(s.NameRu), Characteristic = s.Characteristic, Kind = s.Kind,
                Description = Clean(s.Description), SafeDescription = Clean(s.SafeDescription), Source = Clean(s.Source),
                OwnerUserId = userId, HomebrewPackId = pack.Id,
            });
            count++;
        }
        foreach (var t in doc.Talents ?? [])
        {
            RequireName(t.Name, "таланта");
            if (t.Tier is < 1 or > GenesysRules.MaxTalentTier) throw new DomainRuleException("Тир таланта должен быть от 1 до 5.");
            db.TalentDefs.Add(new TalentDef
            {
                Id = Guid.NewGuid(), System = doc.System, Code = Code(t.Code, "talent", t.Name), Name = t.Name.Trim(),
                NameRu = Clean(t.NameRu), Tier = t.Tier, IsRanked = t.IsRanked,
                Activation = string.IsNullOrWhiteSpace(t.Activation) ? "Пассивный" : t.Activation.Trim(),
                Description = Clean(t.Description), SafeDescription = Clean(t.SafeDescription), Source = Clean(t.Source),
                WoundBonus = t.WoundBonus, StrainBonus = t.StrainBonus, SoakBonus = t.SoakBonus,
                MeleeDefenseBonus = t.MeleeDefenseBonus, RangedDefenseBonus = t.RangedDefenseBonus,
                OwnerUserId = userId, HomebrewPackId = pack.Id,
            });
            count++;
        }
        foreach (var i in doc.Items ?? [])
        {
            RequireName(i.Name, "предмета");
            db.ItemDefs.Add(new ItemDef
            {
                Id = Guid.NewGuid(), System = doc.System, Code = Code(i.Code, "item", i.Name), Name = i.Name.Trim(),
                NameRu = Clean(i.NameRu), Kind = i.Kind, Encumbrance = Math.Max(0, i.Encumbrance),
                SoakBonus = i.SoakBonus, MeleeDefense = i.MeleeDefense, RangedDefense = i.RangedDefense,
                EncumbranceThresholdBonus = i.EncumbranceThresholdBonus, Description = Clean(i.Description),
                SafeDescription = Clean(i.SafeDescription), Source = Clean(i.Source), Price = Math.Max(0, i.Price),
                Rarity = Math.Max(0, i.Rarity), SkillName = Clean(i.SkillName), Damage = Clean(i.Damage),
                Crit = Clean(i.Crit), RangeBand = Clean(i.RangeBand), Properties = Clean(i.Properties),
                OwnerUserId = userId, HomebrewPackId = pack.Id,
            });
            count++;
        }
        foreach (var h in doc.HeroicAbilities ?? [])
        {
            RequireName(h.Name, "героической способности");
            db.HeroicAbilityDefs.Add(new HeroicAbilityDef
            {
                Id = Guid.NewGuid(), Code = Code(h.Code, "heroic", h.Name), Name = h.Name.Trim(), NameRu = Clean(h.NameRu),
                Description = Clean(h.Description), SafeDescription = Clean(h.SafeDescription), Source = Clean(h.Source),
                Requirement = Clean(h.Requirement), ActivationCost = Clean(h.ActivationCost), Activation = Clean(h.Activation),
                Duration = Clean(h.Duration), Frequency = Clean(h.Frequency), Notes = Clean(h.Notes),
                OwnerUserId = userId, HomebrewPackId = pack.Id,
            });
            count++;
        }
        foreach (var a in doc.Archetypes ?? [])
        {
            RequireName(a.Name, "архетипа");
            ValidateCharacteristic(a.Brawn); ValidateCharacteristic(a.Agility); ValidateCharacteristic(a.Intellect);
            ValidateCharacteristic(a.Cunning); ValidateCharacteristic(a.Willpower); ValidateCharacteristic(a.Presence);
            var def = new ArchetypeDef
            {
                Id = Guid.NewGuid(), System = doc.System, Code = Code(a.Code, "archetype", a.Name), Name = a.Name.Trim(),
                NameRu = Clean(a.NameRu), Brawn = a.Brawn, Agility = a.Agility, Intellect = a.Intellect, Cunning = a.Cunning,
                Willpower = a.Willpower, Presence = a.Presence, WoundBase = a.WoundBase, StrainBase = a.StrainBase,
                StartingXp = a.StartingXp, Description = Clean(a.Description), SafeDescription = Clean(a.SafeDescription),
                Source = Clean(a.Source), OwnerUserId = userId, HomebrewPackId = pack.Id,
            };
            foreach (var ability in a.Abilities ?? [])
                def.Abilities.Add(new ArchetypeAbilityDef
                {
                    Id = Guid.NewGuid(), ArchetypeId = def.Id, Code = Code(ability.Code, "archetype-ability", ability.NameRu),
                    NameRu = ability.NameRu.Trim(), NameEn = Clean(ability.NameEn), SafeDescription = Clean(ability.SafeDescription),
                    AutomationKind = ArchetypeAbilityAutomationKind.Manual,
                });
            db.ArchetypeDefs.Add(def);
            count++;
        }
        foreach (var c in doc.Careers ?? [])
        {
            RequireName(c.Name, "карьеры");
            db.CareerDefs.Add(new CareerDef
            {
                Id = Guid.NewGuid(), System = doc.System, Code = Code(c.Code, "career", c.Name), Name = c.Name.Trim(),
                NameRu = Clean(c.NameRu), Description = Clean(c.Description), SafeDescription = Clean(c.SafeDescription),
                Source = Clean(c.Source), CareerSkillNames = (c.CareerSkillNames ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(),
                StartingMoneyFixed = Math.Max(0, c.StartingMoneyFixed), StartingMoneyDice = Clean(c.StartingMoneyDice),
                OwnerUserId = userId, HomebrewPackId = pack.Id,
            });
            count++;
        }

        await db.SaveChangesAsync(ct);
        return new HomebrewPackImportResult(pack.Id, pack.Name, count);
    }

    private static void RequireName(string name, string label)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException($"Название {label} не может быть пустым.");
    }

    private static void ValidateCharacteristic(int value)
    {
        if (value is < 1 or > 5) throw new DomainRuleException("Характеристики архетипа должны быть от 1 до 5.");
    }

    private static string Clean(string? value) => value?.Trim() ?? "";

    private static string Code(string? code, string kind, string name)
    {
        var clean = code?.Trim();
        if (!string.IsNullOrWhiteSpace(clean)) return clean;
        var slug = SlugRegex().Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');
        return $"homebrew.{kind}.{slug}";
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SlugRegex();
}
