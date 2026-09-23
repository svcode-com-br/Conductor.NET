using Conductor;

namespace Conductor.Testing;

/// <summary>
/// Pipeline behavior that records before and after events for assertions.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class RecordingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly RecordingContext _context;
    private readonly string _name;

    /// <summary>
    /// Initializes a behavior that records events using the type name as the label.
    /// </summary>
    /// <param name="context">The shared recording context.</param>
    public RecordingBehavior(RecordingContext context)
        : this(context, "Recording")
    {
    }

    /// <summary>
    /// Initializes a named recording behavior.
    /// </summary>
    /// <param name="context">The shared recording context.</param>
    /// <param name="name">The label written before and after <c>next</c>.</param>
    public RecordingBehavior(RecordingContext context, string name)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _context = context;
        _name = name;
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        _context.Add($"{_name}-before");
        var response = await next().ConfigureAwait(false);
        _context.Add($"{_name}-after");
        return response;
    }
}
