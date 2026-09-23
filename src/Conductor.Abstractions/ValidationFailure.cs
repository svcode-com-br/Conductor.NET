namespace Conductor;

/// <summary>
/// Describes a single request-validation failure.
/// </summary>
/// <param name="PropertyName">The name of the invalid property or member.</param>
/// <param name="ErrorCode">A stable machine-readable error code.</param>
/// <param name="ErrorMessage">A human-readable error message. Must not include secrets.</param>
/// <param name="AttemptedValue">The value that failed validation, when it is safe to expose.</param>
public sealed record ValidationFailure(
    string PropertyName,
    string ErrorCode,
    string ErrorMessage,
    object? AttemptedValue = null);
