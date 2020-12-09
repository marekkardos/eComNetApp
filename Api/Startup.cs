using System.IO;
using Api.ApiResponses;
using Api.StartupConfigurations;
using AutoMapper;
using Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using Api.Dtos.Mapping;
using API.Middleware;
using Core.Entities;
using MediatR;
using Microsoft.Extensions.FileProviders;

namespace Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
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

            services.AddMvc(m =>
                {
                    // e.g application/xml
                    m.ReturnHttpNotAcceptable = true;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = actionContext =>
                    {
                        var errors = actionContext.ModelState
                            .Where(e => e.Value.Errors.Count > 0)
                            .SelectMany(x => x.Value.Errors)
                            .Select(x => x.ErrorMessage).ToArray();

                        var errorResponse = new ApiValidationErrorResponse
                        {
                            Errors = errors
                        };

                        return new BadRequestObjectResult(errorResponse);
                    };
                });

            services.AddMediatR(typeof(BaseEntity));

            DaoConfigureDi.ConfigureServices(services, Configuration);

            services.AddAutoMapper(typeof(MappingProfiles));
            
            SwaggerConfiguration.ConfigureServices(services);

            ApiVersioning.Add(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseStatusCodePagesWithReExecute("/errors/{0}");

            //if (env.IsDevelopment())
            //{
            //    //app.UseDeveloperExceptionPage();
            //}

            app.UseHttpsRedirection();
            
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

            if (env.IsDevelopment())
            {
                SwaggerConfiguration.ConfigurePipeline(app);
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapFallbackToController("Index", "Fallback");
            });
        }
    }
}