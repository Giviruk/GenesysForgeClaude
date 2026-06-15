namespace GenesysForge.Domain;

/// <summary>Видимость NPC для участников кампании.</summary>
public enum NpcVisibility
{
    /// <summary>Виден только владельцу (мастеру).</summary>
    Private = 0,
    /// <summary>Виден участникам привязанной кампании.</summary>
    CampaignVisible = 1,
    /// <summary>Публичный шаблон-заготовка.</summary>
    PublicTemplate = 2,
}
