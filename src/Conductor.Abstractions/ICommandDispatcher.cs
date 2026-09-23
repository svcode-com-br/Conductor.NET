namespace Conductor;

/// <summary>
/// Dispatches commands to exactly one handler through the registered pipeline.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Sends a command to its handler.
    /// </summary>
    /// <typeparam name="TResponse">The command response type.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The token used to cancel dispatch.</param>
    /// <returns>The handler response.</returns>
    Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default);
}
