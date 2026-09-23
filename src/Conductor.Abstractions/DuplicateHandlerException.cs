namespace Conductor;

/// <summary>
/// Thrown when more than one handler is registered for the same closed request type.
/// </summary>
public sealed class DuplicateHandlerException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new exception for the duplicated request handler registrations.
    /// </summary>
    /// <param name="requestType">The request type with more than one handler.</param>
    /// <param name="handlers">The handler implementation types that were registered.</param>
    public DuplicateHandlerException(Type requestType, IReadOnlyCollection<Type> handlers)
        : base($"Multiple handlers are registered for request '{requestType.FullName}'.")
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(handlers);

        RequestType = requestType;
        Handlers = handlers;
    }

    /// <summary>
    /// Gets the request type that has more than one handler.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// Gets the handler implementation types that collided.
    /// </summary>
    public IReadOnlyCollection<Type> Handlers { get; }
}
