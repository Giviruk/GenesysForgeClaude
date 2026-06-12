using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record UpdateItemRequest(ItemState? State, int? Quantity);
