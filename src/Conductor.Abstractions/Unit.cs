namespace Conductor;

/// <summary>
/// Represents successful completion of a command or query that does not return business data.
/// </summary>
/// <remarks>
/// <see cref="Unit"/> is an immutable value type and does not allocate on the managed heap in normal use.
/// </remarks>
public readonly record struct Unit
{
    /// <summary>
    /// Gets the canonical <see cref="Unit"/> value.
    /// </summary>
    public static Unit Value => default;
}
