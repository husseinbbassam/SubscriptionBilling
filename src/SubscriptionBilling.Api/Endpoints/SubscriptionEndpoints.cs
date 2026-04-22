using MediatR;
using SubscriptionBilling.Application.Subscriptions;

namespace SubscriptionBilling.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/subscriptions").WithTags("Subscriptions");

        group.MapPost("/", async (CreateSubscriptionRequest body, IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var key = ResolveIdempotencyKey(http);
            var response = await mediator.Send(
                new CreateSubscriptionCommand(body.CustomerId, body.PlanName, body.Price, body.Currency, body.BillingCycle, key),
                ct);
            return Results.Created($"/subscriptions/{response.SubscriptionId}", response);
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var key = ResolveIdempotencyKey(http);
            var response = await mediator.Send(new CancelSubscriptionCommand(id, key), ct);
            return Results.Ok(response);
        });
    }

    public sealed record CreateSubscriptionRequest(
        Guid CustomerId,
        string PlanName,
        decimal Price,
        string Currency,
        string BillingCycle);

    private static string ResolveIdempotencyKey(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }
        return Guid.NewGuid().ToString("N");
    }
}
