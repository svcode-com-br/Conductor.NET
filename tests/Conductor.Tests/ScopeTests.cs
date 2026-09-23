using Conductor;
using Conductor.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Tests;

public sealed class ScopeTests
{
    [Fact]
    public async Task Scoped_handler_is_resolved_inside_scope()
    {
        var services = new ServiceCollection();
        services.AddConductor();
        services.AddScoped<ScopedState>();
        services.AddScoped<ICommandHandler<ScopedCommand, string>, ScopedCommandHandler>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ScopedState>().Value = "from-scope";
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.SendAsync(new ScopedCommand());

        Assert.Equal("from-scope", result);
    }

    [Fact]
    public async Task Wrapper_cache_does_not_reuse_scoped_handler_instances_across_scopes()
    {
        var services = new ServiceCollection();
        services.AddConductor();
        services.AddScoped<ScopedState>();
        services.AddScoped<ICommandHandler<ScopedCommand, string>, ScopedCommandHandler>();

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        string first;
        string second;

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ScopedState>().Value = "one";
            first = await scope.ServiceProvider
                .GetRequiredService<ICommandDispatcher>()
                .SendAsync(new ScopedCommand());
        }

        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<ScopedState>().Value = "two";
            second = await scope.ServiceProvider
                .GetRequiredService<ICommandDispatcher>()
                .SendAsync(new ScopedCommand());
        }

        Assert.Equal("one", first);
        Assert.Equal("two", second);
    }

    [Fact]
    public void ValidateOnBuild_does_not_report_captive_dependencies_for_default_registrations()
    {
        var services = new ServiceCollection();
        services.AddConductor();
        services.AddScoped<ICommandHandler<PingCommand, string>, PingCommandHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IMediator>());
    }

    private sealed record ScopedCommand : ICommand<string>;

    private sealed class ScopedState
    {
        public string Value { get; set; } = "";
    }

    private sealed class ScopedCommandHandler(ScopedState state) : ICommandHandler<ScopedCommand, string>
    {
        public Task<string> HandleAsync(ScopedCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(state.Value);
    }
}
