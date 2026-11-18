using System.Reflection;
using System.Text.RegularExpressions;
using MediatR;
using Api.SharedKernel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Unit;

public sealed partial class ArchitectureTests
{
    private static readonly Type[] ApiTypes = typeof(Program).Assembly.GetTypes();

    [Fact]
    public void PersistenceTypesLiveInAModulePersistenceFolder()
    {
        var violations = ApiTypes
            .Where(IsPersistenceType)
            .Where(type => type.Namespace is null || !ModulePersistenceNamespace().IsMatch(type.Namespace))
            .Select(type => $"{type.FullName} must live under Modules/<Module>/Infrastructure/Persistence/")
            .ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationDeclaresNoStatusEnums()
    {
        var violations = ApplicationTypes()
            .Where(type => type.IsEnum && type.Name.EndsWith("Status", StringComparison.Ordinal))
            .Select(type => $"{type.FullName}: return Result<T> with a SharedKernel Error instead of a status enum (ADR-012)")
            .ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void HandlersReturnResults()
    {
        var publicHandles = ApplicationTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method.Name == "Handle")
            .Select(method => (Handler: method.DeclaringType!, Response: method.ReturnType));
        var mediatRHandlers = ApiTypes
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(contract => (Handler: type, Response: typeof(Task<>).MakeGenericType(contract.GetGenericArguments()[1]))));

        var violations = publicHandles.Concat(mediatRHandlers)
            .Where(handler => !ReturnsResult(handler.Response))
            .Select(handler => $"{handler.Handler.FullName} handles into {handler.Response}; return Task<Result<T>> (ADR-012)")
            .Distinct()
            .ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void UseCasesAreDispatchedThroughMediatR()
    {
        var handlersOutsideMediatR = ApplicationTypes()
            .Where(type => type.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) is not null)
            .Where(type => !type.GetInterfaces().Any(IsRequestHandler))
            .Select(type => $"{type.FullName} has Handle but does not implement IRequestHandler<,> (ADR-013)");
        var controllersInjectingHandlers = ApiTypes
            .Where(type => type.Namespace?.EndsWith(".Presentation", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
                .Where(parameter => parameter.ParameterType.GetInterfaces().Any(IsRequestHandler))
                .Select(parameter => $"{type.FullName} injects {parameter.ParameterType.Name}; inject ISender instead (ADR-013)"));

        handlersOutsideMediatR.Concat(controllersInjectingHandlers).Should().BeEmpty();
    }

    private static bool IsRequestHandler(Type contract) =>
        contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);

    [GeneratedRegex(@"\AApi\.Modules\.[^.]+\.Infrastructure\.Persistence(\.|\z)")]
    private static partial Regex ModulePersistenceNamespace();

    private static IEnumerable<Type> ApplicationTypes() =>
        ApiTypes.Where(type => type.Namespace?.Contains(".Application", StringComparison.Ordinal) == true
            && type.Namespace.StartsWith("Api.Modules.", StringComparison.Ordinal));

    private static bool IsPersistenceType(Type type) =>
        typeof(DbContext).IsAssignableFrom(type)
        || typeof(Migration).IsAssignableFrom(type)
        || typeof(ModelSnapshot).IsAssignableFrom(type)
        || type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

    private static bool ReturnsResult(Type returnType) =>
        returnType.IsGenericType
        && returnType.GetGenericTypeDefinition() == typeof(Task<>)
        && returnType.GetGenericArguments()[0] is { IsGenericType: true } inner
        && inner.GetGenericTypeDefinition() == typeof(Result<>);
}
