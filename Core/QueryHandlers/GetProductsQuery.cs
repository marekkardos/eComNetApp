using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using MediatR;

namespace Core.QueryHandlers
{
    /// <summary>
    ///  https://github.com/jbogard/MediatR/wiki
    /// </summary>
    public class GetProductsQuery : IRequestHandler<GetProductsQueryRequest, GetProductsQueryResponse>
    {
        private readonly IGenericRepository<Product> _productsRepo;

        public GetProductsQuery(IGenericRepository<Product> productsRepo)
        {
            _productsRepo = productsRepo;
        }

        public async Task<GetProductsQueryResponse> Handle(GetProductsQueryRequest req, CancellationToken cancellationToken)
        {
            var spec = new ProductsWithTypesAndBrandsSpecification(req.ProductParams);

            var countSpec = new ProductWithFiltersForCountSpecificication(req.ProductParams);

            return new GetProductsQueryResponse
            {
                Data = await _productsRepo.ListAsync(spec),
                TotalItems = await _productsRepo.CountAsync(countSpec)
            }; ;
        }
    }

    public class GetProductsQueryRequest : IRequest<GetProductsQueryResponse>
    {
        public ProductSpecParams ProductParams { get; set; }
    }

    public class GetProductsQueryResponse
    {
        public int TotalItems { get; set; }

        public IReadOnlyList<Product> Data { get; set; }
    }
}