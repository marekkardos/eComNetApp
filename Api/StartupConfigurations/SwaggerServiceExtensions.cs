using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Api.StartupConfigurations;

public static class SwaggerServiceExtensions
{
    internal static void AddSwaggerServicesExt(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("Products", new OpenApiInfo
            {
                Title = "Api",
                Version = "v1",
                Description = "Products related API."
            });

            c.SwaggerDoc("Basket", new OpenApiInfo
            {
                Title = "Api",
                Version = "v1",
                Description = "Basket related API."
            });

            c.SwaggerDoc("Buggy", new OpenApiInfo
            {
                Title = "Api",
                Version = "v1",
                Description = "Buggy related API."
            });

            c.SwaggerDoc("Account", new OpenApiInfo
            {
                Title = "Api",
                Version = "v1",
                Description = "Account related API.",

            });

            c.SwaggerDoc("Orders", new OpenApiInfo
            {
                Title = "Api",
                Version = "v1",
                Description = "Orders related API.",

            });

            c.SwaggerDoc("Payments", new OpenApiInfo
            {
                Title = "Api",
                Version = "v1",
                Description = "Payments related API.",

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

            var xmlFilePath = Path.Combine(AppContext.BaseDirectory,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

            c.IncludeXmlComments(xmlFilePath);
        });
    }

    internal static void UseSwaggerExt(this IApplicationBuilder app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/Products/swagger.json", "Products Api");

            c.SwaggerEndpoint("/swagger/Basket/swagger.json", "Basket Api");

            c.SwaggerEndpoint("/swagger/Buggy/swagger.json", "Buggy Api");

            c.SwaggerEndpoint("/swagger/Account/swagger.json", "Account Api");

            c.SwaggerEndpoint("/swagger/Orders/swagger.json", "Orders Api");

            c.SwaggerEndpoint("/swagger/Payments/swagger.json", "Payments Api");
        });
    }
}
