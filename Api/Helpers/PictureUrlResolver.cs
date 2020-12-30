using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Api.Helpers
{
    public class PictureUrlResolver : IPictureUrlResolver
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PictureUrlResolver(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetAbsolutePath(string pictureRelativePath)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var basePath = $"{request.Scheme}://{request.Host}{request.PathBase}";

            return basePath + _config["ApiUrlContent"] + pictureRelativePath;
        }
    }
}