using Conductor;

namespace Conductor.Abstractions.Tests;

public sealed class UnitTests
{
    [Fact]
    public void Value_is_default_and_equal()
    {
        Assert.Equal(default, Unit.Value);
        Assert.Equal(Unit.Value, new Unit());
        Assert.Equal(0, EqualityComparer<Unit>.Default.GetHashCode(Unit.Value));
    }

    [Fact]
    public void Unit_can_be_used_as_generic_response()
    {
        ICommand<Unit> command = new Ping();
        Assert.IsAssignableFrom<ICommand<Unit>>(command);
    }

    private sealed record Ping : ICommand<Unit>;
}

public sealed class ExceptionTests
{
    [Fact]
    public void HandlerNotFoundException_exposes_types()
    {
        var exception = new HandlerNotFoundException(typeof(string), typeof(int));

        Assert.Equal(typeof(string), exception.RequestType);
        Assert.Equal(typeof(int), exception.HandlerType);
        Assert.Contains("System.String", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateHandlerException_exposes_handlers()
    {
        Type[] handlers = [typeof(string), typeof(int)];
        var exception = new DuplicateHandlerException(typeof(Guid), handlers);

        Assert.Equal(typeof(Guid), exception.RequestType);
        Assert.Equal(handlers, exception.Handlers);
    }

    [Fact]
    public void RequestValidationException_exposes_failures()
    {
        var failures = new[]
        {
            new ValidationFailure("Name", "Name.Required", "Name is required.")
        };

        var exception = new RequestValidationException(typeof(string), failures);

        Assert.Equal(typeof(string), exception.RequestType);
        Assert.Same(failures, exception.Failures);
        Assert.Contains("System.String", exception.Message, StringComparison.Ordinal);
    }
}
