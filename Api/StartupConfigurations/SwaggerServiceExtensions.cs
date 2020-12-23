using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;

namespace Api.StartupConfigurations
{
    public static class SwaggerServiceExtensions
    {
        public static void AddCustomSwaggerServices(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("WeatherForecast", new OpenApiInfo
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

                c.SwaggerDoc("Basket", new OpenApiInfo
                {
                    Title = "Api",
                    Version = "v1",
                    Description = "description"
                });

                c.SwaggerDoc("Buggy", new OpenApiInfo
                {
                    Title = "Api",
                    Version = "v1",
                    Description = "description"
                });

                c.SwaggerDoc("Account", new OpenApiInfo
                {
                    Title = "Api",
                    Version = "v1",
                    Description = "description",
                    
                });

                c.SwaggerDoc("Orders", new OpenApiInfo
                {
                    Title = "Api",
                    Version = "v1",
                    Description = "description",

                });

                var securitySchema = new OpenApiSecurityScheme
                {
                    Description = "JWT Auth Bearer Scheme",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };

                c.AddSecurityDefinition("Bearer", securitySchema);
                var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } };
                c.AddSecurityRequirement(securityRequirement);


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

                c.SwaggerEndpoint("/swagger/Basket/swagger.json", "Basket Api");

                c.SwaggerEndpoint("/swagger/WeatherForecast/swagger.json", "WeatherForecast Api");

                c.SwaggerEndpoint("/swagger/Buggy/swagger.json", "Buggy Api");

                c.SwaggerEndpoint("/swagger/Account/swagger.json", "Account Api");

                c.SwaggerEndpoint("/swagger/Orders/swagger.json", "Orders Api");

                //c.RoutePrefix = "";
            });
        }
    }
}
