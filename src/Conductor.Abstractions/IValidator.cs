namespace Conductor;

/// <summary>
/// Validates a request before handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
public interface IValidator<in TRequest>
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="cancellationToken">The token used to cancel validation.</param>
    /// <returns>Zero or more validation failures. An empty collection means success.</returns>
    ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
