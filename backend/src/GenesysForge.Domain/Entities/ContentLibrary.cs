namespace GenesysForge.Domain.Entities;

public enum CustomEntryType { Skill, Talent, Item, Archetype, Career, HeroicAbility, Attachment, Mount }
public enum BaseContentCategory { Skill, Career, Archetype, Talent, Magic, HeroicAbility, Item }
public enum ContentUpdatePolicy { Auto, Manual }
public enum ContentConnectionStatus { Active, Pending, Declined }
public enum PackEntryState { Disabled, Pending }

/// <summary>Membership only: definitions retain their own author and stable identity.</summary>
public class HomebrewPackEntry
{
    public Guid Id { get; set; }
    public Guid HomebrewPackId { get; set; }
    public CustomEntryType EntryType { get; set; }
    public Guid EntryId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class HomebrewPackExclusion
{
    public Guid Id { get; set; }
    public Guid HomebrewPackId { get; set; }
    public BaseContentCategory Category { get; set; }
    public required string ContentKey { get; set; }
}

public class CampaignPackEntryState
{
    public Guid Id { get; set; }
    public Guid HomebrewPackCampaignId { get; set; }
    public CustomEntryType EntryType { get; set; }
    public Guid EntryId { get; set; }
    public PackEntryState State { get; set; }
}

public class CampaignContentItem
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public CustomEntryType EntryType { get; set; }
    public Guid EntryId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ContentConnectionStatus Status { get; set; } = ContentConnectionStatus.Active;
    public Guid? ProposedByUserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class CampaignSystemSetting
{
    public Guid CampaignId { get; set; }
    public GameSystem System { get; set; }
    public bool IsOpen { get; set; } = true;
}

public class CampaignBaseOverride
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public GameSystem System { get; set; }
    public BaseContentCategory Category { get; set; }
    public required string ContentKey { get; set; }
    public bool IsEnabled { get; set; }
}
