using Core.Entities.OrderAggregate;
using MediatR;

namespace Core.CommandHandlers
{
    public class CreateOrderRequest : IRequest<Order>
    {
        public string BuyerEmail { get; }
        public int DeliveryMethodId { get; }
        public string BasketId { get; }
        public Address ShippingAddress { get; }

        public CreateOrderRequest(string buyerEmail, int deliveryMethodId, string basketId, Address shippingAddress)
        {
            BuyerEmail = buyerEmail;
            DeliveryMethodId = deliveryMethodId;
            BasketId = basketId;
            ShippingAddress = shippingAddress;
        }
    }
}