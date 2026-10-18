namespace EnterpriseTaskManagement.Domain.Exceptions;

/// <summary>
/// Raised when a domain invariant is violated. The API layer translates this
/// into an HTTP 400/422 problem details response; the Domain itself has no
/// knowledge of transports.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
