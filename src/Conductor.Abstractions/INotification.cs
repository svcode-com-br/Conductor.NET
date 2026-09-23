namespace Conductor;

/// <summary>
/// An in-process notification that may be handled by zero or more handlers.
/// </summary>
/// <remarks>
/// Domain events are notifications. Publishing is explicit and stays inside the current process.
/// </remarks>
public interface INotification
{
}
