using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Domain.ValueObjects;

/// <summary>
/// Monetary amount with a currency code. Arithmetic is only legal between
/// amounts of the same currency — mixing is a domain error.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException($"Money amount cannot be negative (was {amount}).");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter ISO code.");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency) => Of(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public bool IsZero => Amount == 0m;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException(
                $"Cannot operate on Money of different currencies ({Currency} vs {other.Currency}).");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
