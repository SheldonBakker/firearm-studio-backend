using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Common;

public static class ErrorOrExtensions
{
    public static ActionResult ToProblem<T>(this ControllerBase controller, ErrorOr<T> result) =>
        controller.Problem(detail: result.Errors.FirstOrDefault().Description, statusCode: StatusCode(result));

    public static ActionResult ToActionResult<T>(this ErrorOr<T> result, string? successMessage = null)
    {
        if (result.IsError)
        {
            var error = result.Errors.FirstOrDefault();
            var statusCode = StatusCode(result);
            return new ObjectResult(new ProblemDetails { Detail = error.Description, Status = statusCode })
            {
                StatusCode = statusCode,
            };
        }

        return result.Value switch
        {
            Created => new StatusCodeResult(StatusCodes.Status201Created),
            Updated or Deleted or Success =>
                successMessage is null
                    ? new NoContentResult()
                    : new OkObjectResult(new { message = successMessage }),
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
        _ => StatusCodes.Status500InternalServerError,
    };
}
