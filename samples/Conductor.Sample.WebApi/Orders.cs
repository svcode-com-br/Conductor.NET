using Conductor;

public sealed record CreateOrderCommand(string OrderNumber, string ClientName) : ICommand<Guid>;

public sealed record GetOrderQuery(Guid Id) : IQuery<OrderDto>;

public sealed record OrderDto(Guid Id, string OrderNumber, string ClientName, DateTimeOffset OrderDate);

public sealed class CreateOrderValidator : IValidator<CreateOrderCommand>
{
    public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        if (string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            failures.Add(new ValidationFailure(
                nameof(request.OrderNumber),
                "OrderNumber.Required",
                "OrderNumber is required."));
        }

        if (string.IsNullOrWhiteSpace(request.ClientName))
        {
            failures.Add(new ValidationFailure(
                nameof(request.ClientName),
                "ClientName.Required",
                "ClientName is required."));
        }

        return ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>(failures);
    }
}

public sealed class GetOrderValidator : IValidator<GetOrderQuery>
{
    public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        GetOrderQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ValidationFailure> failures = request.Id == Guid.Empty
            ?
            [
                new ValidationFailure(
                    nameof(request.Id),
                    "Id.Required",
                    "Id is required and cannot be an empty GUID.")
            ]
            : [];

        return ValueTask.FromResult(failures);
    }
}

public sealed class OrderStore
{
    public Dictionary<Guid, OrderDto> Orders { get; } = [];
}

public sealed class CreateOrderHandler(OrderStore store, TimeProvider timeProvider)
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public Task<Guid> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        store.Orders[id] = new OrderDto(
            id,
            command.OrderNumber,
            command.ClientName,
            timeProvider.GetUtcNow());
        return Task.FromResult(id);
    }
}

public sealed class GetOrderHandler(OrderStore store) : IQueryHandler<GetOrderQuery, OrderDto>
{
    public Task<OrderDto> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
    {
        if (!store.Orders.TryGetValue(query.Id, out var order))
        {
            throw new InvalidOperationException("Order was not found.");
        }

        return Task.FromResult(order);
    }
}
