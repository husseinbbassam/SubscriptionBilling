using MediatR;
using SubscriptionBilling.Application.Invoices;

namespace SubscriptionBilling.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder routes)
    {
        var invoices = routes.MapGroup("/invoices").WithTags("Invoices");

        invoices.MapPost("/{id:guid}/pay", async (Guid id, IMediator mediator, HttpContext http, CancellationToken ct) =>
        {
            var key = ResolveIdempotencyKey(http);
            var response = await mediator.Send(new PayInvoiceCommand(id, key), ct);
            return Results.Ok(response);
        });

        var customers = routes.MapGroup("/customers/{customerId:guid}/invoices").WithTags("Invoices");

        customers.MapGet("/", async (Guid customerId, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.Send(new GetInvoicesQuery(customerId), ct);
            return Results.Ok(response);
        });
    }

    private static string ResolveIdempotencyKey(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }
        return Guid.NewGuid().ToString("N");
    }
}
