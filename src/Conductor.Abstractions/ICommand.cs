namespace Conductor;

/// <summary>
/// A request that expresses intent to change application state.
/// </summary>
/// <typeparam name="TResponse">The response type produced when the command is handled.</typeparam>
public interface ICommand<out TResponse>
{
}
