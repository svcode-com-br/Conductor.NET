using System.Text.Json;
using Conductor;
using Conductor.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.AspNetCore.Tests;

public sealed class ProblemDetailsTests
{
    [Fact]
    public async Task RequestValidationException_is_mapped_to_400_problem_details()
    {
        var handler = new RequestValidationExceptionHandler();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new RequestValidationException(
            typeof(SampleCommand),
            [
                new ValidationFailure("Name", "Name.Required", "Name is required."),
                new ValidationFailure("Name", "Name.Length", "Name is too short.")
            ]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(400, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Name is required.", document.RootElement.GetProperty("errors").GetProperty("Name")[0].GetString());
        Assert.Equal("Name.Required", document.RootElement.GetProperty("errorCodes")[0].GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task HandlerNotFoundException_is_not_converted_to_a_client_error()
    {
        var handler = new RequestValidationExceptionHandler();
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            context,
            new HandlerNotFoundException(typeof(SampleCommand), typeof(ICommandHandler<SampleCommand, Unit>)),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task OperationCanceledException_is_not_treated_as_validation()
    {
        var handler = new RequestValidationExceptionHandler();
        var context = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            context,
            new OperationCanceledException(),
            CancellationToken.None);

        Assert.False(handled);
    }

    [Fact]
    public void AddConductorProblemDetails_registers_the_exception_handler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConductorProblemDetails();

        using var provider = services.BuildServiceProvider();
        Assert.Contains(
            provider.GetServices<Microsoft.AspNetCore.Diagnostics.IExceptionHandler>(),
            handler => handler is RequestValidationExceptionHandler);
    }

    private sealed record SampleCommand : ICommand<Unit>;
}
