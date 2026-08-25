using System.Text.RegularExpressions;

using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.ValueObjects;

/// <summary>
/// An email address. Required, syntactically validated and normalized to
/// lowercase so equality and uniqueness checks are case insensitive.
/// </summary>
public sealed partial class Email : ValueObject
{
    public const int MaxLength = 256;

    private Email(string value) => Value = value;

    /// <summary>Normalized (trimmed, lowercase) address.</summary>
    public string Value { get; }

    /// <summary>Part before the '@'.</summary>
    public string LocalPart => Value[..Value.IndexOf('@', StringComparison.Ordinal)];

    /// <summary>Part after the '@'.</summary>
    public string Domain => Value[(Value.IndexOf('@', StringComparison.Ordinal) + 1)..];

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            throw new DomainException($"Email must not exceed {MaxLength} characters.");
        }

        if (!EmailPattern().IsMatch(normalized))
        {
            throw new DomainException($"'{email}' is not a valid email address.");
        }

        return new Email(normalized);
    }

    public static bool TryCreate(string? email, out Email? result)
    {
        try
        {
            result = Create(email ?? string.Empty);
            return true;
        }
        catch (DomainException)
        {
            result = null;
            return false;
        }
    }

    [GeneratedRegex(
        @"^[^@\s]+(?:\.[^@\s.]+)*@[^@\s.]+(?:\.[^@\s.]+)+$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    private static partial Regex EmailPattern();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
