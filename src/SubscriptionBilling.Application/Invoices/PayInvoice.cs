using FluentValidation;
using MediatR;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Abstractions;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Application.Invoices;

public sealed record PayInvoiceCommand(Guid InvoiceId, string IdempotencyKey)
    : IIdempotentCommand<PayInvoiceResponse>;

public sealed record PayInvoiceResponse(Guid InvoiceId, DateTime PaidAtUtc);

public sealed class PayInvoiceValidator : AbstractValidator<PayInvoiceCommand>
{
    public PayInvoiceValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}

public sealed class PayInvoiceHandler : IRequestHandler<PayInvoiceCommand, PayInvoiceResponse>
{
    private readonly IInvoiceRepository _invoices;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public PayInvoiceHandler(IInvoiceRepository invoices, IUnitOfWork uow, IClock clock)
    {
        _invoices = invoices;
        _uow = uow;
        _clock = clock;
    }

    public async Task<PayInvoiceResponse> Handle(PayInvoiceCommand request, CancellationToken ct)
    {
        var id = InvoiceId.From(request.InvoiceId);
        var invoice = await _invoices.GetAsync(id, ct)
            ?? throw new DomainException($"Invoice {request.InvoiceId} not found.");

        invoice.Pay(_clock.UtcNow);
        await _uow.SaveChangesAsync(ct);

        return new PayInvoiceResponse(invoice.Id.Value, invoice.PaidAtUtc!.Value);
    }
}
