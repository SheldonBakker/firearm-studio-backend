using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace FirearmStudio.WebApi.Common;

public static class ErrorOrExtensions
{
    public static ActionResult ToProblem<T>(this ControllerBase controller, ErrorOr<T> result)
    {
        var error = result.Errors.FirstOrDefault();

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return controller.Problem(detail: error.Description, statusCode: statusCode);
    }
}
