using MediatR;
using SubscriptionBilling.Domain.Abstractions;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Application.Billing;

/// <summary>
/// Issued by the billing background job. Finds every Active subscription whose
/// period has rolled over and issues the next invoice. Intentionally not an
/// IIdempotentCommand — the domain logic itself is safe to re-run (each call
/// only issues invoices for periods that are actually due).
/// </summary>
public sealed record RunBillingCycleCommand : IRequest<RunBillingCycleResponse>;

public sealed record RunBillingCycleResponse(int InvoicesGenerated);

public sealed class RunBillingCycleHandler : IRequestHandler<RunBillingCycleCommand, RunBillingCycleResponse>
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public RunBillingCycleHandler(
        ISubscriptionRepository subscriptions,
        IInvoiceRepository invoices,
        IUnitOfWork uow,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _invoices = invoices;
        _uow = uow;
        _clock = clock;
    }

    public async Task<RunBillingCycleResponse> Handle(RunBillingCycleCommand request, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var due = await _subscriptions.GetDueForBillingAsync(now, ct);
        var generated = 0;

        foreach (var subscription in due)
        {
            // A subscription may have multiple periods overdue (e.g. job was
            // offline for days). Roll forward until caught up.
            while (true)
            {
                var invoice = subscription.GenerateNextInvoice(now);
                if (invoice is null) break;
                await _invoices.AddAsync(invoice, ct);
                generated++;
            }
        }

        if (generated > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }

        return new RunBillingCycleResponse(generated);
    }
}
