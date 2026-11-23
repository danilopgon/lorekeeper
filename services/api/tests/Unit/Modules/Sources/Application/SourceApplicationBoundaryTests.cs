using System.Reflection;
using Api.Modules.Campaigns.Application;
using Api.Modules.Sources.Application;
using FluentAssertions;

namespace Unit.Modules.Sources.Application;

public sealed class SourceApplicationBoundaryTests
{
    private static readonly string[] ForbiddenNamespacePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Api.Modules.Sources.Infrastructure",
        "Api.Modules.Campaigns.Infrastructure",
        "Microsoft.AspNetCore"
    ];

    [Fact]
    public void SourceApplicationTypesDependOnNoPersistenceTypes()
    {
        var applicationTypes = typeof(SourceDto).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Api.Modules.Sources.Application", StringComparison.Ordinal) == true);

        var violations = applicationTypes
            .SelectMany(type => DependenciesOf(type).Select(dependency => $"{type.FullName} -> {dependency.FullName}"))
            .Where(violation => ForbiddenNamespacePrefixes.Any(prefix => violation.Split(" -> ")[1].StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void OnlySourceLookupResolvesCampaigns()
    {
        var violations = typeof(SourceDto).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("Api.Modules.Sources.Application", StringComparison.Ordinal) == true)
            .Where(type => type != typeof(SourceLookup) && type.DeclaringType != typeof(SourceLookup))
            .Where(type => DependenciesOf(type).Contains(typeof(ICampaignQueries)))
            .Select(type => $"{type.FullName} depends on ICampaignQueries; inject SourceLookup instead. It resolves the campaign "
                + "and source ids of every Sources request in the documented error order (`06`: campaign_id_invalid, "
                + "campaign_not_found, then source_id_invalid), so handlers do not repeat or reorder those checks.")
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
