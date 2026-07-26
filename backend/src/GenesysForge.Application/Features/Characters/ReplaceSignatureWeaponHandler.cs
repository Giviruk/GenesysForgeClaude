using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Features.Characters;

public record ReplaceSignatureWeaponCommand(Guid UserId, Guid CharacterId, ReplaceSignatureWeaponRequest Request)
    : ICommand<Unit>;

/// <summary>
/// Отмечает именное оружие потерянным или возвращает/заменяет его (ROT-HA-02). Это отдельная
/// narrative-команда, доступная и после завершения создания: сам параметр остаётся неизменяемым,
/// меняется только судьба конкретного экземпляра. Строка одна, поэтому старое оружие и замена
/// не могут остаться активными одновременно.
/// </summary>
public class ReplaceSignatureWeaponHandler(IAppDbContext db) : ICommandHandler<ReplaceSignatureWeaponCommand, Unit>
{
    public async Task<Unit> Handle(ReplaceSignatureWeaponCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (c.RequiredHeroicParameter != HeroicParameterKind.SignatureWeapon)
            throw new DomainRuleException(
                "У персонажа нет способности с именным оружием.", "heroic.weapon.not_applicable");
        if (c.SignatureWeapon is not { } weapon)
            throw new DomainRuleException(
                "Именное оружие ещё не создано.", "heroic.weapon.missing");

        var req = command.Request;
        if (req.Lost)
        {
            weapon.IsLost = true;
        }
        else
        {
            // Возврат прежнего оружия — запрос без описания формы; замена — с новой формой целиком.
            var replacing = req.WeaponProfile is not null || req.Craftsmanship is not null
                || !string.IsNullOrWhiteSpace(req.NarrativeForm) || req.FormTraits is not null;
            if (replacing)
            {
                if (req.WeaponProfile is not { } profile || !Enum.IsDefined(profile))
                    throw new DomainRuleException(
                        "У замены должен быть профиль из таблицы.", "heroic.weapon.profile_required");
                if (req.Craftsmanship is not { } craftsmanship || !Enum.IsDefined(craftsmanship))
                    throw new DomainRuleException(
                        "У замены должно быть качество изготовления.", "heroic.weapon.craftsmanship_required");

                weapon.Profile = profile;
                weapon.Craftsmanship = craftsmanship;
                weapon.NarrativeForm = HeroicParameterRules.ValidateNarrativeForm(req.NarrativeForm);
                weapon.FormTraits = HeroicParameterRules.ValidateFormTraits(
                    profile, req.FormTraits ?? WeaponFormTraits.None);
            }
            weapon.IsLost = false;
        }

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.SignatureWeaponReplaced,
            weapon.IsLost ? "Именное оружие потеряно" : $"Именное оружие: «{weapon.NarrativeForm}»",
            data: new
            {
                lost = weapon.IsLost,
                profile = weapon.Profile.ToString(),
                craftsmanship = weapon.Craftsmanship.ToString(),
                form = weapon.NarrativeForm,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
