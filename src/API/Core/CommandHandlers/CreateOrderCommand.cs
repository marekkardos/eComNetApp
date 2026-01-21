using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.CommandHandlers
{
    public class CreateOrderCommand(
        ILogger<CreateOrderCommand> logger,
        IBasketRepository basketRepo,
        IUnitOfWork unitOfWork,
        IPaymentService paymentService) : IRequestHandler<CreateOrderRequest, Order>
    {
        public async Task<Order> Handle(CreateOrderRequest req, CancellationToken cancellationToken)
        {
            Guard.Against.Null(req, nameof(req));
            Guard.Against.Null(req.BasketId, nameof(req.BasketId));
            Guard.Against.Null(req.BuyerEmail, nameof(req.BuyerEmail));
            Guard.Against.Null(req.ShippingAddress, nameof(req.ShippingAddress));
            Guard.Against.OutOfRange(req.DeliveryMethodId, nameof(req.DeliveryMethodId), 1, 4);

            var basket = await basketRepo.GetBasketAsync(req.BasketId);

            if (basket == null)
            {
                logger.LogDebug("Basket not found for BasketId: {BasketId}", req.BasketId);
                return null;
            }

            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {
                var productItem = await unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
                items.Add(orderItem);
            }

            var deliveryMethod = await unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(req.DeliveryMethodId);

            if (deliveryMethod == null)
            {
                logger.LogWarning("DeliveryMethod not found for DeliveryMethodId: {DeliveryMethodId}", req.DeliveryMethodId);
                return null;
            }

            var subtotal = items.Sum(item => item.Price * item.Quantity);

            var spec = new OrderByPaymentIntentIdSpecification(basket.PaymentIntentId);
            var existingOrder = await unitOfWork.Repository<Order>().GetEntityWithSpec(spec.AsTracking());

            if (existingOrder != null)
            {
                logger.LogInformation("Replacing existing order (OrderId: {OrderId}) for PaymentIntentId: {PaymentIntentId}",
                    existingOrder.Id, basket.PaymentIntentId);
                unitOfWork.Repository<Order>().Delete(existingOrder);
                await paymentService.CreateOrUpdatePaymentIntent(basket.PaymentIntentId);
            }

            var order = new Order(items, req.BuyerEmail, req.ShippingAddress, deliveryMethod, subtotal,
                basket.PaymentIntentId);

            unitOfWork.Repository<Order>().Add(order);

            var result = await unitOfWork.Complete();

            if (result <= 0)
            {
                return null;
            }

            return order;
        }
    }
}
