using System.Collections.Concurrent;
using Conductor.Internal;

namespace Conductor;

internal sealed class CommandDispatcher : ICommandDispatcher
{
    private static readonly ConcurrentDictionary<Type, object> Wrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var wrapper = GetOrAddWrapper<TResponse>(command.GetType());
        using var activity = ConductorDiagnostics.StartCommand(command.GetType());

        try
        {
            var response = await wrapper
                .HandleAsync(command, _serviceProvider, cancellationToken)
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

    private static CommandHandlerWrapper<TResponse> GetOrAddWrapper<TResponse>(Type requestType)
    {
        var wrapper = Wrappers.GetOrAdd(requestType, type =>
        {
            var wrapperType = typeof(CommandHandlerWrapperImpl<,>).MakeGenericType(type, typeof(TResponse));
            return Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Unable to create handler wrapper for '{type.FullName}'.");
        });

        return (CommandHandlerWrapper<TResponse>)wrapper;
    }
}
