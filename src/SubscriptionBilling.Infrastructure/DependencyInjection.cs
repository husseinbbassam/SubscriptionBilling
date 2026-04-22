using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Domain.Abstractions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Infrastructure.BackgroundJobs;
using SubscriptionBilling.Infrastructure.Clock;
using SubscriptionBilling.Infrastructure.Idempotency;
using SubscriptionBilling.Infrastructure.Outbox;
using SubscriptionBilling.Infrastructure.Persistence;
using SubscriptionBilling.Infrastructure.Repositories;

namespace SubscriptionBilling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<DomainEventsToOutboxInterceptor>();

        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("SubscriptionBilling");
            options.AddInterceptors(sp.GetRequiredService<DomainEventsToOutboxInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddHostedService<BillingCycleHostedService>();

        return services;
    }
}
