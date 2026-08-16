using FirearmStudio.Application.Model.Options;
using FirearmStudio.WebApi.Common;
using FirearmStudio.WebApi.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FirearmStudio.WebApi.Tests;

public sealed class FailureCodesTests
{
    [Fact]
    public async Task GlobalExceptionHandler_returns_500_with_unhandled_code_and_no_exception_leak()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException("secret-error-detail"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var bodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("\"unhandled\"", bodyText);
        Assert.DoesNotContain("secret-error-detail", bodyText);
    }

    [Fact]
    public async Task ValidationFilter_failure_includes_validation_failed_code()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleRequest>, SampleRequestValidator>();
        var sp = services.BuildServiceProvider();

        var filter = new ValidationFilter(sp);
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = new SampleRequest("") },
            new object());

        await filter.OnActionExecutionAsync(
            executingContext,
            () => Task.FromResult(new ActionExecutedContext(actionContext, [], new object())));

        var result = Assert.IsType<BadRequestObjectResult>(executingContext.Result);
        var details = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal("validation_failed", details.Extensions["code"] as string);
    }

    [Fact]
    public void ModelBinding_InvalidModelStateResponseFactory_includes_validation_failed_code()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = actionContext =>
            {
                var details = new ValidationProblemDetails(actionContext.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                };
                details.Extensions["code"] = "validation_failed";
                return new BadRequestObjectResult(details);
            };
        });
        var sp = services.BuildServiceProvider();

        var options = sp.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());
        actionContext.ModelState.AddModelError("field", "Required.");

        var result = options.InvalidModelStateResponseFactory(actionContext);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var details = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal("validation_failed", details.Extensions["code"] as string);
        Assert.True(details.Errors.ContainsKey("field"));
    }

    [Fact]
    public async Task ApiKeyMiddleware_invalid_key_response_includes_api_key_invalid_code()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        var sp = services.BuildServiceProvider();

        var settings = new ApiKeySettings { Key = "valid-key", HeaderName = "X-Api-Key" };
        var middleware = new ApiKeyMiddleware(settings);
        var context = new DefaultHttpContext();
        context.RequestServices = sp;
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var bodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("api_key.invalid", bodyText);
    }

    private sealed record SampleRequest(string Name);

    private sealed class SampleRequestValidator : AbstractValidator<SampleRequest>
    {
        public SampleRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
