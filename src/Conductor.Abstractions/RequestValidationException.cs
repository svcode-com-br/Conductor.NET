namespace Conductor;

/// <summary>
/// Thrown when one or more validators reject a request.
/// </summary>
public sealed class RequestValidationException : Exception
{
    /// <summary>
    /// Initializes a new exception for the specified request type and failures.
    /// </summary>
    /// <param name="requestType">The request type that failed validation.</param>
    /// <param name="failures">The aggregated validation failures.</param>
    public RequestValidationException(
        Type requestType,
        IReadOnlyCollection<ValidationFailure> failures)
        : base($"Validation failed for request '{requestType.FullName}'.")
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(failures);

        RequestType = requestType;
        Failures = failures;
    }

    /// <summary>
    /// Gets the request type that failed validation.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// Gets the aggregated validation failures.
    /// </summary>
    public IReadOnlyCollection<ValidationFailure> Failures { get; }
}
