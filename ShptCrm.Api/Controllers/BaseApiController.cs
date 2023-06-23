using Microsoft.AspNetCore.Mvc;
using ShptCrm.Models.Errors;

namespace ShptCrm.Api.Controllers
{
    [ApiController]
    public class BaseApiController:ControllerBase
    {
        protected async Task<IActionResult> ExceptionResult(Exception exception)
        {
            return exception switch
            {
                _ => BadRequest(new ResponseErrorModel(HttpContext.TraceIdentifier, exception.Message, exception.InnerException?.Message))
            } ;
        }
    }
}
