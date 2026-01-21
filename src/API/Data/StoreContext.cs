using Core.Entities;
using Core.Entities.OrderAggregate;
using Infrastructure.Data.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Data;

public class StoreContext : DbContext
{
    private readonly ILoggerFactory _logFactory;
    private readonly IConfiguration _config;

    public StoreContext(DbContextOptions<StoreContext> options, ILoggerFactory logFactory, IConfiguration config) : base(options)
    {
        _logFactory = logFactory;
        _config = config;
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductBrand> ProductBrands { get; set; }
    public DbSet<ProductType> ProductTypes { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<DeliveryMethod> DeliveryMethods { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLoggerFactory(_logFactory);

        var efLoggingConfigurationsStr = "Logging:Ef:";
        
        var enableDetailedErrorsStr = _config[efLoggingConfigurationsStr + "EnableDetailedErrors"];
        bool.TryParse(enableDetailedErrorsStr, out var enableDetailedErrors);
        optionsBuilder.EnableDetailedErrors(enableDetailedErrors);

        var enableSensitiveDataLoggingStr = _config[efLoggingConfigurationsStr + "EnableSensitiveDataLogging"];
        bool.TryParse(enableSensitiveDataLoggingStr, out var enableSensitiveDataLogging);
        optionsBuilder.EnableSensitiveDataLogging(enableSensitiveDataLogging);

        optionsBuilder
            .ConfigureWarnings(b => b.Log(
                (RelationalEventId.ConnectionOpened, LogLevel.Information),
                (RelationalEventId.ConnectionClosed, LogLevel.Information)));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new DeliveryMethodConiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}