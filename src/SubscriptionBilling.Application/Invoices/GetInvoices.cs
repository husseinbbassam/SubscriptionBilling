using FluentValidation;
using MediatR;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;

namespace SubscriptionBilling.Application.Invoices;

public sealed record GetInvoicesQuery(Guid CustomerId) : IRequest<IReadOnlyList<InvoiceDto>>;

public sealed record InvoiceDto(
    Guid InvoiceId,
    Guid SubscriptionId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string Status,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    DateTime IssuedAtUtc,
    DateTime? PaidAtUtc);

public sealed class GetInvoicesValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
    }
}

public sealed class GetInvoicesHandler : IRequestHandler<GetInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoices;

    public GetInvoicesHandler(IInvoiceRepository invoices) => _invoices = invoices;

    public async Task<IReadOnlyList<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var invoices = await _invoices.GetByCustomerAsync(CustomerId.From(request.CustomerId), ct);

        return invoices
            .Select(i => new InvoiceDto(
                i.Id.Value,
                i.SubscriptionId.Value,
                i.CustomerId.Value,
                i.Amount.Amount,
                i.Amount.Currency,
                i.Status.ToString(),
                i.PeriodStartUtc,
                i.PeriodEndUtc,
                i.IssuedAtUtc,
                i.PaidAtUtc))
            .ToList();
    }
}
