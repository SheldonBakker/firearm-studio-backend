using System.Collections.Concurrent;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FirearmStudio.WebApi.Common;

public sealed class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    private static readonly ConcurrentDictionary<Type, Type> ValidatorTypeCache = new();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var argumentType = argument.GetType();
            var validatorType = ValidatorTypeCache.GetOrAdd(
                argumentType,
                t => typeof(IValidator<>).MakeGenericType(t));

            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                var details = new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest };
                details.Extensions["code"] = "validation_failed";
                context.Result = new BadRequestObjectResult(details);
                return;
            }
        }

        await next();
    }
}
