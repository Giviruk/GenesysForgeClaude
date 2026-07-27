using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class DuplicateCharacterHandler(IAppDbContext db) : ICommandHandler<DuplicateCharacterCommand, Guid>
{
    public async Task<Guid> Handle(DuplicateCharacterCommand command, CancellationToken ct = default)
    {
        var src = await db.GetOwnedAsync(command.UserId, command.CharacterId, tracking: false, ct);
        var now = DateTime.UtcNow;

        var copy = new Character
        {
            Id = Guid.NewGuid(),
            OwnerUserId = command.UserId,
            Name = $"{src.Name} (копия)",
            System = src.System,
            ArchetypeId = src.ArchetypeId,
            CareerId = src.CareerId,
            Brawn = src.Brawn,
            Agility = src.Agility,
            Intellect = src.Intellect,
            Cunning = src.Cunning,
            Willpower = src.Willpower,
            Presence = src.Presence,
            TotalXp = src.TotalXp,
            SpentXp = src.SpentXp,
            IsCreationPhase = src.IsCreationPhase,
            WoundsCurrent = src.WoundsCurrent,
            StrainCurrent = src.StrainCurrent,
            // Пороги — часть состояния персонажа, а не пересчитываемое значение: копия обязана
            // показывать те же числа, что и оригинал (ROT-CRE-02).
            CreationWoundThreshold = src.CreationWoundThreshold,
            CreationStrainThreshold = src.CreationStrainThreshold,
            ThresholdSnapshotProvenance = src.ThresholdSnapshotProvenance,
            RulesReviewRequired = src.RulesReviewRequired,
            Money = src.Money,
            SpeciesAbilityChoiceCode = src.SpeciesAbilityChoiceCode,
            StartingEquipmentMode = src.StartingEquipmentMode,
            StartingPurchaseBudget = src.StartingPurchaseBudget,
            HeroicAbilityId = src.HeroicAbilityId,
            HeroicUpgradeRank = src.HeroicUpgradeRank,
            HeroicDurationRanks = src.HeroicDurationRanks,
            HeroicFrequencyRanks = src.HeroicFrequencyRanks,
            HeroicStoryUpgrade = src.HeroicStoryUpgrade,
            // Личность копируется вместе со способностью (ROT-HA-01): у копии то же происхождение,
            // и она не превращается в персонажа с незаполненной личностью.
            HeroicCustomName = src.HeroicCustomName,
            HeroicOriginMode = src.HeroicOriginMode,
            HeroicOriginPrimary = src.HeroicOriginPrimary,
            HeroicOriginSecondary = src.HeroicOriginSecondary,
            HeroicOriginNarrative = src.HeroicOriginNarrative,
            HeroicOriginRolls = src.HeroicOriginRolls,
            // Параметр способности принадлежит копии так же, как и оригиналу (ROT-HA-02).
            HeroicConfiguration = src.HeroicConfiguration is null ? null : new CharacterHeroicConfiguration
            {
                Id = Guid.NewGuid(),
                ParagonSkillDefId = src.HeroicConfiguration.ParagonSkillDefId,
                ParagonSkillName = src.HeroicConfiguration.ParagonSkillName,
                SixthSenseSubject = src.HeroicConfiguration.SixthSenseSubject,
            },
            SignatureWeapon = src.SignatureWeapon is null ? null : new CharacterSignatureWeapon
            {
                Id = Guid.NewGuid(),
                Profile = src.SignatureWeapon.Profile,
                Craftsmanship = src.SignatureWeapon.Craftsmanship,
                NarrativeForm = src.SignatureWeapon.NarrativeForm,
                FormTraits = src.SignatureWeapon.FormTraits,
                IsLost = src.SignatureWeapon.IsLost,
            },
            Desire = src.Desire,
            Fear = src.Fear,
            Strength = src.Strength,
            Flaw = src.Flaw,
            Background = src.Background,
            CreatedAt = now,
            Skills = src.Skills.Select(s => new CharacterSkill
            {
                SkillDefId = s.SkillDefId,
                Ranks = s.Ranks,
                IsCareer = s.IsCareer,
                FreeRanks = s.FreeRanks,
            }).ToList(),
            Talents = src.Talents.Select(t => new CharacterTalent
            {
                TalentDefId = t.TalentDefId,
                Ranks = t.Ranks,
                GrantedCharacteristics = t.GrantedCharacteristics,
                NeedsChoice = t.NeedsChoice,
                Choices = t.Choices.Select(x => new CharacterTalentChoice
                {
                    Id = Guid.NewGuid(),
                    RankIndex = x.RankIndex,
                    Kind = x.Kind,
                    Value = x.Value,
                    DisplayName = x.DisplayName,
                }).ToList(),
            }).ToList(),
            Items = src.Items.Select(i => new CharacterItem
            {
                Id = Guid.NewGuid(),
                ItemDefId = i.ItemDefId,
                Quantity = i.Quantity,
                State = i.State,
                Provenance = i.Provenance,
            }).ToList(),
            CriticalInjuries = src.CriticalInjuries.Select(ci => new CharacterCriticalInjury
            {
                Id = Guid.NewGuid(),
                RuleCode = ci.RuleCode,
                NameRu = ci.NameRu,
                Severity = ci.Severity,
                RollResult = ci.RollResult,
                Notes = ci.Notes,
                CreatedAt = now,
            }).ToList(),
            HeroicSecondaryEffects = src.HeroicSecondaryEffects.Select(x => new CharacterHeroicSecondaryEffect
            {
                Id = Guid.NewGuid(),
                HeroicSecondaryEffectDefId = x.HeroicSecondaryEffectDefId,
            }).ToList(),
        };

        var notes = await db.CharacterNotes.AsNoTracking()
            .Where(n => n.CharacterId == src.Id)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(ct);
        foreach (var note in notes)
        {
            db.CharacterNotes.Add(new CharacterNote
            {
                Id = Guid.NewGuid(),
                CharacterId = copy.Id,
                OwnerUserId = command.UserId,
                Title = note.Title,
                Body = note.Body,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        db.Characters.Add(copy);
        await db.SaveChangesAsync(ct);
        return copy.Id;
    }
}
