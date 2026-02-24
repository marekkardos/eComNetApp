using Api.ApiResponses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Api.Controllers;

[Route("error")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorHandlerController(ILogger<ErrorHandlerController> logger, IWebHostEnvironment env) : BaseApiController
{
    public IActionResult HandleErrorDevelopment()
    {
        IExceptionHandlerFeature exceptionFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
        Exception exception = exceptionFeature?.Error;

        logger.LogError(exception, "An unhandled exception occurred: {Message}", exception?.Message);

        if (!env.IsDevelopment())
        {
            return new ObjectResult(new ApiExceptionResponse(HttpStatusCode.InternalServerError));
        }

        return new ObjectResult(new ApiExceptionResponse(
            HttpStatusCode.InternalServerError,
            exception?.Message,
            exception?.StackTrace
        ));
    }
}
