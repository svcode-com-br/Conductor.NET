using System.Collections.Concurrent;
using Conductor.Internal;

namespace Conductor;

internal sealed class QueryDispatcher : IQueryDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var wrapper = GetOrAddWrapper<TResponse>(query.GetType());
        using var activity = ConductorDiagnostics.StartQuery(query.GetType());

        try
        {
            var response = await wrapper
                .HandleAsync(query, _serviceProvider, cancellationToken)
                .ConfigureAwait(false);

            ConductorDiagnostics.SetOutcome(activity, ConductorTelemetry.OutcomeSuccess);
            return response;
        }
        catch (RequestValidationException exception)
        {
            ConductorDiagnostics.SetOutcome(activity, ConductorTelemetry.OutcomeValidationFailed);
            ConductorDiagnostics.SetValidationFailureCount(activity, exception.Failures.Count);
            throw;
        }
        catch (OperationCanceledException)
        {
            ConductorDiagnostics.SetOutcome(activity, ConductorTelemetry.OutcomeCanceled);
            throw;
        }
        catch
        {
            ConductorDiagnostics.SetOutcome(activity, ConductorTelemetry.OutcomeError);
            throw;
        }
    }

    private static QueryHandlerWrapper<TResponse> GetOrAddWrapper<TResponse>(Type requestType)
    {
        var wrapper = Wrappers.GetOrAdd(requestType, type =>
        {
            var wrapperType = typeof(QueryHandlerWrapperImpl<,>).MakeGenericType(type, typeof(TResponse));
            return Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Unable to create handler wrapper for '{type.FullName}'.");
        });

        return (QueryHandlerWrapper<TResponse>)wrapper;
    }
}
