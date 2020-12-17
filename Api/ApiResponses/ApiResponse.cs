using System.Net;

namespace Api.ApiResponses
{
    public class ApiResponse
    {
        public ApiResponse(HttpStatusCode statusCode, string message = null)
        {
            StatusCode = (int)statusCode;
            Message = message ?? GetDefaultMessageForStatusCode((int)statusCode);
        }

        public int StatusCode { get; set; }
        public string Message { get; set; }

        private string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "A bad request, you have made",
                401 => "Authorized, you are not",
                404 => "Resource found, it was not",
                500 => "Server Error",
                _ => null
            };
        }
    }
}