namespace SubscriptionBilling.Domain.SeedWork;

/// <summary>
/// Thrown when a domain invariant is violated. Catch this at the API boundary
/// and translate to HTTP 409/422 as appropriate.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
