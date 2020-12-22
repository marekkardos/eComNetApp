using System;
using System.Linq;
using System.Reflection;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Data
{
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
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                SqlLiteSpecificMapping(modelBuilder); ;
            }

            //StoreContextSeedForMigration.Seed(modelBuilder, _logFactory);
        }

        private static void SqlLiteSpecificMapping(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(decimal));
                var dateTimeProperties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset));

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();
                }

                foreach (var property in dateTimeProperties)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name)
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}