using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public record SetHeroicConfigurationCommand(Guid UserId, Guid CharacterId, SetHeroicConfigurationRequest Request)
    : ICommand<Unit>;

/// <summary>
/// Выбирает параметр primary effect (ROT-HA-02): навык Paragon, категорию Sixth Sense либо
/// профиль именного оружия. Параметр обязателен до завершения создания и после него не меняется.
/// </summary>
public class SetHeroicConfigurationHandler(IAppDbContext db) : ICommandHandler<SetHeroicConfigurationCommand, Unit>
{
    public async Task<Unit> Handle(SetHeroicConfigurationCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        HeroicConfigurationGate.EnsureEditable(c);

        var req = command.Request;
        var kind = c.RequiredHeroicParameter;
        if (kind == HeroicParameterKind.None)
            throw new DomainRuleException(
                "Выбранная героическая способность не требует параметра.", "heroic.parameter.not_applicable");

        switch (kind)
        {
            case HeroicParameterKind.ParagonSkill:
                await ApplyParagonAsync(db, c, req, command.UserId, ct);
                break;
            case HeroicParameterKind.SixthSenseSubject:
                ApplySixthSense(db, c, req);
                break;
            case HeroicParameterKind.SignatureWeapon:
                ApplySignatureWeapon(db, c, req);
                break;
        }

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.HeroicParameterSet,
            $"Параметр героической способности: {Describe(c)}",
            data: new { kind = kind.ToString(), summary = Describe(c) });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static async Task ApplyParagonAsync(
        IAppDbContext db, Character c, SetHeroicConfigurationRequest req, Guid userId, CancellationToken ct)
    {
        RejectForeignFields(req, allowParagon: true);
        if (req.ParagonSkillDefId is not { } skillId)
            throw new DomainRuleException(
                "Выберите навык, с которым работает способность.", "heroic.parameter.skill_required");

        // Навык должен быть доступен именно этому персонажу: встроенный или собственный кастом
        // той же системы из видимого набора. Чужой id не должен проходить по одному только Guid.
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(db, userId, c.System, c.Id, ct: ct);
        var skill = await db.SkillDefs.FirstOrDefaultAsync(s =>
            s.Id == skillId
            && s.System == c.System
            && (s.OwnerUserId == null
                || (s.OwnerUserId == userId
                    && (s.HomebrewPackId == null || visiblePackIds.Contains(s.HomebrewPackId.Value)))), ct);
        if (skill is null)
            throw new DomainRuleException("Навык недоступен персонажу.", "heroic.parameter.skill_not_available");

        var config = EnsureConfiguration(db, c);
        config.ParagonSkillDefId = skill.Id;
        config.ParagonSkillDef = skill;
        config.ParagonSkillName = skill.Name;
        config.SixthSenseSubject = "";
    }

    private static void ApplySixthSense(IAppDbContext db, Character c, SetHeroicConfigurationRequest req)
    {
        RejectForeignFields(req, allowSubject: true);
        var subject = HeroicParameterRules.ValidateSixthSenseSubject(req.SixthSenseSubject);

        var config = EnsureConfiguration(db, c);
        config.SixthSenseSubject = subject;
        config.ParagonSkillDefId = null;
        config.ParagonSkillDef = null;
        config.ParagonSkillName = "";
    }

    private static void ApplySignatureWeapon(IAppDbContext db, Character c, SetHeroicConfigurationRequest req)
    {
        RejectForeignFields(req, allowWeapon: true);
        if (req.WeaponProfile is not { } profile)
            throw new DomainRuleException(
                "Выберите профиль именного оружия.", "heroic.weapon.profile_required");
        if (req.Craftsmanship is not { } craftsmanship || !Enum.IsDefined(craftsmanship))
            throw new DomainRuleException(
                "Выберите качество изготовления именного оружия.", "heroic.weapon.craftsmanship_required");
        if (!Enum.IsDefined(profile))
            throw new DomainRuleException(
                "Неизвестный профиль именного оружия.", "heroic.weapon.profile_unknown");

        var form = HeroicParameterRules.ValidateNarrativeForm(req.NarrativeForm);
        var traits = HeroicParameterRules.ValidateFormTraits(profile, req.FormTraits ?? WeaponFormTraits.None);

        // Оружие не более одного: существующая строка переписывается, вторая не создаётся.
        var weapon = c.SignatureWeapon;
        if (weapon is null)
        {
            weapon = new CharacterSignatureWeapon { Id = Guid.NewGuid(), CharacterId = c.Id };
            c.SignatureWeapon = weapon;
            db.CharacterSignatureWeapons.Add(weapon);
        }
        weapon.Profile = profile;
        weapon.Craftsmanship = craftsmanship;
        weapon.NarrativeForm = form;
        weapon.FormTraits = traits;
        weapon.IsLost = false;
    }

    private static CharacterHeroicConfiguration EnsureConfiguration(IAppDbContext db, Character c)
    {
        if (c.HeroicConfiguration is not null) return c.HeroicConfiguration;
        var config = new CharacterHeroicConfiguration { Id = Guid.NewGuid(), CharacterId = c.Id };
        c.HeroicConfiguration = config;
        db.CharacterHeroicConfigurations.Add(config);
        return config;
    }

    /// <summary>Поля чужого параметра — ошибка, а не мусор: молча игнорировать их нельзя.</summary>
    private static void RejectForeignFields(
        SetHeroicConfigurationRequest req,
        bool allowParagon = false, bool allowSubject = false, bool allowWeapon = false)
    {
        var hasParagon = req.ParagonSkillDefId is not null;
        var hasSubject = !string.IsNullOrWhiteSpace(req.SixthSenseSubject);
        var hasWeapon = req.WeaponProfile is not null || req.Craftsmanship is not null
            || !string.IsNullOrWhiteSpace(req.NarrativeForm) || req.FormTraits is not null;

        if ((hasParagon && !allowParagon) || (hasSubject && !allowSubject) || (hasWeapon && !allowWeapon))
            throw new DomainRuleException(
                "Запрос содержит параметры другой героической способности.",
                "heroic.parameter.foreign_field");
    }

    private static string Describe(Character c) => c.RequiredHeroicParameter switch
    {
        HeroicParameterKind.ParagonSkill => c.HeroicConfiguration?.ParagonSkillName ?? "",
        HeroicParameterKind.SixthSenseSubject => c.HeroicConfiguration?.SixthSenseSubject ?? "",
        HeroicParameterKind.SignatureWeapon => c.SignatureWeapon?.NarrativeForm ?? "",
        _ => "",
    };
}
