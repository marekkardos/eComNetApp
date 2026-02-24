using Api.ApiResponses;
using Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("errors/{code}")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public class ErrorController : BaseApiController
{
    [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch]
    public IActionResult Error(int code)
    {
        return new ObjectResult(new ApiResponse(code));
    }
}