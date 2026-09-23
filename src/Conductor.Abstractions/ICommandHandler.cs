namespace Conductor;

/// <summary>
/// Handles a single closed command type.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="command">The command instance.</param>
    /// <param name="cancellationToken">The token used to cancel handler execution.</param>
    /// <returns>The handler response.</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
