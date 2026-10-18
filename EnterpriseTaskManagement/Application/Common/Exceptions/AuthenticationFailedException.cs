namespace EnterpriseTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Single, deliberately opaque authentication failure. Unknown email, wrong
/// password, missing credential, inactive and soft-deleted accounts all raise
/// this same exception with the same message, so the API cannot be used to
/// enumerate accounts. The real reason is never sent to the client.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public const string GenericMessage = "Invalid email or password.";

    public AuthenticationFailedException()
        : base(GenericMessage)
    {
    }

    public AuthenticationFailedException(string message)
        : base(message)
    {
    }

    public AuthenticationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
