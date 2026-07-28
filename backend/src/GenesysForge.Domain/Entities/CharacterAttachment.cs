namespace GenesysForge.Domain.Entities;

/// <summary>
/// Экземпляр улучшения у персонажа (ROT-EQP-ATT-01). Пока <see cref="HostCharacterItemId"/> пуст,
/// улучшение лежит в запасе и ни на что не влияет; после установки оно принадлежит конкретному
/// предмету. Один экземпляр не может стоять на двух предметах — это одна строка.
/// </summary>
public class CharacterAttachment
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid AttachmentDefId { get; set; }
    public AttachmentDef? AttachmentDef { get; set; }

    /// <summary>Предмет-носитель; <c>null</c> — улучшение не установлено.</summary>
    public Guid? HostCharacterItemId { get; set; }

    /// <summary>Откуда улучшение появилось: покупка, выдача, импорт.</summary>
    public ItemProvenance Provenance { get; set; } = ItemProvenance.Purchased;

    /// <summary>
    /// Пометка ведущего: почему установка чар разрешена без магического навыка, чем кончилось
    /// снятие и подобное. Пусто — обычный случай.
    /// </summary>
    public string Note { get; set; } = "";
}
