using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>
/// Узкие tracking-запросы для горячих путей правок персонажа. В отличие от полного
/// <c>WithRelations()</c> каждый сценарий грузит только данные, которые реально нужны его правилам.
/// </summary>
public static class CharacterMutationQueries
{
    /// <summary>
    /// Покупке предмета нужны только потенциальные строки для stacking, установленные улучшения
    /// этих строк и, если предмет сразу надевают, уже занятые слоты экипировки. Остальной граф
    /// персонажа (таланты, навыки, транспорт, героика и т. п.) в покупке не участвует.
    /// </summary>
    public static IQueryable<Character> AddItemQuery(
        this IAppDbContext db, Guid itemDefId, bool needsEquipmentValidation)
        => db.Characters
            .Include(c => c.Items.Where(i =>
                i.ItemDefId == itemDefId
                || (needsEquipmentValidation && i.State == ItemState.Equipped)))
                .ThenInclude(i => i.ItemDef)
            .Include(c => c.Attachments.Where(a => a.HostCharacterItemId != null));
}
