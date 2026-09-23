using Conductor;
using Conductor.DependencyInjection;
using Conductor.Sample.ConsoleApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddConductor(options =>
{
    options.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
});
builder.Services.AddSingleton(TimeProvider.System);

using var host = builder.Build();
using var scope = host.Services.CreateScope();

var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
var orderId = await mediator.SendAsync(new CreateOrderCommand("PO-10001", "Northwind Traders"));
var order = await mediator.QueryAsync(new GetOrderQuery(orderId));

Console.WriteLine(
    $"Created order {order.OrderNumber} for {order.ClientName} on {order.OrderDate:u} ({order.Id}).");
