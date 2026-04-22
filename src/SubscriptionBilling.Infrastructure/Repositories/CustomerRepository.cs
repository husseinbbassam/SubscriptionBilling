using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly BillingDbContext _db;

    public CustomerRepository(BillingDbContext db) => _db = db;

    public async Task AddAsync(Customer customer, CancellationToken ct = default)
    {
        await _db.Customers.AddAsync(customer, ct);
    }

    public Task<Customer?> GetAsync(CustomerId id, CancellationToken ct = default) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
}
