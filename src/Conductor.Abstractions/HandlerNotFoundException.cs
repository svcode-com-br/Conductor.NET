namespace Conductor;

/// <summary>
/// Thrown when no handler is registered for a dispatched request type.
/// </summary>
public sealed class HandlerNotFoundException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new exception for the missing handler.
    /// </summary>
    /// <param name="requestType">The request type that was dispatched.</param>
    /// <param name="handlerType">The handler service type that was expected.</param>
    public HandlerNotFoundException(Type requestType, Type handlerType)
        : base($"No handler is registered for request '{requestType.FullName}'. " +
               $"Expected service '{handlerType.FullName}'.")
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(handlerType);

        RequestType = requestType;
        HandlerType = handlerType;
    }

    /// <summary>
    /// Gets the request type that could not be resolved.
    /// </summary>
    public Type RequestType { get; }

    /// <summary>
    /// Gets the handler service type that was expected.
    /// </summary>
    public Type HandlerType { get; }
}
