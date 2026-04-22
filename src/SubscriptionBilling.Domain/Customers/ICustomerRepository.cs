namespace SubscriptionBilling.Domain.Customers;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task<Customer?> GetAsync(CustomerId id, CancellationToken ct = default);
}
