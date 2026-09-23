namespace Conductor.Testing;

/// <summary>
/// Assertions for pipeline execution order that do not depend on a specific test framework.
/// </summary>
public static class PipelineOrderAssertions
{
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when the recorded events do not match
    /// the expected sequence.
    /// </summary>
    /// <param name="expected">The expected event labels.</param>
    /// <param name="actual">The recorded event labels.</param>
    public static void Equal(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        if (expected.SequenceEqual(actual))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Pipeline order mismatch.{Environment.NewLine}" +
            $"Expected: [{string.Join(", ", expected)}]{Environment.NewLine}" +
            $"Actual:   [{string.Join(", ", actual)}]");
    }
}
