namespace Conductor.Testing;

/// <summary>
/// Counts handler invocations for test assertions.
/// </summary>
public sealed class HandlerInvocationCounter
{
    private int _count;

    /// <summary>
    /// Gets the number of recorded invocations.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Records a handler invocation.
    /// </summary>
    public void Increment() => Interlocked.Increment(ref _count);

    /// <summary>
    /// Throws when the invocation count does not match <paramref name="expected"/>.
    /// </summary>
    /// <param name="expected">The expected number of calls.</param>
    public void ShouldHaveBeenCalled(int expected)
    {
        if (_count == expected)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Handler invocation count mismatch. Expected {expected} but was {_count}.");
    }
}
