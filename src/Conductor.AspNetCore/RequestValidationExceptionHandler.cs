using System.Text.Json;
using Conductor;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Conductor.AspNetCore;

/// <summary>
/// Maps <see cref="RequestValidationException"/> to HTTP 400 ProblemDetails.
/// Configuration failures such as missing handlers are not converted to client errors.
/// </summary>
public sealed class RequestValidationExceptionHandler : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not RequestValidationException validationException)
        {
            return false;
        }

        var errors = validationException.Failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problem = new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Detail = validationException.Message,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["errorCodes"] = validationException.Failures
            .Select(failure => new Dictionary<string, string>
            {
                ["propertyName"] = failure.PropertyName,
                ["errorCode"] = failure.ErrorCode
            })
            .ToArray();

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken).ConfigureAwait(false);

        return true;
    }
}
