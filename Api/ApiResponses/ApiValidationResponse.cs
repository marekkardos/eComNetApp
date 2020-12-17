using System.Collections.Generic;
using System.Net;

namespace Api.ApiResponses
{
    public class ApiValidationErrorResponse : ApiResponse
    {
        public ApiValidationErrorResponse() : base(HttpStatusCode.BadRequest)
        {
        }
        public IEnumerable<string> Errors { get; set; }
    }
}