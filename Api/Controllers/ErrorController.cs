using System.Net;
using Api.ApiResponses;
using Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("errors/{code}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : BaseApiController
    {
        public IActionResult Error(HttpStatusCode code)
        {
            return new ObjectResult(new ApiResponse(code));
        }
    }
}