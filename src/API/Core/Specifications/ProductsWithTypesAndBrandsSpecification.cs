using Core.Entities;
using Core.Specifications.Base;
using Core.Specifications.Builder;

namespace Core.Specifications
{
    public class ProductsWithTypesAndBrandsSpecification : BaseSpecification<Product>
    {
        public ProductsWithTypesAndBrandsSpecification(ProductSpecParams productParams)
        {
            Query
                .Where(x =>
                    (string.IsNullOrEmpty(productParams.Search) || x.Name.ToLower().Contains(productParams.Search)) &&
                    (!productParams.BrandId.HasValue || x.ProductBrandId == productParams.BrandId) &&
                    (!productParams.TypeId.HasValue || x.ProductTypeId == productParams.TypeId))
                .Include(x => x.ProductType)
                .Include(x => x.ProductBrand);

            if (!string.IsNullOrEmpty(productParams.Sort))
            {
                switch (productParams.Sort)
                {
                    case "priceAsc":
                        Query.OrderBy(p => p.Price);
                        break;
                    case "priceDesc":
                        Query.OrderByDescending(p => p.Price);
                        break;
                    default:
                        Query.OrderBy(n => n.Name);
                        break;
                }
            }
            else
            {
                Query.OrderBy(n => n.Name);
            }

            Query.Paginate(productParams);
        }

        public ProductsWithTypesAndBrandsSpecification(int id)
        {
            Query
                .Where(x => x.Id == id)
                .Include(x => x.ProductType)
                .Include(x => x.ProductBrand);
        }
    }
}