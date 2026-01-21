using Api.Helpers;
using AutoMapper;
using Core.Entities.OrderAggregate;

namespace Api.Dtos.Mapping
{
    public class OrderItemUrlResolver : IValueResolver<OrderItem, OrderItemDto, string>
    {
        private readonly IPictureUrlResolver _urlResolver;


        public OrderItemUrlResolver(IPictureUrlResolver urlResolver)
        {
            _urlResolver = urlResolver;
        }

        public string Resolve(OrderItem source, OrderItemDto destination, string destMember, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source.ItemOrdered.PictureUrl))
            {
                return null;
            }

            return _urlResolver.GetAbsolutePath(source.ItemOrdered.PictureUrl);
        }
    }
}