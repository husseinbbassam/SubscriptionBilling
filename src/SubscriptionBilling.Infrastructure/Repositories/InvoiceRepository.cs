using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly BillingDbContext _db;

    public InvoiceRepository(BillingDbContext db) => _db = db;

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        await _db.Invoices.AddAsync(invoice, ct);
    }

    public Task<Invoice?> GetAsync(InvoiceId id, CancellationToken ct = default) =>
        _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Invoice>> GetByCustomerAsync(
        CustomerId customerId,
        CancellationToken ct = default)
    {
        var list = await _db.Invoices
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .ToListAsync(ct);
        return list;
    }
}
