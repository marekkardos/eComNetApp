using Api.StartupConfigurations;
using AutoMapper;
using Data;
using Api.Dtos.Mapping;
using Api.Helpers;
using Api.Identity;
using API.Middleware;
using Core.Entities;
using MediatR;
using Microsoft.Extensions.FileProviders;
using Services;
using StackExchange.Redis;
using Serilog;

namespace Api
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration conf, IWebHostEnvironment environment)
        {
            services.AddLogging(conf, environment);

            var redisConnection = conf.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is missing");

            services.AddControllers();

            services.AddCors(opt =>
            {
                opt.AddPolicy("CustomCorsPolicy", policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        //.AllowCredentials()
                        .AllowAnyOrigin();
                });
            });

            services.AddSingleton<IConnectionMultiplexer>(c =>
            {
                var configuration = ConfigurationOptions.Parse(redisConnection, true);
                return ConnectionMultiplexer.Connect(configuration);
            });

            //services.AddMvc(m =>
            //    {
            //        // e.g application/xml
            //        m.ReturnHttpNotAcceptable = true;
            //    })
            //    //.SetCompatibilityVersion(CompatibilityVersion.Latest)
            //    .ConfigureApiBehaviorOptions(options =>
            //    {
            //        options.InvalidModelStateResponseFactory = actionContext =>
            //        {
            //            var errors = actionContext.ModelState
            //                .Where(e => e.Value.Errors.Count > 0)
            //                .SelectMany(x => x.Value.Errors)
            //                .Select(x => x.ErrorMessage).ToArray();

            //            var errorResponse = new ApiValidationErrorResponse
            //            {
            //                Errors = errors
            //            };

            //            return new BadRequestObjectResult(errorResponse);
            //        };
            //    });

            services.AddMediatR(typeof(BaseEntity));
            services.AddAutoMapper(typeof(MappingProfiles));

            services.AddDataPersistenceServices(conf);
            services.AddCustomIdentityServices(conf);

            services.AddEndpointsApiExplorer();
            services.AddCustomSwaggerServices();

            services.AddCustomApiVersioning();
            services.AddScoped<IPictureUrlResolver, PictureUrlResolver>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddApplicationServices(conf);
        }

        public static void ConfigureApp(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSerilogRequestLogging();

            app.UseMiddleware<ExceptionMiddleware>();
            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            //app.UseHttpsRedirection();

            // allow wwwroot
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "Content")
                ),
                RequestPath = "/content"
            });

            app.UseRouting();

            // CORS headers are only sent on cross domain requests and the
            // ASP.NET CORS module is smart enough to detect whether a same domain request
            // is firing and if it is, doesn't send the headers. 
            // test with: testCORS.html in Api folder
            app.UseCors("CustomCorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            SwaggerServiceExtensions.ConfigurePipeline(app);

            //if (env.IsDevelopment())
            //{
            //    TelemetryConfiguration.Active.DisableTelemetry = true;
            //    TelemetryDebugWriter.IsTracingDisabled = true;
            //}

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapFallbackToController("Index", "Fallback");
            });
        }
    }
}