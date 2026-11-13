using System.Reflection;
using Api.Modules.Campaigns.Application;
using FluentAssertions;

namespace Unit.Modules.Campaigns.Application;

public sealed class CampaignApplicationBoundaryTests
{
    private static readonly string[] ForbiddenNamespacePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Api.Modules.Campaigns.Infrastructure"
    ];

    [Fact]
    public void CampaignApplicationTypesDependOnNoPersistenceTypes()
    {
        var applicationTypes = typeof(CampaignDto).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Api.Modules.Campaigns.Application", StringComparison.Ordinal) == true);

        var violations = applicationTypes
            .SelectMany(type => DependenciesOf(type).Select(dependency => $"{type.FullName} -> {dependency.FullName}"))
            .Where(violation => ForbiddenNamespacePrefixes.Any(prefix => violation.Split(" -> ")[1].StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        violations.Should().BeEmpty();
    }

    private static IEnumerable<Type> DependenciesOf(Type type)
    {
        const BindingFlags members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        var constructorParameters = type.GetConstructors(members).SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType);
        var fields = type.GetFields(members).Select(field => field.FieldType);
        var methodSignatures = type.GetMethods(members).SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType));

        return constructorParameters.Concat(fields).Concat(methodSignatures).SelectMany(Unwrap);
    }

    private static IEnumerable<Type> Unwrap(Type type) =>
        type.IsGenericType
            ? type.GetGenericArguments().SelectMany(Unwrap).Prepend(type.GetGenericTypeDefinition())
            : type.HasElementType ? Unwrap(type.GetElementType()!) : [type];
}
