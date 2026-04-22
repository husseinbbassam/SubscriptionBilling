using System.Text.RegularExpressions;
using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Domain.ValueObjects;

/// <summary>
/// Represents a validated email address. Parsing and normalization happen
/// at construction — once you hold an Email, it's trustworthy.
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Of(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email cannot be empty.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!Pattern.IsMatch(normalized))
        {
            throw new DomainException($"Email '{value}' is not in a valid format.");
        }

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
