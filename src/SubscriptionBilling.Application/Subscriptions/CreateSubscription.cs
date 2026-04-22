using FluentValidation;
using MediatR;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Abstractions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Application.Subscriptions;

/// <summary>
/// Creates a subscription and immediately activates it. Activation generates
/// the first invoice (required domain rule). If you want a "pending" flow,
/// split this into two commands.
/// </summary>
public sealed record CreateSubscriptionCommand(
    Guid CustomerId,
    string PlanName,
    decimal Price,
    string Currency,
    string BillingCycle,
    string IdempotencyKey
) : IIdempotentCommand<CreateSubscriptionResponse>;

public sealed record CreateSubscriptionResponse(Guid SubscriptionId, Guid FirstInvoiceId);

public sealed class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PlanName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.BillingCycle).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}

public sealed class CreateSubscriptionHandler
    : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResponse>
{
    private readonly ICustomerRepository _customers;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public CreateSubscriptionHandler(
        ICustomerRepository customers,
        ISubscriptionRepository subscriptions,
        IInvoiceRepository invoices,
        IUnitOfWork uow,
        IClock clock)
    {
        _customers = customers;
        _subscriptions = subscriptions;
        _invoices = invoices;
        _uow = uow;
        _clock = clock;
    }

    public async Task<CreateSubscriptionResponse> Handle(CreateSubscriptionCommand request, CancellationToken ct)
    {
        var customerId = CustomerId.From(request.CustomerId);
        var customer = await _customers.GetAsync(customerId, ct)
            ?? throw new DomainException($"Customer {request.CustomerId} not found.");

        var subscription = Subscription.Create(
            customer.Id,
            request.PlanName,
            Money.Of(request.Price, request.Currency),
            BillingCycle.FromName(request.BillingCycle),
            _clock.UtcNow);

        var firstInvoice = subscription.Activate(_clock.UtcNow);

        await _subscriptions.AddAsync(subscription, ct);
        await _invoices.AddAsync(firstInvoice, ct);
        await _uow.SaveChangesAsync(ct);

        return new CreateSubscriptionResponse(subscription.Id.Value, firstInvoice.Id.Value);
    }
}
