using System;
using System.Net;
using Api.ApiResponses;
using Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiExplorerSettings(GroupName = "Buggy")]
    public class BuggyController : BaseApiController
    {
        public BuggyController()
        {
        }

        [HttpGet("testauth")]
        [Authorize]
        public ActionResult<string> GetSecretText()
        {
            return "secret stuff";
        }

        [HttpGet("notfound")]
        public ActionResult GetNotFoundRequest()
        {
            return NotFound(new ApiResponse(HttpStatusCode.NotFound));
        }

        [HttpGet("servererror")]
        public ActionResult GetServerError()
        {
            throw new Exception("simulate server error.");
        }

        [HttpGet("badrequest")]
        public ActionResult GetBadRequest()
        {
            return BadRequest(new ApiResponse(HttpStatusCode.BadRequest));
        }

        [HttpGet("badrequest/{id}")]
        public ActionResult GetNotFoundRequest(int id)
        {
            return Ok();
        }
    }
}