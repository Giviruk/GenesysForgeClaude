using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Characters;

namespace GenesysForge.Api.Endpoints;

/// <summary>
/// Отдаёт обновлённые части листа прямо в ответе на правку — те, которые клиент назвал заголовком
/// <see cref="HeaderName"/>.
///
/// <para>
/// Зачем: интерфейс после каждой правки всё равно перечитывает лист, и это стоит целого лишнего
/// обращения к серверу — на развёрнутом приложении четверть-полсекунды даже при уже установленном
/// соединении.
/// </para>
///
/// <para>
/// Почему части, а не лист целиком: правка на вкладке «Лист» не требует ни инвентаря, ни талантов,
/// а это три четверти веса ответа. Клиент называет то, что у него сейчас на экране
/// (<c>X-Return-Slices: base,items</c>), и получает ровно это; остальное он у себя помечает
/// устаревшим и перечитает, когда откроет соответствующую вкладку.
/// </para>
///
/// <para>
/// Сделано опт-ином, а не сменой контракта: без заголовка все маршруты группы отвечают
/// <c>204 No Content</c> ровно как раньше. Поэтому старые клиенты и существующие тесты статусов
/// продолжают работать без единой правки.
/// </para>
/// </summary>
public static class ReturnSheetFilter
{
    /// <summary>Заголовок-просьба вернуть названные части листа вместе с ответом на правку.</summary>
    public const string HeaderName = "X-Return-Slices";

    /// <summary>
    /// Вешается на всю группу персонажей. Срабатывает только когда совпало всё сразу: клиент
    /// попросил, маршрут относится к конкретному персонажу и правка удалась — то есть ответ либо
    /// <c>204 No Content</c>, либо <c>201 Created</c> о записи внутри этого же персонажа
    /// (<see cref="CreatedInCharacterResponse"/>). Идентификатор созданного при этом не теряется —
    /// он уезжает в <see cref="SheetSlicesDto.CreatedId"/>.
    ///
    /// <para>
    /// Остальные ответы проходят нетронутыми: <c>duplicate</c> и <c>import</c> тоже отвечают
    /// <c>201 Created</c>, но создают <i>другого</i> персонажа — подменять их частями исходного
    /// листа нельзя.
    /// </para>
    /// </summary>
    public static async ValueTask<object?> Apply(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        var http = context.HttpContext;
        if (!http.Request.Headers.TryGetValue(HeaderName, out var requested)) return result;

        // Создало ли что-то: у обычной правки — нет, у покупки предмета или транспорта — да.
        Guid? createdId = null;
        if (result is IValueHttpResult { Value: CreatedInCharacterResponse created })
            createdId = created.Id;
        // Всё остальное с телом ответа проходит нетронутым: подменять его нечем и незачем.
        else if (result is not IStatusCodeHttpResult { StatusCode: StatusCodes.Status204NoContent })
            return result;

        if (http.Request.RouteValues["id"] is not string raw || !Guid.TryParse(raw, out var id))
            return result;

        try
        {
            var slices = SheetSlices.Parse(requested);
            var handler = http.RequestServices
                .GetRequiredService<IQueryHandler<GetCharacterSlicesQuery, SheetSlicesDto>>();
            var built = await handler.Handle(
                new GetCharacterSlicesQuery(http.User.UserId(), id, slices), http.RequestAborted);
            return Results.Ok(createdId is null ? built : built with { CreatedId = createdId });
        }
        catch (Exception)
        {
            // Правка уже сохранена и удалась. Если собрать срез не вышло — например, персонажа
            // только что удалили этой же командой, — отдаём исходный ответ: удобство ответа не
            // повод превращать успешную запись в ошибку. Клиент тогда перечитает лист сам.
            return result;
        }
    }
}
