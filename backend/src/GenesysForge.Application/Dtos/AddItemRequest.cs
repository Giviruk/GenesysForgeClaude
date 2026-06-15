using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Добавление предмета. <paramref name="Cost"/> — сколько монет списать (покупка в «магазине»);
/// null или ≤0 — бесплатное добавление без изменения кошелька.
/// </summary>
public record AddItemRequest(Guid ItemDefId, int Quantity, ItemState State, int? Cost = null);

/// <summary>Продажа предмета: убрать <paramref name="Quantity"/> штук и зачислить <paramref name="Proceeds"/> монет.</summary>
public record SellItemRequest(int Quantity, int Proceeds);
