using System.Text.RegularExpressions;

namespace EnterpriseTaskManagement.Domain.ValueObjects;

public sealed class EmployeeCode : ValueObject
{
    private static readonly Regex EmployeeCodeRegex =
        new(@"^EMP\d{6}$", RegexOptions.Compiled);

    public string Value { get; private set; } = null!;

    // Required by EF Core
    private EmployeeCode()
    {
    }

    private EmployeeCode(string value)
    {
        Value = value;
    }

    public static EmployeeCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Employee code is required.", nameof(value));

        value = value.Trim().ToUpperInvariant();

        if (!EmployeeCodeRegex.IsMatch(value))
            throw new ArgumentException(
                "Employee code must follow the format EMP followed by 6 digits.",
                nameof(value));

        return new EmployeeCode(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }
}