using System.Net.Mail;

namespace EnterpriseTaskManagement.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; private set; } = null!;

    // Required by EF Core
    private Email()
    {
    }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        try
        {
            _ = new MailAddress(value);
        }
        catch
        {
            throw new ArgumentException("Invalid email format.", nameof(value));
        }

        return new Email(value.Trim().ToLowerInvariant());
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