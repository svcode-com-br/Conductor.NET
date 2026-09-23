namespace Conductor.Testing;

/// <summary>
/// Collects pipeline events recorded by <see cref="RecordingBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class RecordingContext
{
    private readonly List<string> _events = [];

    /// <summary>
    /// Gets the recorded pipeline events in execution order.
    /// </summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>
    /// Appends an event to the log.
    /// </summary>
    /// <param name="value">The event label.</param>
    public void Add(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _events.Add(value);
    }

    /// <summary>
    /// Removes all recorded events.
    /// </summary>
    public void Clear() => _events.Clear();
}
