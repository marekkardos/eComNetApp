using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Api.Helpers
{
    public class PictureUrlResolver : IPictureUrlResolver
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;

        public PictureUrlResolver(IWebHostEnvironment env, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            this._env = env;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetAbsolutePath(string pictureRelativePath)
        {
            var request = _httpContextAccessor.HttpContext.Request;

            var pathScheme = _env.IsDevelopment() ? request.Scheme : "https";

            var basePath = $"{pathScheme}://{request.Host}{request.PathBase}";

            return basePath + _config["ApiUrlContent"] + pictureRelativePath;
        }
    }
}