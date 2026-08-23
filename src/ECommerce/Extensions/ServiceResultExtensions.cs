using ECommerce.Common;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Extensions
{
    public static class ServiceResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ServiceResult<T> result, ControllerBase controller) =>
            result.Success
                ? controller.Ok(result.Data)
                : result.ErrorType switch
                {
                    ServiceErrorType.NotFound => controller.NotFound(result.ErrorMessage),
                    ServiceErrorType.Conflict => controller.Conflict(result.ErrorMessage),
                    ServiceErrorType.Validation => controller.UnprocessableEntity(result.ErrorMessage),
                    _ => controller.BadRequest(result.ErrorMessage)
                };
    }
}
