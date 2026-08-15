using ErrorOr;
using FirearmStudio.Application.Auth;
using FirearmStudio.Application.Common;
using FirearmStudio.WebApi.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FirearmStudio.WebApi.Tests;

public sealed class ErrorOrExtensionsTests
{
    private static (int? StatusCode, string? Code, string? Detail) Map(Error error)
    {
        var result = ((ErrorOr<Success>)error).ToActionResult();

        var objectResult = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);

        return (objectResult.StatusCode, problem.Extensions["code"] as string, problem.Detail);
    }

    [Fact]
    public void Throttled_challenge_is_answered_with_429()
    {
        var mapped = Map(Error.Custom(
            ThrottleErrorTypes.Throttled,
            AuthErrorCodes.ChallengeUnavailable,
            "Too many codes have been requested recently. Try again later."));

        Assert.Equal(StatusCodes.Status429TooManyRequests, mapped.StatusCode);
        Assert.Equal(AuthErrorCodes.ChallengeUnavailable, mapped.Code);
        Assert.Equal("Too many codes have been requested recently. Try again later.", mapped.Detail);
    }

    [Fact]
    public void Unavailable_phone_channel_is_answered_with_502()
    {
        var mapped = Map(Error.Custom(
            UpstreamErrorTypes.UpstreamFailure,
            AuthErrorCodes.PhoneChannelUnavailable,
            "A verification code could not be sent to that number right now. Try again later."));

        Assert.Equal(StatusCodes.Status502BadGateway, mapped.StatusCode);
        Assert.Equal(AuthErrorCodes.PhoneChannelUnavailable, mapped.Code);
    }

    [Fact]
    public void Generic_failures_are_still_answered_with_500()
    {
        var mapped = Map(Error.Failure("Some.Failure", "Something went wrong."));

        Assert.Equal(StatusCodes.Status500InternalServerError, mapped.StatusCode);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    public void Built_in_error_types_keep_their_status_codes(ErrorType type, int expected)
    {
        var mapped = Map(Error.Custom((int)type, "Some.Code", "Some description."));

        Assert.Equal(expected, mapped.StatusCode);
    }
}
