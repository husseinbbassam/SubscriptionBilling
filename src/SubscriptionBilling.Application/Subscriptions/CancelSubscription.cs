using FluentValidation;
using MediatR;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Abstractions;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Application.Subscriptions;

public sealed record CancelSubscriptionCommand(Guid SubscriptionId, string IdempotencyKey)
    : IIdempotentCommand<CancelSubscriptionResponse>;

public sealed record CancelSubscriptionResponse(Guid SubscriptionId, DateTime CanceledAtUtc);

public sealed class CancelSubscriptionValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}

public sealed class CancelSubscriptionHandler
    : IRequestHandler<CancelSubscriptionCommand, CancelSubscriptionResponse>
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public CancelSubscriptionHandler(
        ISubscriptionRepository subscriptions,
        IUnitOfWork uow,
        IClock clock)
    {
        _subscriptions = subscriptions;
        _uow = uow;
        _clock = clock;
    }

    public async Task<CancelSubscriptionResponse> Handle(CancelSubscriptionCommand request, CancellationToken ct)
    {
        var id = SubscriptionId.From(request.SubscriptionId);
        var subscription = await _subscriptions.GetAsync(id, ct)
            ?? throw new DomainException($"Subscription {request.SubscriptionId} not found.");

        subscription.Cancel(_clock.UtcNow);
        await _uow.SaveChangesAsync(ct);

        return new CancelSubscriptionResponse(subscription.Id.Value, subscription.CanceledAtUtc!.Value);
    }
}
