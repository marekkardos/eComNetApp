using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;

namespace Api.StartupConfigurations
{
    public static class SwaggerConfiguration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("WeatherForecast", new OpenApiInfo
                {
                    Title = "Api",
                    Version = "v1",
                    Description = "description"
                });

                c.SwaggerDoc("ValuesControllerExample", new OpenApiInfo
                {
                    Title = "Api",
                    Version = "v1",
                    Description = "description"
                });

                c.SwaggerDoc("Products", new OpenApiInfo
                {
                    Title = "Api",
                    Version = "v1",
                    Description = "description"
                });

                c.IncludeXmlComments(
                    Path.Combine(AppContext.BaseDirectory,
                        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            });
        }

        internal static void ConfigurePipeline(IApplicationBuilder app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/Products/swagger.json", "Products Api");

                c.SwaggerEndpoint("/swagger/WeatherForecast/swagger.json", "WeatherForecast Api");

                c.SwaggerEndpoint("/swagger/ValuesControllerExample/swagger.json", "ValuesControllerExample Api");

                //c.RoutePrefix = "";
            });
        }
    }
}
