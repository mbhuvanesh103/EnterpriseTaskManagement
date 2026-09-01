namespace EnterpriseTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller is authenticated but the use case's own ownership rule
/// denies the operation (for example a Manager trying to change a project led by
/// somebody else). The API layer translates it into an HTTP 403 problem details
/// response; the Application layer knows nothing about transports.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    public const string GenericMessage = "You are not allowed to perform this operation.";

    public ForbiddenAccessException()
        : base(GenericMessage)
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
