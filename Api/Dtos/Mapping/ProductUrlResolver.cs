using Api.Helpers;
using AutoMapper;
using Core.Entities;

namespace Api.Dtos.Mapping

{
    public class ProductUrlResolver : IValueResolver<Product, ProductToReturnDto, string>
    {
        private readonly IPictureUrlResolver _urlResolver;

        public ProductUrlResolver(IPictureUrlResolver urlResolver)
        {
            _urlResolver = urlResolver;
        }

        public string Resolve(Product source, ProductToReturnDto destination, string destMember,
            ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source.PictureUrl))
            {
                return null;
            }

            return _urlResolver.GetAbsolutePath(source.PictureUrl);
        }
    }
}