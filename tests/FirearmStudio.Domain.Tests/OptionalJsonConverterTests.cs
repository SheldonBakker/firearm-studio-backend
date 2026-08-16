using System.Text.Json;
using FirearmStudio.Application.Model;
using FirearmStudio.Domain.Enums;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class OptionalJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new OptionalJsonConverterFactory() },
    };

    [Fact]
    public void Null_json_for_non_nullable_int_throws_json_exception()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Optional<int>>("null", Options));
    }

    [Fact]
    public void Null_json_for_non_nullable_int_exception_has_descriptive_message()
    {
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Optional<int>>("null", Options));

        Assert.Contains("null", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Null_json_for_non_nullable_enum_throws_json_exception()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Optional<DepositMode>>("null", Options));
    }

    [Fact]
    public void Null_json_for_nullable_string_produces_set_optional_with_null_value()
    {
        var result = JsonSerializer.Deserialize<Optional<string?>>("null", Options);

        Assert.True(result.IsSet);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Null_json_for_nullable_value_type_produces_set_optional_with_null_value()
    {
        var result = JsonSerializer.Deserialize<Optional<DateOnly?>>("null", Options);

        Assert.True(result.IsSet);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Valid_int_value_deserializes_correctly()
    {
        var result = JsonSerializer.Deserialize<Optional<int>>("42", Options);

        Assert.True(result.IsSet);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Omitted_field_produces_unset_optional()
    {
        var json = "{}";
        var wrapper = JsonSerializer.Deserialize<WrapperWithInt>(json, Options);

        Assert.False(wrapper!.Value.IsSet);
    }

    private sealed record WrapperWithInt(Optional<int> Value);
}
