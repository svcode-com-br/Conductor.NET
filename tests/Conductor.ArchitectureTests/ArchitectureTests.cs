using Conductor;
using Conductor.AspNetCore;
using Conductor.DependencyInjection;
using Conductor.Testing;
using NetArchTest.Rules;

namespace Conductor.ArchitectureTests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Abstractions_do_not_reference_forbidden_assemblies()
    {
        var referenced = typeof(IMediator).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .ToArray();

        Assert.DoesNotContain("Conductor", referenced);
        Assert.DoesNotContain("Conductor.DependencyInjection", referenced);
        Assert.DoesNotContain("Conductor.Testing", referenced);
        Assert.DoesNotContain("Conductor.AspNetCore", referenced);
        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, name => name.StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal));
    }

    [Fact]
    public void Runtime_does_not_depend_on_aspnet_ef_or_optional_packages()
    {
        var result = Types.InAssembly(typeof(ConductorTelemetry).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Conductor.DependencyInjection",
                "Conductor.Testing",
                "Conductor.AspNetCore",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void DependencyInjection_does_not_depend_on_aspnet_or_testing()
    {
        var result = Types.InAssembly(typeof(ConductorServiceCollectionExtensions).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Conductor.Testing",
                "Conductor.AspNetCore",
                "Microsoft.AspNetCore",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Testing_package_does_not_depend_on_aspnet()
    {
        var result = Types.InAssembly(typeof(FakeMediator).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Conductor.AspNetCore", "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Public_abstractions_live_in_conductor_namespace()
    {
        var result = Types.InAssembly(typeof(IMediator).Assembly)
            .That()
            .ArePublic()
            .Should()
            .ResideInNamespace("Conductor")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    [Fact]
    public void Runtime_internals_are_not_public()
    {
        var publicTypes = typeof(ConductorTelemetry).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName ?? type.Name)
            .ToArray();

        Assert.All(publicTypes, name => Assert.StartsWith("Conductor.", name, StringComparison.Ordinal));
        Assert.DoesNotContain(publicTypes, name => name is "Conductor.CommandDispatcher" or "Conductor.QueryDispatcher" or "Conductor.Mediator" or "Conductor.NotificationPublisher");
        Assert.Equal("Conductor.ConductorTelemetry", Assert.Single(publicTypes));
    }

    [Fact]
    public void AspNetCore_package_depends_inward_on_abstractions_only()
    {
        var result = Types.InAssembly(typeof(RequestValidationExceptionHandler).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Conductor.DependencyInjection",
                "Conductor.Testing",
                "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Format(result));
    }

    private static string Format(TestResult result) =>
        result.FailingTypeNames is { Count: > 0 }
            ? string.Join(", ", result.FailingTypeNames)
            : "architecture rule failed";
}
