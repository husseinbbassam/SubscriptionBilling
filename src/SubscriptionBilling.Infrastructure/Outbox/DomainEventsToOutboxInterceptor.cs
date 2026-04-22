using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Outbox;

/// <summary>
/// EF Core interceptor that runs on SaveChanges: it walks every tracked
/// aggregate, drains its DomainEvents buffer, and appends an OutboxMessage row.
/// This gives us atomicity — events are persisted in the same transaction as
/// the state change that caused them. A separate dispatcher (not included in
/// this sample) reads unprocessed rows and publishes them.
/// </summary>
public sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context as BillingDbContext;
        if (context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var aggregates = context.ChangeTracker
            .Entries()
            .Where(HasDomainEvents)
            .ToList();

        foreach (var entry in aggregates)
        {
            var events = GetAndClearEvents(entry);
            foreach (var domainEvent in events)
            {
                context.OutboxMessages.Add(new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName!,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
                    OccurredOnUtc = domainEvent.OccurredOnUtc
                });
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static bool HasDomainEvents(EntityEntry entry)
    {
        var type = entry.Entity.GetType();
        while (type is not null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
            type = type.BaseType;
        }
        return false;
    }

    private static IReadOnlyList<IDomainEvent> GetAndClearEvents(EntityEntry entry)
    {
        dynamic aggregate = entry.Entity;
        var events = (IReadOnlyCollection<IDomainEvent>)aggregate.DomainEvents;
        var copy = events.ToList();
        aggregate.ClearDomainEvents();
        return copy;
    }
}
