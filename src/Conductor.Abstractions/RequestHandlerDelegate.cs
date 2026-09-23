namespace Conductor;

/// <summary>
/// Continuable delegate that invokes the next pipeline behavior or the terminal handler.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <returns>A task that produces the pipeline response.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
