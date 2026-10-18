namespace EnterpriseTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown by a use case when the requested change collides with data that
/// already exists (duplicate email, duplicate employee code). The API layer
/// translates it into an HTTP 409 problem details response.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private ConflictException(string entityName, string field, string message)
        : base(message)
    {
        EntityName = entityName;
        Field = field;
    }

    /// <summary>Name of the aggregate/entity the conflict was detected on.</summary>
    public string? EntityName { get; }

    /// <summary>The unique field that already holds the supplied value.</summary>
    public string? Field { get; }

    /// <summary>Builds a consistent "&lt;Entity&gt; with &lt;field&gt; '&lt;value&gt;' already exists." message.</summary>
    public static ConflictException Duplicate(string entityName, string field, object value) =>
        new(entityName, field, $"{entityName} with {field} '{value}' already exists.");
}
