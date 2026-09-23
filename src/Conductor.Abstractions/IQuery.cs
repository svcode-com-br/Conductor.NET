namespace Conductor;

/// <summary>
/// A request that retrieves data and must not intentionally change application state.
/// </summary>
/// <typeparam name="TResponse">The response type produced when the query is handled.</typeparam>
public interface IQuery<out TResponse>
{
}
