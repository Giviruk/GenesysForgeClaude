using System.Text.RegularExpressions;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.ContentLibrary;
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
        var exclusions = await db.HomebrewPackExclusions.Where(x => ids.Contains(x.HomebrewPackId)).ToListAsync(ct);
        var campaigns = await (from link in db.HomebrewPackCampaigns join c in db.Campaigns on link.CampaignId equals c.Id
            where ids.Contains(link.HomebrewPackId) && link.Status == ContentConnectionStatus.Active
            select new { link.HomebrewPackId, c.Id, c.Name }).ToListAsync(ct);
        return packs.Select(p => HomebrewPackMapper.ToListItem(p, counts.GetValueOrDefault(p.Id)) with
        {
            ExclusionCount = exclusions.Count(x => x.HomebrewPackId == p.Id),
            Campaigns = campaigns.Where(x => x.HomebrewPackId == p.Id).Select(x => new PackCampaignDto(x.Id, x.Name)).ToList(),
        }).ToList();
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

        if (await db.CampaignCharacters.AnyAsync(x => x.CharacterId == command.CharacterId, ct))
            throw new DomainRuleException("Наборами персонажа в кампании управляет мастер. Подключите набор к кампании.",
                "homebrew.character_campaign_context");

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
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);

        if (command.UpdatePolicy is { } requestedPolicy && !Enum.IsDefined(requestedPolicy))
            throw new DomainRuleException("Неизвестная политика обновления.");

        var row = await db.HomebrewPackCampaigns.FirstOrDefaultAsync(
            x => x.HomebrewPackId == command.PackId && x.CampaignId == command.CampaignId, ct);
        if (row is null)
        {
            // First connection by ID requires ownership. Shared originals are authorized by token.
            await HomebrewPackMapper.GetOwnedAsync(db, command.UserId, command.PackId, ct);
            db.HomebrewPackCampaigns.Add(new HomebrewPackCampaign
            {
                Id = Guid.NewGuid(),
                HomebrewPackId = command.PackId,
                CampaignId = command.CampaignId,
                IsEnabled = command.IsEnabled, UpdatePolicy = command.UpdatePolicy ?? ContentUpdatePolicy.Auto,
            });
        }
        else
        {
            row.IsEnabled = command.IsEnabled;
            if (command.IsEnabled) row.Status = ContentConnectionStatus.Active;
            if (command.UpdatePolicy is { } policy)
            {
                if (row.UpdatePolicy != policy && policy == ContentUpdatePolicy.Auto)
                    db.CampaignPackEntryStates.RemoveRange(await db.CampaignPackEntryStates
                        .Where(x => x.HomebrewPackCampaignId == row.Id && x.State == PackEntryState.Pending).ToListAsync(ct));
                row.UpdatePolicy = policy;
            }
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

internal static class HomebrewPackMapper
{
    public const string Format = "genesysforge.homebrew-pack.v2";

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
        var rows = await db.HomebrewPackEntries.AsNoTracking().Where(x => packIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
        return rows.GroupBy(x => x.HomebrewPackId).ToDictionary(g => g.Key, g => g.Count());
    }

    public static async Task<HomebrewPackExportDto> ToExportAsync(IAppDbContext db, HomebrewPack pack, CancellationToken ct)
    {
        var skills = await db.SkillDefs.AsNoTracking()
            .Where(s => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == s.Id))
            .Select(s => new HomebrewSkillDto(s.Code, s.Name, s.NameRu, s.Characteristic, s.Kind, s.Description, s.SafeDescription, s.Source))
            .ToListAsync(ct);
        var talents = await db.TalentDefs.AsNoTracking()
            .Where(t => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == t.Id))
            .Select(t => new HomebrewTalentDto(t.Code, t.Name, t.NameRu, t.Tier, t.IsRanked, t.Activation, t.Description,
                t.SafeDescription, t.Source, t.WoundBonus, t.StrainBonus, t.SoakBonus, t.MeleeDefenseBonus,
                t.RangedDefenseBonus, t.Category))
            .ToListAsync(ct);
        var items = await db.ItemDefs.AsNoTracking()
            .Where(i => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == i.Id))
            .Select(i => new HomebrewItemDto(i.Code, i.Name, i.NameRu, i.Kind, i.Encumbrance, i.SoakBonus, i.MeleeDefense,
                i.RangedDefense, i.EncumbranceThresholdBonus, i.Description, i.SafeDescription, i.Source,
                i.Price ?? 0, i.Rarity ?? 0,
                i.SkillName, i.Damage, i.Crit, i.RangeBand, i.Properties))
            .ToListAsync(ct);
        var heroics = await db.HeroicAbilityDefs.AsNoTracking()
            .Where(h => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == h.Id))
            .Select(h => new HomebrewHeroicAbilityDto(h.Code, h.Name, h.NameRu, h.Description, h.SafeDescription, h.Source,
                h.Requirement, h.ActivationCost, h.Activation, h.Duration, h.Frequency, h.Notes))
            .ToListAsync(ct);
        var archetypes = await db.ArchetypeDefs.AsNoTracking().Include(a => a.Abilities)
            .Where(a => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == a.Id))
            .Select(a => new HomebrewArchetypeDto(a.Code, a.Name, a.NameRu, a.Brawn, a.Agility, a.Intellect, a.Cunning,
                a.Willpower, a.Presence, a.WoundBase, a.StrainBase, a.StartingXp, a.Description, a.SafeDescription, a.Source,
                a.Abilities.Select(x => new HomebrewArchetypeAbilityDto(x.Code, x.NameRu, x.NameEn, x.SafeDescription)).ToList()))
            .ToListAsync(ct);
        var careers = await db.CareerDefs.AsNoTracking()
            .Where(c => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == c.Id))
            .Select(c => new HomebrewCareerDto(c.Code, c.Name, c.NameRu, c.Description, c.SafeDescription, c.Source,
                c.CareerSkillNames, c.StartingMoneyFixed, c.StartingMoneyDice))
            .ToListAsync(ct);
        var attachments = (await db.AttachmentDefs.AsNoTracking().Include(x => x.Effects)
            .Where(x => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == x.Id)).ToListAsync(ct))
            .Select(x => new HomebrewAttachmentDto(x.Code, x.Name, x.NameRu, x.HardPointCost, x.Price, x.Rarity, x.IsEnchantment, x.HostKind, x.RequiredTraits, x.RequiredAnyTraits, x.ForbiddenTraits, x.Effects.Select(v => new HomebrewAttachmentEffectDto(v.Kind, v.QualityCode, v.OppositeQualityCode, v.SkillName, v.Value, v.Increment, v.Condition, v.Note)).ToList(), x.Description, x.SafeDescription, x.DescriptionEn, x.Source)).ToList();
        var mounts = (await db.MountDefs.AsNoTracking().Include(x => x.Skills).Include(x => x.Abilities).Include(x => x.Attacks)
            .Where(x => db.HomebrewPackEntries.Any(e => e.HomebrewPackId == pack.Id && e.EntryId == x.Id)).ToListAsync(ct))
            .Select(x => new HomebrewMountDto(x.Code, x.Name, x.NameRu, x.TransportKind, x.MovementMode, x.RequiresTraction, x.Kind, x.Brawn, x.Agility, x.Intellect, x.Cunning, x.Willpower, x.Presence, x.Soak, x.WoundThreshold, x.StrainThreshold, x.MeleeDefense, x.RangedDefense, x.Silhouette, x.Capacity, x.Price, x.Rarity, x.IncludedGear, x.RequiresRidingCheck, x.Skills.Select(v => new HomebrewMountSkillDto(v.Name, v.Ranks, v.IsGroupSkill)).ToList(), x.Abilities.Select(v => new HomebrewMountAbilityDto(v.Name, v.NameRu, v.Description, v.DescriptionEn)).ToList(), x.Attacks.Select(v => new HomebrewMountAttackDto(v.Name, v.NameRu, v.SkillName, v.Damage, v.Critical, v.Range, v.QualityCodes)).ToList(), x.Description, x.SafeDescription, x.DescriptionEn, x.Source)).ToList();
        return new HomebrewPackExportDto(Format, pack.Name, pack.Description, pack.System, skills, talents, items, heroics, archetypes, careers,
            await db.HomebrewPackExclusions.Where(x => x.HomebrewPackId == pack.Id).Select(x => new BaseContentRef(x.Category, x.ContentKey)).ToListAsync(ct), attachments, mounts);
    }
}

internal static partial class HomebrewPackImporter
{
    public static async Task<HomebrewPackImportResult> ImportAsync(
        IAppDbContext db, Guid userId, HomebrewPackExportDto doc, CancellationToken ct)
    {
        if (doc.Format != HomebrewPackMapper.Format && doc.Format != "genesysforge.homebrew-pack.v1")
            throw new DomainRuleException("Неподдерживаемый формат homebrew-набора.");
        if (!Enum.IsDefined(doc.System)) throw new DomainRuleException("Неизвестная игровая система.");
        CreatePackHandler.Validate(doc.Name, ImportedDescription(doc.Description));
        if (string.IsNullOrWhiteSpace(doc.Name))
            throw new DomainRuleException("Название homebrew-набора не может быть пустым.");

        var pack = new HomebrewPack
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = doc.Name.Trim(),
            Description = ImportedDescription(doc.Description),
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
                Category = t.Category,
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

        foreach (var x in doc.Attachments ?? [])
        {
            RequireName(x.Name, "элемента");
            db.AttachmentDefs.Add(new AttachmentDef { Id = Guid.NewGuid(), System = doc.System, OwnerUserId = userId, HomebrewPackId = pack.Id, Code = Clean(x.Code), Name = Clean(x.Name), NameRu = Clean(x.NameRu), HardPointCost = x.HardPointCost, Price = x.Price, Rarity = x.Rarity, IsEnchantment = x.IsEnchantment, HostKind = x.HostKind, RequiredTraits = x.RequiredTraits, RequiredAnyTraits = x.RequiredAnyTraits, ForbiddenTraits = x.ForbiddenTraits, Effects = (x.Effects ?? []).Select(v => new AttachmentEffect { Id = Guid.NewGuid(), Kind = v.Kind, QualityCode = Clean(v.QualityCode), OppositeQualityCode = Clean(v.OppositeQualityCode), SkillName = Clean(v.SkillName), Value = v.Value, Increment = v.Increment, Condition = v.Condition, Note = Clean(v.Note) }).ToList(), Description = Clean(x.Description), SafeDescription = Clean(x.SafeDescription), DescriptionEn = Clean(x.DescriptionEn), Source = Clean(x.Source) });
            count++;
        }
        foreach (var x in doc.Mounts ?? [])
        {
            RequireName(x.Name, "элемента");
            db.MountDefs.Add(new MountDef { Id = Guid.NewGuid(), System = doc.System, OwnerUserId = userId, HomebrewPackId = pack.Id, Code = Clean(x.Code), Name = Clean(x.Name), NameRu = Clean(x.NameRu), TransportKind = x.TransportKind, MovementMode = x.MovementMode, RequiresTraction = x.RequiresTraction, Kind = x.Kind, Brawn = x.Brawn, Agility = x.Agility, Intellect = x.Intellect, Cunning = x.Cunning, Willpower = x.Willpower, Presence = x.Presence, Soak = x.Soak, WoundThreshold = x.WoundThreshold, StrainThreshold = x.StrainThreshold, MeleeDefense = x.MeleeDefense, RangedDefense = x.RangedDefense, Silhouette = x.Silhouette, Capacity = x.Capacity, Price = x.Price, Rarity = x.Rarity, IncludedGear = (x.IncludedGear ?? []).ToList(), RequiresRidingCheck = x.RequiresRidingCheck, Skills = (x.Skills ?? []).Select(v => new MountSkill { Id = Guid.NewGuid(), Name = Clean(v.Name), Ranks = v.Ranks, IsGroupSkill = v.IsGroupSkill }).ToList(), Abilities = (x.Abilities ?? []).Select(v => new MountAbility { Id = Guid.NewGuid(), Name = Clean(v.Name), NameRu = Clean(v.NameRu), Description = Clean(v.Description), DescriptionEn = Clean(v.DescriptionEn) }).ToList(), Attacks = (x.Attacks ?? []).Select(v => new MountAttack { Id = Guid.NewGuid(), Name = Clean(v.Name), NameRu = Clean(v.NameRu), SkillName = Clean(v.SkillName), Damage = v.Damage, Critical = v.Critical, Range = v.Range, QualityCodes = (v.QualityCodes ?? []).ToList() }).ToList(), Description = Clean(x.Description), SafeDescription = Clean(x.SafeDescription), DescriptionEn = Clean(x.DescriptionEn), Source = Clean(x.Source) });
            count++;
        }

        foreach (var e in db.SkillDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.Skill, e.Id);
        foreach (var e in db.TalentDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.Talent, e.Id);
        foreach (var e in db.ItemDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.Item, e.Id);
        foreach (var e in db.ArchetypeDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.Archetype, e.Id);
        foreach (var e in db.CareerDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.Career, e.Id);
        foreach (var e in db.HeroicAbilityDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.HeroicAbility, e.Id);
        foreach (var e in db.AttachmentDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.Attachment, e.Id);
        foreach (var e in db.MountDefs.Local.Where(x => x.HomebrewPackId == pack.Id).ToList()) AddEntry(CustomEntryType.Mount, e.Id);
        void AddEntry(CustomEntryType type, Guid id) => db.HomebrewPackEntries.Add(new HomebrewPackEntry { Id = Guid.NewGuid(), HomebrewPackId = pack.Id, EntryType = type, EntryId = id });
        List<string> warnings = [];
        var catalog = await BaseCatalog.LoadAsync(db, doc.System, ct);
        foreach (var item in (doc.Exclusions ?? []).Distinct())
        {
            if (!catalog.Any(x => x.Category == item.Category && x.Key == item.Key)) { warnings.Add($"Неизвестное ограничение пропущено: {item.Category}/{item.Key}"); continue; }
            db.HomebrewPackExclusions.Add(new HomebrewPackExclusion { Id = Guid.NewGuid(), HomebrewPackId = pack.Id, Category = item.Category, ContentKey = item.Key });
        }
        await db.SaveChangesAsync(ct);
        return new HomebrewPackImportResult(pack.Id, pack.Name, count, warnings);
    }

    private static void RequireName(string name, string label)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainRuleException($"Название {label} не может быть пустым.");
    }

    private static void ValidateCharacteristic(int value)
    {
        if (value is < 1 or > 5) throw new DomainRuleException("Характеристики архетипа должны быть от 1 до 5.");
    }

    private static string ImportedDescription(string? value)
    {
        var description = Clean(value);
        // These descriptions identify auto-created packs; imported copies are independent packs.
        return description.StartsWith("Personal custom:", StringComparison.Ordinal)
            || description.StartsWith("Campaign custom:", StringComparison.Ordinal) ? "" : description;
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
