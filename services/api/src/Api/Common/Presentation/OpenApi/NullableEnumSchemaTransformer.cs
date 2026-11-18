using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Api.Common.Presentation.OpenApi;

/// <summary>
/// Removes <c>null</c> from shared enum schemas. A nullable property already wraps the enum in
/// <c>oneOf: [null, enum]</c>, and a <c>null</c> inside the enum makes both branches match, which <c>oneOf</c> forbids.
/// </summary>
internal sealed class NullableEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;

        if (type.IsEnum && schema.Enum is { Count: > 0 } values)
        {
            schema.Enum = values.Where(value => value is not null).ToList();
        }

        return Task.CompletedTask;
    }
}
