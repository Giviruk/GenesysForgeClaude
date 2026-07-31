using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Characters;

namespace GenesysForge.Api.Endpoints;

/// <summary>
/// Отдаёт обновлённый лист прямо в ответе на правку персонажа, если клиент об этом попросил
/// заголовком <see cref="HeaderName"/>.
///
/// <para>
/// Зачем: интерфейс после каждой правки всё равно перечитывает лист, и это стоит целого лишнего
/// обращения к серверу — на развёрнутом приложении четверть-полсекунды даже при уже установленном
/// соединении. Лист собирается тем же самым query-обработчиком, что и обычный GET, так что второй
/// раз он не считается — просто уезжает вместе с ответом на правку.
/// </para>
///
/// <para>
/// Сделано опт-ином, а не сменой контракта: без заголовка все 36 маршрутов группы отвечают
/// <c>204 No Content</c> ровно как раньше. Поэтому старые клиенты и существующие тесты статусов
/// продолжают работать без единой правки.
/// </para>
/// </summary>
public static class ReturnSheetFilter
{
    /// <summary>Заголовок-просьба вернуть лист вместе с ответом на правку.</summary>
    public const string HeaderName = "X-Return-Sheet";

    /// <summary>
    /// Вешается на всю группу персонажей. Срабатывает только когда совпало всё сразу: клиент
    /// попросил, маршрут относится к конкретному персонажу и обработчик ответил <c>204</c>.
    /// Остальные ответы (<c>201 Created</c>, тела запросов, ошибки) проходят нетронутыми.
    /// </summary>
    public static async ValueTask<object?> Apply(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        var http = context.HttpContext;
        if (!http.Request.Headers.ContainsKey(HeaderName)) return result;
        // Правка удалась, но ответа с телом не было — только такой случай и подменяем.
        if (result is not IStatusCodeHttpResult { StatusCode: StatusCodes.Status204NoContent })
            return result;
        if (http.Request.RouteValues["id"] is not string raw || !Guid.TryParse(raw, out var id))
            return result;

        try
        {
            var handler = http.RequestServices
                .GetRequiredService<IQueryHandler<GetCharacterSheetQuery, CharacterSheetDto>>();
            var sheet = await handler.Handle(
                new GetCharacterSheetQuery(http.User.UserId(), id), http.RequestAborted);
            return Results.Ok(sheet);
        }
        catch (Exception)
        {
            // Правка уже сохранена и удалась. Если собрать лист не вышло — например, персонажа
            // только что удалили этой же командой, — отдаём исходный 204: удобство ответа не повод
            // превращать успешную запись в ошибку. Клиент в таком случае перечитает лист сам.
            return result;
        }
    }
}
