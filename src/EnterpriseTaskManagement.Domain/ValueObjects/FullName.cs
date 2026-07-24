namespace EnterpriseTaskManagement.Domain.ValueObjects;

public sealed class FullName : ValueObject
{
    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    // Required by EF Core
    private FullName()
    {
    }

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static FullName Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        firstName = firstName.Trim();
        lastName = lastName.Trim();

        if (firstName.Length > 100)
            throw new ArgumentException(
                "First name cannot exceed 100 characters.",
                nameof(firstName));

        if (lastName.Length > 100)
            throw new ArgumentException(
                "Last name cannot exceed 100 characters.",
                nameof(lastName));

        return new FullName(firstName, lastName);
    }

    public string Full => $"{FirstName} {LastName}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString()
    {
        return Full;
    }
}