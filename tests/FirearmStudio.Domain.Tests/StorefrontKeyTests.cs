using System.Text.RegularExpressions;
using FirearmStudio.Domain.Common;
using Xunit;

namespace FirearmStudio.Domain.Tests;

public class StorefrontKeyTests
{
    [Fact]
    public void Generate_returns_exactly_length_characters()
    {
        var key = StorefrontKey.Generate();

        Assert.Equal(StorefrontKeyConstants.Length, key.Length);
    }

    [Fact]
    public void Generate_starts_with_prefix()
    {
        var key = StorefrontKey.Generate();

        Assert.StartsWith(StorefrontKeyConstants.Prefix, key, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_suffix_contains_only_base64url_characters()
    {
        var key = StorefrontKey.Generate();
        var suffix = key[StorefrontKeyConstants.Prefix.Length..];

        Assert.Matches(new Regex("^[A-Za-z0-9_-]+$"), suffix);
    }

    [Fact]
    public void Generate_returns_different_values_on_two_calls()
    {
        var first = StorefrontKey.Generate();
        var second = StorefrontKey.Generate();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Matches_returns_true_for_equal_strings()
    {
        var key = StorefrontKey.Generate();

        Assert.True(StorefrontKey.Matches(key, key));
    }

    [Fact]
    public void Matches_returns_false_for_different_key()
    {
        var first = StorefrontKey.Generate();
        var second = StorefrontKey.Generate();

        Assert.False(StorefrontKey.Matches(first, second));
    }

    [Fact]
    public void Matches_returns_false_when_stored_is_null()
    {
        Assert.False(StorefrontKey.Matches(null, StorefrontKey.Generate()));
    }

    [Fact]
    public void Matches_returns_false_when_provided_is_null()
    {
        Assert.False(StorefrontKey.Matches(StorefrontKey.Generate(), null));
    }

    [Fact]
    public void Matches_returns_false_when_stored_is_empty()
    {
        Assert.False(StorefrontKey.Matches(string.Empty, StorefrontKey.Generate()));
    }

    [Fact]
    public void Matches_returns_false_when_provided_is_empty()
    {
        Assert.False(StorefrontKey.Matches(StorefrontKey.Generate(), string.Empty));
    }

    [Fact]
    public void Matches_returns_false_for_prefix_only_value()
    {
        var key = StorefrontKey.Generate();

        Assert.False(StorefrontKey.Matches(key, StorefrontKeyConstants.Prefix));
    }

    [Fact]
    public void AuditSuffix_returns_last_four_characters()
    {
        var key = StorefrontKey.Generate();

        Assert.Equal(key[^StorefrontKeyConstants.AuditSuffixLength..], StorefrontKey.AuditSuffix(key));
    }

    [Fact]
    public void Length_equals_prefix_length_plus_43()
    {
        Assert.Equal(StorefrontKeyConstants.Length, StorefrontKeyConstants.Prefix.Length + 43);
    }
}
