namespace EnterpriseTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown by a use case when the aggregate it must operate on does not exist.
/// The API layer translates it into an HTTP 404 problem details response; the
/// Application layer knows nothing about transports.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    private NotFoundException(string entityName, string key, string message)
        : base(message)
    {
        EntityName = entityName;
        Key = key;
    }

    /// <summary>Name of the aggregate/entity that was searched for.</summary>
    public string? EntityName { get; }

    /// <summary>The key the lookup used, as text.</summary>
    public string? Key { get; }

    /// <summary>Builds a consistent "&lt;Entity&gt; with &lt;field&gt; '&lt;key&gt;' was not found." message.</summary>
    public static NotFoundException For(string entityName, object key, string keyName = "identifier") =>
        new(entityName, key?.ToString() ?? string.Empty,
            $"{entityName} with {keyName} '{key}' was not found.");
}
