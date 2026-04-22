using MediatR;
using SubscriptionBilling.Application.Customers;

namespace SubscriptionBilling.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/customers").WithTags("Customers");

        group.MapPost("/", async (CreateCustomerRequest body, IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var key = ResolveIdempotencyKey(http);
            var response = await mediator.Send(new CreateCustomerCommand(body.Name, body.Email, key), ct);
            return Results.Created($"/customers/{response.CustomerId}", response);
        });
    }

    public sealed record CreateCustomerRequest(string Name, string Email);

    private static string ResolveIdempotencyKey(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }
        // Fall back to a per-request key so the command still works, but
        // clients should supply one for real deduplication.
        return Guid.NewGuid().ToString("N");
    }
}
