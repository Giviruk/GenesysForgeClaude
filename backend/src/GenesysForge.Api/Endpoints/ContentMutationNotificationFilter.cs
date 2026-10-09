using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;

namespace GenesysForge.Api.Endpoints;

/// <summary>Capture connections before deletion, and notify only after a successful mutation.</summary>
public class ContentMutationNotificationFilter(IAppDbContext db, ICampaignNotifier notifier) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var targets = await ContentNotificationTargets.ForAuthorAsync(db, context.HttpContext.User.UserId(), context.HttpContext.RequestAborted);
        if (Guid.TryParse(request.RouteValues["campaignId"]?.ToString(), out var campaignId)) targets.Add(campaignId);
        var result = await next(context);
        foreach (var id in targets.Distinct()) await notifier.CampaignChangedAsync(id);
        return result;
    }
}
