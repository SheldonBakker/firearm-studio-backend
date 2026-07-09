using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Common;

public static class ErrorOrExtensions
{
    public static ActionResult ToActionResult<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            var error = result.Errors.FirstOrDefault();
            var statusCode = StatusCode(result);
            var problem = new ProblemDetails
            {
                Detail = error.Description,
                Status = statusCode,
            };
            problem.Extensions["code"] = error.Code;

            return new ObjectResult(problem)
            {
                StatusCode = statusCode,
            };
        }

        return result.Value switch
        {
            Updated or Deleted or Success => new NoContentResult(),
            var value => new OkObjectResult(value),
        };
    }

    private static int StatusCode<T>(ErrorOr<T> result) => result.Errors.FirstOrDefault().Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Failure => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError,
    };
}
