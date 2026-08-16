using ErrorOr;
using FirearmStudio.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Common;

public static class ErrorOrExtensions
{
    public static ActionResult ToActionResult<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            var error = result.Errors.FirstOrDefault();
            var statusCode = StatusCode(error);
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

    private static int StatusCode(Error error)
    {
        if ((int)error.Type == UpstreamErrorTypes.UpstreamFailure)
        {
            return StatusCodes.Status502BadGateway;
        }

        if ((int)error.Type == ThrottleErrorTypes.Throttled)
        {
            return StatusCodes.Status429TooManyRequests;
        }

        return error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
