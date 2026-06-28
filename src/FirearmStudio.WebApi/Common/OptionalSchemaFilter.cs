using FirearmStudio.Application.Model;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FirearmStudio.WebApi.Common;

public sealed class OptionalSchemaFilter : ISchemaFilter
{
    private static readonly System.Reflection.PropertyInfo[] WritableProperties =
        typeof(OpenApiSchema).GetProperties().Where(p => p.CanRead && p.CanWrite).ToArray();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Optional<>))
        {
            return;
        }

        var underlyingType = type.GetGenericArguments()[0];
        var inner = context.SchemaGenerator.GenerateSchema(underlyingType, context.SchemaRepository);

        foreach (var property in WritableProperties)
        {
            property.SetValue(schema, property.GetValue(inner));
        }
    }
}
