using Accounting.Identity.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Identity.API.Common;

/// <summary>
/// Factory for creating RFC 9457 compliant Problem Details responses.
/// Maps domain Result patterns to appropriate HTTP status codes and problem details.
/// </summary>
public static class ProblemDetailsFactory
{
    /// <summary>
    /// Creates a Problem Details response from a failed Result.
    /// </summary>
    public static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Error error,
        int statusCode)
    {
        var problemDetails = new ProblemDetails
        {
            Type = GetProblemType(error.Code),
            Title = GetTitle(statusCode),
            Status = statusCode,
            Detail = error.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = error.Code;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        return problemDetails;
    }

    /// <summary>
    /// Creates a validation problem details response.
    /// </summary>
    public static ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        Dictionary<string, string[]> errors)
    {
        var problemDetails = new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc9457#section-3.1",
            Title = "One or more validation errors occurred",
            Status = StatusCodes.Status400BadRequest,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        return problemDetails;
    }

    private static string GetProblemType(string errorCode)
    {
        return errorCode switch
        {
            _ when errorCode.StartsWith("Validation") => "https://tools.ietf.org/html/rfc9457#section-3.1",
            _ when errorCode.StartsWith("NotFound") => "https://tools.ietf.org/html/rfc9457#section-3.1",
            _ when errorCode.StartsWith("Conflict") => "https://tools.ietf.org/html/rfc9457#section-3.1",
            _ when errorCode.StartsWith("Unauthorized") => "https://tools.ietf.org/html/rfc9457#section-3.1",
            _ => "https://tools.ietf.org/html/rfc9457#section-3.1"
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            _ => "An error occurred"
        };
    }

    /// <summary>
    /// Maps error codes to HTTP status codes.
    /// </summary>
    public static int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            _ when errorCode.StartsWith("Validation") => StatusCodes.Status400BadRequest,
            _ when errorCode.StartsWith("NotFound") => StatusCodes.Status404NotFound,
            _ when errorCode.StartsWith("Conflict") => StatusCodes.Status409Conflict,
            _ when errorCode.StartsWith("Unauthorized") => StatusCodes.Status401Unauthorized,
            _ when errorCode.StartsWith("Forbidden") => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
