namespace Conductor;

internal sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        ArgumentNullException.ThrowIfNull(validators);
        _validators = validators;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        List<ValidationFailure>? failures = null;

        foreach (var validator in _validators)
        {
            var result = await validator
                .ValidateAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (result.Count == 0)
            {
                continue;
            }

            failures ??= [];
            failures.AddRange(result);
        }

        if (failures is { Count: > 0 })
        {
            throw new RequestValidationException(typeof(TRequest), failures.AsReadOnly());
        }

        return await next().ConfigureAwait(false);
    }
}
