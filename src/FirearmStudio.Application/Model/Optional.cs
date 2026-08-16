using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FirearmStudio.Application.Model;

[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    public Optional(T value)
    {
        IsSet = true;
        Value = value;
    }

    public bool IsSet { get; }

    public T Value { get; }

    public void ApplyTo(Action<T> set)
    {
        if (IsSet)
        {
            set(Value);
        }
    }
}

public static class OptionalHelpers
{
    public static bool HasAtLeastOneSet<T>(T value) where T : notnull =>
        CompiledOptionalProbe<T>.HasAnySet(value);

    private static class CompiledOptionalProbe<T>
    {
        private static readonly Func<T, bool> Probe = Compile();

        internal static bool HasAnySet(T value) => Probe(value);

        private static Func<T, bool> Compile()
        {
            var request = Expression.Parameter(typeof(T), "request");

            var isSetChecks = typeof(T).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                            && p.PropertyType.GetGenericTypeDefinition() == typeof(Optional<>))
                .Select(p => (Expression)Expression.Property(Expression.Property(request, p), nameof(Optional<int>.IsSet)))
                .ToList();

            if (isSetChecks.Count == 0)
            {
                return static _ => false;
            }

            return Expression.Lambda<Func<T, bool>>(isSetChecks.Aggregate(Expression.OrElse), request).Compile();
        }
    }
}

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
    {
        private static readonly bool IsNullableT =
            !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null;

        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (IsNullableT)
                {
                    return new(default!);
                }

                throw new JsonException($"null is not a valid value for {typeToConvert}.");
            }

            return new(JsonSerializer.Deserialize<T>(ref reader, options)!);
        }

        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Value, options);
    }
}
