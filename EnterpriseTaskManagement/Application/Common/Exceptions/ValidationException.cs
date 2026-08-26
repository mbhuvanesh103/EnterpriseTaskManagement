using FluentValidation.Results;

namespace EnterpriseTaskManagement.Application.Common.Exceptions;

/// <summary>
/// Aggregated input-shape failures produced by the validation pipeline behavior.
/// The API layer maps <see cref="Errors"/> onto RFC 7807 problem details.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures occurred.") =>
        Errors = new Dictionary<string, string[]>();

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this() =>
        Errors = failures
            .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.ToArray());

    /// <summary>Field name → messages.</summary>
    public IDictionary<string, string[]> Errors { get; }
}
