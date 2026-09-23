using Conductor;
using Conductor.DependencyInjection;
using Conductor.Sample.Blazor.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddConductor(options =>
{
    options.RegisterServicesFromAssemblyContaining<GetDashboardQuery>();
});

builder.Services.AddSingleton<DashboardStore>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public sealed record GetDashboardQuery : IQuery<DashboardView>;

public sealed record DashboardView(string Title, DateTimeOffset GeneratedAt);

public sealed class DashboardStore
{
    public DashboardView Current { get; } = new("Conductor.NET sample", DateTimeOffset.UtcNow);
}

public sealed class GetDashboardHandler(DashboardStore store, TimeProvider timeProvider)
    : IQueryHandler<GetDashboardQuery, DashboardView>
{
    public Task<DashboardView> HandleAsync(GetDashboardQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(store.Current with { GeneratedAt = timeProvider.GetUtcNow() });
}
