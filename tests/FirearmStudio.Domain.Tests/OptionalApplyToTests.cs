using FirearmStudio.Application.Model;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class OptionalApplyToTests
{
    [Fact]
    public void ApplyTo_calls_action_when_set()
    {
        var optional = new Optional<int>(42);
        var captured = 0;

        optional.ApplyTo(v => captured = v);

        Assert.Equal(42, captured);
    }

    [Fact]
    public void ApplyTo_does_not_call_action_when_not_set()
    {
        Optional<int> optional = default;
        var called = false;

        optional.ApplyTo(_ => called = true);

        Assert.False(called);
    }

    [Fact]
    public void ApplyTo_passes_correct_value_to_action()
    {
        var optional = new Optional<string?>("hello");
        string? captured = null;

        optional.ApplyTo(v => captured = v);

        Assert.Equal("hello", captured);
    }

    [Fact]
    public void ApplyTo_passes_null_value_when_set_to_null()
    {
        var optional = new Optional<string?>(null);
        var called = false;
        string? captured = "default";

        optional.ApplyTo(v => { called = true; captured = v; });

        Assert.True(called);
        Assert.Null(captured);
    }

    [Fact]
    public void HasAtLeastOneSet_returns_true_when_one_optional_property_is_set()
    {
        var request = new TestPatchRequest(new Optional<string?>("value"), default);

        Assert.True(OptionalHelpers.HasAtLeastOneSet(request));
    }

    [Fact]
    public void HasAtLeastOneSet_returns_false_when_no_optional_properties_are_set()
    {
        var request = new TestPatchRequest(default, default);

        Assert.False(OptionalHelpers.HasAtLeastOneSet(request));
    }

    [Fact]
    public void HasAtLeastOneSet_returns_true_when_second_optional_property_is_set()
    {
        var request = new TestPatchRequest(default, new Optional<int>(5));

        Assert.True(OptionalHelpers.HasAtLeastOneSet(request));
    }

    [Fact]
    public void HasAtLeastOneSet_returns_true_when_all_optional_properties_are_set()
    {
        var request = new TestPatchRequest(new Optional<string?>("x"), new Optional<int>(1));

        Assert.True(OptionalHelpers.HasAtLeastOneSet(request));
    }

    private sealed record TestPatchRequest(Optional<string?> Name, Optional<int> Count);
}
