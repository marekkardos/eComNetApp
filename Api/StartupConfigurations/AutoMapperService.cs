using Api.Dtos.Mapping;
using AutoMapper;

namespace Api.StartupConfigurations;

public static class AutoMapperService
{
    public static IServiceCollection AddAutoMapperServiceExt(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfiles));

        return services;
    }
    
    public static IApplicationBuilder AssertAutoMapperConfigurationIsValid(this IApplicationBuilder app)
    {
        // Build the service provider to resolve AutoMapper's IMapper
        var mapper = app.ApplicationServices.GetRequiredService<IMapper>(); 
        
        // Assert configuration is valid
        mapper.ConfigurationProvider.AssertConfigurationIsValid();

        return app;
    }
}