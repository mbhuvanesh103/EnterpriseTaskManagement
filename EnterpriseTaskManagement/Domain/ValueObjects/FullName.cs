using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.ValueObjects;

/// <summary>
/// A person's name. Requires both a first and a last name; the represented
/// value ("First [Middle] Last") may not exceed 100 characters.
/// Immutable, structurally equal, self-validating.
/// </summary>
public sealed class FullName : ValueObject
{
    public const int MaxLength = 100;

    private FullName(string firstName, string? middleName, string lastName, string value)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        Value = value;
    }

    public string FirstName { get; }

    public string? MiddleName { get; }

    public string LastName { get; }

    /// <summary>Normalized, whitespace-collapsed full name.</summary>
    public string Value { get; }

    /// <summary>Creates a validated name from its parts.</summary>
    public static FullName Create(string firstName, string lastName, string? middleName = null)
    {
        var first = Normalize(firstName);
        var last = Normalize(lastName);
        var middle = string.IsNullOrWhiteSpace(middleName) ? null : Normalize(middleName!);

        if (first.Length == 0)
        {
            throw new DomainException("First name is required.");
        }

        if (last.Length == 0)
        {
            throw new DomainException("Last name is required.");
        }

        var value = middle is null ? $"{first} {last}" : $"{first} {middle} {last}";

        if (value.Length > MaxLength)
        {
            throw new DomainException($"Full name must not exceed {MaxLength} characters.");
        }

        return new FullName(first, middle, last, value);
    }

    /// <summary>
    /// Parses a single string such as "Ada Byron Lovelace": the first token is
    /// the first name, the last token the last name, anything between is the
    /// middle name.
    /// </summary>
    public static FullName Parse(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Full name is required.");
        }

        var parts = fullName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            throw new DomainException("Full name must contain a first and a last name.");
        }

        var first = parts[0];
        var last = parts[^1];
        var middle = parts.Length > 2 ? string.Join(' ', parts[1..^1]) : null;

        return Create(first, last, middle);
    }

    public static bool TryParse(string? fullName, out FullName? result)
    {
        try
        {
            result = Parse(fullName ?? string.Empty);
            return true;
        }
        catch (DomainException)
        {
            result = null;
            return false;
        }
    }

    private static string Normalize(string input) =>
        string.Join(
            ' ',
            (input ?? string.Empty).Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
