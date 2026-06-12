using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record AddItemRequest(Guid ItemDefId, int Quantity, ItemState State);
