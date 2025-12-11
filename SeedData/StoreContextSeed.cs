using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SeedData
{
    public static class StoreContextSeed
    {
        public static async Task SeedAsync(StoreContext context, ILoggerFactory loggerFactory)
        {
            var strategy = context.Database.CreateExecutionStrategy();

            try
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                path = Path.GetFullPath(Path.Combine(path, @"..\..\..\"));

                if (!await context.ProductBrands.AnyAsync())
                {
                    var brandsData =
                        await File.ReadAllTextAsync(path + @"/Data/brands.json");

                    var brands = JsonSerializer.Deserialize<List<ProductBrand>>(brandsData) 
                                ?? throw new SeedDataException("Failed to deserialize ProductBrands data.");

                    await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await context.Database.BeginTransactionAsync())
                        {
                            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ProductBrands ON");
                            await context.ProductBrands.AddRangeAsync(brands);

                            await context.SaveChangesAsync();
                            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ProductBrands OFF");

                            await transaction.CommitAsync();
                        }
                    });
                }

                if (!await context.ProductTypes.AnyAsync())
                {
                    var typesData =
                        await File.ReadAllTextAsync(path + @"/Data/types.json");

                    var types = JsonSerializer.Deserialize<List<ProductType>>(typesData)
                        ?? throw new SeedDataException("Failed to deserialize ProductTypes data.");


                    await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await context.Database.BeginTransactionAsync())
                        {
                            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ProductTypes ON");
                            await context.ProductTypes.AddRangeAsync(types);

                            await context.SaveChangesAsync();
                            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT ProductTypes OFF");

                            await transaction.CommitAsync();
                        }
                    });
                }

                if (!await context.Products.AnyAsync())
                {
                    var productsData =
                        await File.ReadAllTextAsync(path + @"/Data/products.json");

                    var products = JsonSerializer.Deserialize<List<Product>>(productsData) 
                        ?? throw new SeedDataException("Failed to deserialize Products data.");

                    await context.Products.AddRangeAsync(products);

                    await context.SaveChangesAsync();
                }

                if (!await context.DeliveryMethods.AnyAsync())
                {
                    var dmData =
                        await File.ReadAllTextAsync(path + @"/Data/delivery.json");

                    var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(dmData)
                        ?? throw new SeedDataException("Failed to deserialize DeliveryMethod data.");

                    await strategy.ExecuteAsync(async () =>
                    {
                        using (var transaction = await context.Database.BeginTransactionAsync())
                        {
                            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT DeliveryMethods ON");
                            await context.DeliveryMethods.AddRangeAsync(methods);

                            await context.SaveChangesAsync();
                            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT DeliveryMethods OFF");

                            await transaction.CommitAsync();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger(nameof(StoreContextSeed));
                logger.LogError(ex, "StoreContextSeed error.");
            }
        }
    }
}