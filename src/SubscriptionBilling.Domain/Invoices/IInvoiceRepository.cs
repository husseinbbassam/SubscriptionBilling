using SubscriptionBilling.Domain.Customers;

namespace SubscriptionBilling.Domain.Invoices;

public interface IInvoiceRepository
{
    Task AddAsync(Invoice invoice, CancellationToken ct = default);
    Task<Invoice?> GetAsync(InvoiceId id, CancellationToken ct = default);
    Task<IReadOnlyList<Invoice>> GetByCustomerAsync(CustomerId customerId, CancellationToken ct = default);
}
