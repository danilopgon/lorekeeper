using System.Reflection;
using Api.SharedKernel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Unit;

public sealed class ArchitectureTests
{
    private static readonly Type[] ApiTypes = typeof(Program).Assembly.GetTypes();

    [Fact]
    public void PersistenceTypesLiveInAModulePersistenceFolder()
    {
        var violations = ApiTypes
            .Where(IsPersistenceType)
            .Where(type => type.Namespace?.Contains(".Infrastructure.Persistence", StringComparison.Ordinal) != true)
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
        var violations = ApplicationTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => method.Name == "Handle")
            .Where(method => !ReturnsResult(method.ReturnType))
            .Select(method => $"{method.DeclaringType!.FullName}.Handle returns {method.ReturnType.Name}; return Task<Result<T>> (ADR-012)")
            .ToList();

        violations.Should().BeEmpty();
    }

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
