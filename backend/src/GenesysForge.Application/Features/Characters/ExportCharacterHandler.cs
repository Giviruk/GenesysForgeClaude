using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class ExportCharacterHandler(IAppDbContext db) : IQueryHandler<ExportCharacterQuery, CharacterExportDto>
{
    public async Task<CharacterExportDto> Handle(ExportCharacterQuery query, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(query.UserId, query.CharacterId, tracking: false, ct);

        var notes = await db.CharacterNotes
            .Where(n => n.CharacterId == c.Id)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new CharacterNoteExport(n.Title, n.Body))
            .ToListAsync(ct);

        // Транспорт нумеруется один раз, и по этим индексам ссылаются груз и тяга: id экземпляра в
        // чужом аккаунте не существует (ROT-TRANSPORT-01).
        var mounts = c.Mounts
            .Where(m => m.MountDef is not null)
            .OrderBy(m => m.CreatedAt)
            .ToList();
        var mountIndex = mounts
            .Select((m, i) => (m.Id, Index: i))
            .ToDictionary(x => x.Id, x => x.Index);

        var data = new CharacterExportData(
            Name: c.Name,
            System: c.System,
            ArchetypeCode: c.Archetype?.Code ?? "",
            ArchetypeName: c.Archetype?.Name ?? "",
            CareerCode: c.Career?.Code ?? "",
            CareerName: c.Career?.Name ?? "",
            Characteristics: new Dictionary<string, int>
            {
                ["brawn"] = c.Brawn,
                ["agility"] = c.Agility,
                ["intellect"] = c.Intellect,
                ["cunning"] = c.Cunning,
                ["willpower"] = c.Willpower,
                ["presence"] = c.Presence,
            },
            TotalXp: c.TotalXp,
            SpentXp: c.SpentXp,
            Money: c.Money,
            IsCreationPhase: c.IsCreationPhase,
            WoundsCurrent: c.WoundsCurrent,
            StrainCurrent: c.StrainCurrent,
            Skills: c.Skills
                .Select(s => new CharacterSkillExport(s.SkillDef?.Code ?? "", s.SkillDef?.Name ?? "", s.Ranks, s.IsCareer, s.FreeRanks))
                .ToList(),
            Talents: c.Talents
                .Select(t => new CharacterTalentExport(t.TalentDef?.Code ?? "", t.TalentDef?.Name ?? "", t.Ranks,
                    t.GrantedCharacteristics,
                    t.Choices.OrderBy(x => x.RankIndex)
                        .Select(x => new CharacterTalentChoiceExport(x.RankIndex, x.Kind, x.Value, x.DisplayName))
                        .ToList(),
                    t.NeedsChoice))
                .ToList(),
            Items: c.Items
                .Select(i => new CharacterItemExport(
                    i.ItemDef?.Code ?? "", i.ItemDef?.Name ?? "", i.Quantity, i.State,
                    i.Provenance, i.Craftsmanship, i.DamageState, i.ImplementMaterial,
                    i.ImplementChoices, i.ImplementConfigured,
                    i.ShardActivationChoice, i.ShardEffectAction, i.ShardEffectChoice,
                    i.ShardConfigured,
                    i.CarriedByMountId is { } id && mountIndex.TryGetValue(id, out var idx)
                        ? idx
                        : null,
                    i.IsInstalledOnMount))
                .ToList(),
            HeroicAbilityCode: c.HeroicAbility?.Code,
            HeroicAbilityName: c.HeroicAbility?.Name,
            HeroicUpgradeRank: c.HeroicUpgradeRank,
            Notes: notes,
            HeroicDurationRanks: c.HeroicDurationRanks,
            HeroicFrequencyRanks: c.HeroicFrequencyRanks,
            HeroicStoryUpgrade: c.HeroicStoryUpgrade,
            HeroicSecondaryEffectCodes: c.HeroicSecondaryEffects
                .Where(x => x.HeroicSecondaryEffectDef is not null)
                .Select(x => x.HeroicSecondaryEffectDef!.Code)
                .ToList(),
            CreationWoundThreshold: c.CreationWoundThreshold,
            CreationStrainThreshold: c.CreationStrainThreshold,
            ThresholdSnapshotProvenance: c.ThresholdSnapshotProvenance,
            RulesReviewRequired: c.RulesReviewRequired,
            StartingEquipmentMode: c.StartingEquipmentMode,
            StartingPurchaseBudget: c.StartingPurchaseBudget,
            SpeciesAbilityChoiceCode: c.SpeciesAbilityChoiceCode,
            HeroicCustomName: c.HeroicCustomName,
            HeroicOriginMode: c.HeroicOriginMode,
            HeroicOriginPrimary: c.HeroicOriginPrimary,
            HeroicOriginSecondary: c.HeroicOriginSecondary,
            HeroicOriginNarrative: c.HeroicOriginNarrative,
            HeroicOriginRolls: [.. HeroicIdentityRules.ParseRolls(c.HeroicOriginRolls)],
            ParagonSkillCode: c.HeroicConfiguration?.ParagonSkillDef?.Code,
            ParagonSkillName: c.HeroicConfiguration?.ParagonSkillName,
            SixthSenseSubject: c.HeroicConfiguration?.SixthSenseSubject,
            SignatureWeaponProfile: c.SignatureWeapon?.Profile,
            SignatureWeaponCraftsmanship: c.SignatureWeapon?.Craftsmanship,
            SignatureWeaponForm: c.SignatureWeapon?.NarrativeForm,
            SignatureWeaponTraits: c.SignatureWeapon?.FormTraits,
            SignatureWeaponLost: c.SignatureWeapon?.IsLost ?? false,
            Mounts: mounts
                .Select(m => new CharacterMountExport(
                    m.MountDef!.Code, m.MountDef.Name, m.Name, m.WoundsCurrent, CarriedLoad: 0,
                    m.IsActive, m.Notes, m.Provenance,
                    m.DrawnByMountId is { } drawnBy && mountIndex.TryGetValue(drawnBy, out var drawnIdx)
                        ? drawnIdx
                        : null))
                .ToList());

        return new CharacterExportDto(CharacterExportDto.CurrentFormat, DateTime.UtcNow, data);
    }
}
