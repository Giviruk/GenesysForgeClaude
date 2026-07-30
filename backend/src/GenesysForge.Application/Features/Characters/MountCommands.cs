using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Покупка или выдача скакуна (ROT-MOUNT-ITEM-01): создаётся существо, а не предмет.</summary>
public record BuyMountCommand(Guid UserId, Guid CharacterId, BuyMountRequest Request) : ICommand<Guid>;

/// <summary>Продажа скакуна по тем же трём способам, что и у предметов (ROT-ECO-01).</summary>
public record SellMountCommand(
    Guid UserId, Guid CharacterId, Guid MountId, SellMountRequest Request) : ICommand<Unit>;

/// <summary>Правка состояния скакуна: кличка, раны, груз, «под седлом», заметка.</summary>
public record UpdateMountCommand(
    Guid UserId, Guid CharacterId, Guid MountId, UpdateMountRequest Request) : ICommand<Unit>;

/// <summary>Удаление скакуна без выручки (погиб, отпущен, ошибка ввода).</summary>
public record RemoveMountCommand(Guid UserId, Guid CharacterId, Guid MountId) : ICommand<Unit>;
