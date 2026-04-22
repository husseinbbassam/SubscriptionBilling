using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.ValueObjects;
using SubscriptionBilling.Infrastructure.Idempotency;
using SubscriptionBilling.Infrastructure.Outbox;

namespace SubscriptionBilling.Infrastructure.Persistence;

public sealed class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyEntry> IdempotencyEntries => Set<IdempotencyEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCustomers(modelBuilder);
        ConfigureSubscriptions(modelBuilder);
        ConfigureInvoices(modelBuilder);
        ConfigureOutbox(modelBuilder);
        ConfigureIdempotency(modelBuilder);
    }

    private static void ConfigureCustomers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id)
                .HasConversion(id => id.Value, guid => CustomerId.From(guid));
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.OwnsOne(c => c.Email, owned =>
            {
                owned.Property(p => p.Value).HasColumnName("Email").IsRequired().HasMaxLength(320);
            });
            e.Ignore(c => c.DomainEvents);
        });
    }

    private static void ConfigureSubscriptions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(e =>
        {
            e.ToTable("Subscriptions");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id)
                .HasConversion(id => id.Value, guid => SubscriptionId.From(guid));
            e.Property(s => s.CustomerId)
                .HasConversion(id => id.Value, guid => CustomerId.From(guid))
                .IsRequired();
            e.Property(s => s.PlanName).IsRequired().HasMaxLength(100);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(s => s.BillingCycle)
                .HasConversion(v => v.Name, s => BillingCycle.FromName(s))
                .HasMaxLength(20);
            e.OwnsOne(s => s.Price, owned =>
            {
                owned.Property(p => p.Amount).HasColumnName("PriceAmount").HasColumnType("decimal(18,2)");
                owned.Property(p => p.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
            });
            e.Ignore(s => s.DomainEvents);
        });
    }

    private static void ConfigureInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>(e =>
        {
            e.ToTable("Invoices");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id)
                .HasConversion(id => id.Value, guid => InvoiceId.From(guid));
            e.Property(i => i.CustomerId)
                .HasConversion(id => id.Value, guid => CustomerId.From(guid))
                .IsRequired();
            e.Property(i => i.SubscriptionId)
                .HasConversion(id => id.Value, guid => SubscriptionId.From(guid))
                .IsRequired();
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.OwnsOne(i => i.Amount, owned =>
            {
                owned.Property(p => p.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
                owned.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            e.Ignore(i => i.DomainEvents);
            e.HasIndex(i => i.CustomerId);
            e.HasIndex(i => i.SubscriptionId);
        });
    }

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).IsRequired().HasMaxLength(300);
            e.Property(x => x.Payload).IsRequired();
            e.Property(x => x.OccurredOnUtc).IsRequired();
            e.HasIndex(x => x.ProcessedOnUtc);
        });
    }

    private static void ConfigureIdempotency(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyEntry>(e =>
        {
            e.ToTable("IdempotencyEntries");
            e.HasKey(x => x.Key);
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.CommandType).IsRequired().HasMaxLength(300);
            e.Property(x => x.ResponseJson).IsRequired();
        });
    }
}
