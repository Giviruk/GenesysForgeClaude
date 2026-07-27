using GenesysForge.Domain.Entities;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Восемь стандартных Secondary Effects героических способностей RoT. Каждый стоит 1 ability point,
/// не имеет рангов, и купить можно не более двух разных.
/// <para>
/// Описания — собственные парафразы (ROT-HA-CONTENT): <c>PrivateFull</c> обязан давать полный
/// механический смысл — дальность, момент срабатывания, цель и ограничения, — а не одно название.
/// <c>SafeDescription</c> остаётся коротким и чисел не раскрывает.
/// </para>
/// </summary>
public static class HeroicSecondaryEffectCatalog
{
    public static List<HeroicSecondaryEffectDef> Load() =>
    [
        Effect("devastating", "Devastating", "Сокрушительный",
            safe: "Усиливает одно попадание каждой атаки.",
            description:
                "Пока способность активна, к одному выбранному попаданию каждой атаки владельца "
                + "добавляется 2 урона. При промахе бонус не переносится на следующую атаку. Если атака "
                + "наносит несколько попаданий (Linked, автоматический огонь, несколько целей), бонус "
                + "получает ровно одно попадание этой атаки.",
            descriptionEn:
                "While the ability is active, add 2 damage to one chosen hit of each of the owner's attacks. "
                + "A miss does not bank the bonus for a later attack. If an attack scores several hits "
                + "(Linked, auto-fire, multiple targets), exactly one hit of that attack gets the bonus."),

        Effect("diminish", "Diminish", "Ослабление",
            safe: "Мешает проверкам противников рядом с владельцем.",
            description:
                "Пока способность активна, каждый противник, находящийся на короткой дистанции от "
                + "владельца в момент сбора пула, добавляет одну кость помех ко всем своим проверкам "
                + "навыков. Дальность проверяется на каждую проверку: уход за пределы короткой дистанции "
                + "немедленно снимает модификатор, возвращение — снова добавляет.",
            descriptionEn:
                "While the ability is active, every enemy within short range of the owner when the pool is "
                + "built adds one setback die to all of their skill checks. Range is checked per check: moving "
                + "beyond short range removes the modifier at once, moving back in reapplies it."),

        Effect("drain", "Drain", "Истощение",
            safe: "Изматывает противников рядом с владельцем.",
            description:
                "Сразу при активации и затем в начале каждого собственного хода владельца, пока "
                + "способность активна, каждый противник, находящийся в этот момент на короткой "
                + "дистанции, получает 2 усталости. Это отдельные периодические события, а не урон атаки: "
                + "поглощение не применяется. Срабатывание при активации происходит ровно один раз и не "
                + "повторяется началом того же хода.",
            descriptionEn:
                "Immediately on activation and then at the start of each of the owner's own turns while the "
                + "ability is active, every enemy within short range at that moment suffers 2 strain. These are "
                + "separate periodic events, not attack damage: soak does not apply. The activation tick happens "
                + "exactly once and is not repeated by the start of that same turn."),

        Effect("empowered", "Empowered", "Усиление",
            safe: "Усиливает проверки владельца.",
            description:
                "Пока способность активна, владелец добавляет одну кость подмоги ко всем своим проверкам "
                + "навыков.",
            descriptionEn:
                "While the ability is active, the owner adds one boost die to all of their skill checks."),

        Effect("empower-allies", "Empower Allies", "Усиление союзников",
            safe: "Усиливает проверки союзников рядом с владельцем.",
            description:
                "Пока способность активна, каждый союзник, находящийся на короткой дистанции от владельца "
                + "в момент сбора пула, добавляет одну кость подмоги к своим проверкам навыков. Сам "
                + "владелец своим союзником для этого эффекта не считается.",
            descriptionEn:
                "While the ability is active, every ally within short range of the owner when the pool is built "
                + "adds one boost die to their skill checks. The owner does not count as their own ally for this "
                + "effect."),

        Effect("rejuvenation", "Rejuvenation", "Восстановление",
            safe: "Снимает усталость с владельца.",
            description:
                "Сразу при активации и затем в начале каждого собственного хода владельца, пока "
                + "способность активна, владелец снимает 2 усталости, но не ниже нуля. Владелец без порога "
                + "усталости (приспешник или соперник) не лечит ничего — в лечение ран это не "
                + "превращается. Срабатывание при активации происходит один раз и не дублируется началом "
                + "того же хода.",
            descriptionEn:
                "Immediately on activation and then at the start of each of the owner's own turns while the "
                + "ability is active, the owner heals 2 strain, never below zero. An owner without a strain "
                + "threshold (a minion or rival) heals nothing — this never turns into wound healing. The "
                + "activation tick happens once and is not repeated by the start of that same turn."),

        Effect("rejuvenate-allies", "Rejuvenate Allies", "Восстановление союзников",
            safe: "Снимает усталость с союзников рядом с владельцем.",
            description:
                "При активации и затем в начале каждого собственного хода владельца все союзники, "
                + "находящиеся в этот момент на короткой дистанции, снимают 2 усталости, но не ниже нуля. "
                + "Союзник без порога усталости не лечит ничего; лечением ран этот эффект не становится.",
            descriptionEn:
                "On activation and then at the start of each of the owner's own turns while the ability is "
                + "active, every ally within short range at that moment heals 2 strain, never below zero. An "
                + "ally without a strain threshold heals nothing; this never becomes wound healing."),

        Effect("renewal", "Renewal", "Обновление",
            safe: "Добавляет группе слот инициативы.",
            description:
                "При активации в структурированной сцене владелец может бросить Хладнокровие или "
                + "Бдительность и создать один новый слот инициативы игроков с этим результатом. Слот "
                + "держится до конца сцены и доступен сразу, но не позволяет персонажу, уже сходившему в "
                + "текущем раунде, сходить второй раз — считаются именно уже отходившие участники, а не "
                + "число слотов. Каждая следующая законная активация может создать ещё один слот. Вне "
                + "структурированной сцены выбор остаётся отложенным и пропадает, если сцена не началась "
                + "до конца этой активации.",
            descriptionEn:
                "On activation during a structured encounter the owner may roll Cool or Vigilance and create one "
                + "new PC initiative slot with that result. The slot lasts until the end of the encounter and is "
                + "available at once, but it does not let a character who has already taken a turn this round take "
                + "a second one — what counts is which participants have already acted, not the number of slots. "
                + "Each later lawful activation may create one more slot. Outside a structured encounter the choice "
                + "stays pending and expires if no encounter starts before this activation ends."),
    ];

    private static HeroicSecondaryEffectDef Effect(
        string slug, string name, string nameRu, string safe, string description, string descriptionEn) => new()
    {
        Id = Guid.NewGuid(),
        Code = $"rot.heroic.secondary.{slug}",
        Name = name,
        NameRu = nameRu,
        Description = description,
        SafeDescription = safe,
        DescriptionEn = descriptionEn,
        Source = "Realms of Terrinoth, с. 79",
    };
}
