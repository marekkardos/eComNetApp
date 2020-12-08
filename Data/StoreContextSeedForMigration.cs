using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data
{
    public class StoreContextSeedForMigration
    {
        public static void Seed(ModelBuilder modelBuilder, ILoggerFactory loggerFactory)
        {
            try
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var dataPath = Path.GetFullPath(Path.Combine(path, @"..\..\..\..\"));

                SeedProductBrands(modelBuilder, dataPath);

                SeedProductTypes(modelBuilder, dataPath);

                SeedProducts(modelBuilder, dataPath);

                SeedDeliveryMethods(modelBuilder, dataPath);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger<StoreContextSeed>();
                logger.LogError(ex.Message);
            }
        }

        private static void SeedDeliveryMethods(ModelBuilder modelBuilder, string path)
        {
            var dmData =
                File.ReadAllText(path + @"/Data/SeedData/delivery.json");

            var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(dmData);

            modelBuilder.Entity<DeliveryMethod>().HasData(methods ?? new List<DeliveryMethod>());
        }

        private static void SeedProducts(ModelBuilder modelBuilder, string path)
        {
            var productsData =
                File.ReadAllText(path + @"/Data/SeedData/products.json");

            var products = JsonSerializer.Deserialize<List<Product>>(productsData);

            if (products == null)
            {
                return;
            }

            foreach (var idx in Enumerable.Range(0, products.Count))
            {
                if (products[idx].Id == 0)
                {
                    products[idx].Id = -(idx + 1);
                }
            }

            modelBuilder.Entity<Product>().HasData(products);
        }

        private static void SeedProductTypes(ModelBuilder modelBuilder, string path)
        {
            var typesData =
                File.ReadAllText(path + @"/Data/SeedData/types.json");

            var types = JsonSerializer.Deserialize<List<ProductType>>(typesData);

            modelBuilder.Entity<ProductType>().HasData(types ?? new List<ProductType>());
        }

        private static void SeedProductBrands(ModelBuilder modelBuilder, string path)
        {
            var brandsData =
                File.ReadAllText(path + @"/Data/SeedData/brands.json");

            var brands = JsonSerializer.Deserialize<List<ProductBrand>>(brandsData);

            modelBuilder.Entity<ProductBrand>().HasData(brands ?? new List<ProductBrand>());
        }
    }
}