using Conductor;
using Conductor.AspNetCore;
using Conductor.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddConductor(options =>
{
    options.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
});
builder.Services.AddConductorProblemDetails();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseExceptionHandler();

app.MapPost("/orders", async (CreateOrderCommand command, IMediator mediator, CancellationToken cancellationToken) =>
{
    var id = await mediator.SendAsync(command, cancellationToken);
    return Results.Created($"/orders/{id}", new { id });
});

app.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var order = await mediator.QueryAsync(new GetOrderQuery(id), cancellationToken);
    return Results.Ok(order);
});

app.Run();
