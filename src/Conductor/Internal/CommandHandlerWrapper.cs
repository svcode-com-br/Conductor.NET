using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Internal;

internal abstract class CommandHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> HandleAsync(
        ICommand<TResponse> command,
        IServiceProvider services,
        CancellationToken cancellationToken);
}

internal sealed class CommandHandlerWrapperImpl<TCommand, TResponse> : CommandHandlerWrapper<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override Task<TResponse> HandleAsync(
        ICommand<TResponse> command,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var typed = (TCommand)command;
        var handler = services.GetService<ICommandHandler<TCommand, TResponse>>()
            ?? throw new HandlerNotFoundException(typeof(TCommand), typeof(ICommandHandler<TCommand, TResponse>));

        ConductorDiagnostics.SetHandlerType(System.Diagnostics.Activity.Current, handler.GetType());

        return PipelineExecutor.ExecuteAsync(
            services,
            typed,
            () => handler.HandleAsync(typed, cancellationToken),
            cancellationToken);
    }
}
