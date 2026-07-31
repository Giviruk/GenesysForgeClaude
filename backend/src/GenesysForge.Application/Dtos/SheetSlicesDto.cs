namespace GenesysForge.Application.Dtos;

/// <summary>
/// Ответ, несущий только запрошенные части листа. Незапрошенное остаётся <c>null</c> — это
/// «не загружено», а не «пусто»: пустой список значит, что предметов (талантов, транспорта) у
/// персонажа действительно нет, и вкладка нарисует «Пусто», а не спиннер.
///
/// <para>
/// Одним и тем же типом отвечают и чтение вкладки, и правка: клиент в обоих случаях просто
/// накладывает пришедшие срезы на то, что у него уже есть.
/// </para>
/// </summary>
public record SheetSlicesDto(
    /// <summary>Характеристики, пороги, навыки, героика, деньги, опыт — без тяжёлых коллекций.</summary>
    CharacterSheetDto? Base = null,
    List<CharacterItemDto>? Items = null,
    List<CharacterTalentDto>? Talents = null,
    Dictionary<int, int>? TalentTierCounts = null,
    IReadOnlyList<CharacterMountDto>? Mounts = null,
    IReadOnlyList<CharacterAttachmentDto>? Attachments = null,
    /// <summary>
    /// Идентификатор только что созданной записи — у маршрутов, которые её создают (покупка
    /// предмета, транспорта, улучшения). Иначе за ним пришлось бы оставлять отдельный ответ, а
    /// вместе с ним и второй запрос за листом.
    /// </summary>
    Guid? CreatedId = null);
