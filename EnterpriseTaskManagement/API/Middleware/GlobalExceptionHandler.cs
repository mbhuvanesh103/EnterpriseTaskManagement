using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Domain.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ApplicationValidationException = EnterpriseTaskManagement.Application.Common.Exceptions.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace EnterpriseTaskManagement.API.Middleware;

/// <summary>
/// Single place where exceptions become HTTP responses (.NET
/// <see cref="IExceptionHandler"/>, wired through <c>UseExceptionHandler</c>).
/// Every response is RFC 7807 <see cref="ProblemDetails"/> JSON with
/// <c>type</c>, <c>title</c>, <c>status</c> and <c>detail</c>.
///
/// Mapping:
///   NotFoundException                       -> 404
///   ForbiddenAccessException (ownership rule)   -> 403
///   ConflictException (duplicate email/code) -> 409
///   FluentValidation.ValidationException     -> 400 (+ per-field errors)
///   Application ValidationException          -> 400 (+ per-field errors)
///   DomainException (broken business rule)   -> 400
///   ArgumentException                        -> 400
///   anything else                            -> 500 with a generic detail
///
/// Only messages authored by our own Domain/Application layers are echoed.
/// Unhandled exceptions never expose their message, stack trace, connection
/// string, credentials or token values — those are logged server-side only.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string ProblemTypeBase = "https://httpstatuses.io/";

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        ProblemDetails problem = Map(exception);

        problem.Type ??= ProblemTypeBase + problem.Status;
        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogInformation("Request failed with {Status} for {Method} {Path}: {Title}",
                problem.Status, httpContext.Request.Method, httpContext.Request.Path, problem.Title);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static ProblemDetails Map(Exception exception) => exception switch
    {
        NotFoundException notFound => Problem(
            StatusCodes.Status404NotFound, "Resource not found", notFound.Message),

        // Authentication failures are always the same generic 401: no hint about
        // whether the account exists, is inactive, or the password was wrong.
        AuthenticationFailedException => Problem(
            StatusCodes.Status401Unauthorized, "Authentication failed",
            AuthenticationFailedException.GenericMessage),

        ForbiddenAccessException forbidden => Problem(
            StatusCodes.Status403Forbidden, "Forbidden", forbidden.Message),

        ConflictException conflict => Problem(
            StatusCodes.Status409Conflict, "Conflict", conflict.Message),

        // Optimistic concurrency: the rowversion token no longer matched, so the
        // row was changed by someone else. Never silently overwrite it.
        DbUpdateConcurrencyException => Problem(
            StatusCodes.Status409Conflict, "Concurrency conflict",
            "The resource was modified by another request. Reload it and retry."),

        ApplicationValidationException validation => WithErrors(
            Problem(StatusCodes.Status400BadRequest, "Validation failed", validation.Message),
            validation.Errors),

        FluentValidationException fluent => WithErrors(
            Problem(StatusCodes.Status400BadRequest, "Validation failed",
                "One or more validation failures occurred."),
            fluent.Errors
                .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
                .ToDictionary(group => group.Key, group => group.ToArray())),

        DomainException domain => Problem(
            StatusCodes.Status400BadRequest, "Business rule violation", domain.Message),

        ArgumentException argument => Problem(
            StatusCodes.Status400BadRequest, "Invalid argument", argument.Message),

        _ => Problem(
            StatusCodes.Status500InternalServerError, "Internal server error",
            "An unexpected error occurred while processing the request."),
    };

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
        };

    private static ProblemDetails WithErrors(ProblemDetails problem, IDictionary<string, string[]> errors)
    {
        problem.Extensions["errors"] = errors;
        return problem;
    }
}
