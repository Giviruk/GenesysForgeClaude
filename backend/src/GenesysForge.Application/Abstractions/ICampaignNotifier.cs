namespace GenesysForge.Application.Abstractions;

/// <summary>
/// Рассылка realtime-уведомлений подписчикам кампании (реализация — SignalR в Api).
/// События намеренно «тонкие»: клиент по событию перечитывает нужный REST-ресурс
/// (REST остаётся источником истины). No-op реализация по умолчанию, если хаб не подключён.
/// </summary>
public interface ICampaignNotifier
{
    /// <summary>Изменилась сцена Game Table кампании (участники/слоты/story points/...).</summary>
    Task GameTableChangedAsync(Guid campaignId);

    /// <summary>Изменился состав/заметки кампании.</summary>
    Task CampaignChangedAsync(Guid campaignId);

    /// <summary>В лог стола добавлен бросок (клиент перечитывает лог бросков).</summary>
    Task RollAddedAsync(Guid campaignId);
}
