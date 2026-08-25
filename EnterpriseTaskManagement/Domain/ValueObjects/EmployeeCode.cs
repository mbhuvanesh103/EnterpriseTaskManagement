using System.Text.RegularExpressions;

using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.ValueObjects;

/// <summary>
/// Organization-issued employee identifier: the literal prefix "EMP" followed
/// by exactly six digits, e.g. EMP000001. Input is trimmed and upper-cased
/// before validation.
/// </summary>
public sealed partial class EmployeeCode : ValueObject
{
    public const string Prefix = "EMP";
    public const int DigitCount = 6;
    public const int Length = 9;

    private EmployeeCode(string value) => Value = value;

    /// <summary>Normalized code, e.g. "EMP000001".</summary>
    public string Value { get; }

    /// <summary>Numeric part of the code.</summary>
    public int Sequence => int.Parse(Value[Prefix.Length..], System.Globalization.CultureInfo.InvariantCulture);

    public static EmployeeCode Create(string employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            throw new DomainException("Employee code is required.");
        }

        var normalized = employeeCode.Trim().ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
        {
            throw new DomainException(
                $"Employee code must be '{Prefix}' followed by exactly {DigitCount} digits (e.g. EMP000001).");
        }

        return new EmployeeCode(normalized);
    }

    /// <summary>Builds a code from its numeric sequence, e.g. 1 -> EMP000001.</summary>
    public static EmployeeCode FromSequence(int sequence)
    {
        if (sequence is < 0 or > 999_999)
        {
            throw new DomainException($"Employee code sequence must fit in {DigitCount} digits.");
        }

        return new EmployeeCode(
            Prefix + sequence.ToString("D" + DigitCount, System.Globalization.CultureInfo.InvariantCulture));
    }

    public static bool TryCreate(string? employeeCode, out EmployeeCode? result)
    {
        try
        {
            result = Create(employeeCode ?? string.Empty);
            return true;
        }
        catch (DomainException)
        {
            result = null;
            return false;
        }
    }

    [GeneratedRegex(
        @"^EMP[0-9]{6}$",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    private static partial Regex CodePattern();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
