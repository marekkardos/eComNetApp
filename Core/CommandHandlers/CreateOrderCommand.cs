using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Ex;
using Core.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.CommandHandlers
{
    public class CreateOrderCommand : IRequestHandler<CreateOrderRequest, Order>
    {
        private readonly ILogger<CreateOrderCommand> _logger;
        //private readonly IPaymentService _paymentService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBasketRepository _basketRepo;

        public CreateOrderCommand(ILogger<CreateOrderCommand> logger, IBasketRepository basketRepo,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            //_paymentService = paymentService;
            _unitOfWork = unitOfWork;
            _basketRepo = basketRepo;
        }

        public async Task<Order> Handle(CreateOrderRequest req, CancellationToken cancellationToken)
        {
            Guard.Against.Null(req, nameof(req));
            Guard.Against.Null(req.BasketId, nameof(req.BasketId));
            Guard.Against.Null(req.BuyerEmail, nameof(req.BuyerEmail));
            Guard.Against.Null(req.ShippingAddress, nameof(req.ShippingAddress));
            Guard.Against.OutOfRange(req.DeliveryMethodId, nameof(req.DeliveryMethodId), 1, 4);

            // get basket from the repo
            var basket = await _basketRepo.GetBasketAsync(req.BasketId);

            if (basket == null)
            {
                _logger.LogWarning($"basket == null for basketId:{req.BasketId}");

                throw new CreateOrderException($"Cannot find Basket with id:{req.BasketId}");
            }

            // get items from the product repo
            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {
                var productItem = await _unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity);
                items.Add(orderItem);
            }

            // get delivery method from repo
            var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(req.DeliveryMethodId);

            if (deliveryMethod == null)
            {
                _logger.LogWarning($"deliveryMethod == null for DeliveryMethodId:{req.DeliveryMethodId}");

                throw new CreateOrderException($"Cannot find DeliveryMethod with id:{req.DeliveryMethodId}");
            }

            // calc subtotal
            var subtotal = items.Sum(item => item.Price * item.Quantity);

            // check to see if order exists
            //var spec = new OrderByPaymentIntentIdSpecification(basket.PaymentIntentId);
            //var existingOrder = await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec.AsTacking());

            //if (existingOrder != null)
            //{
            //    _unitOfWork.Repository<Order>().Delete(existingOrder);
            //    //await _paymentService.CreateOrUpdatePaymentIntent(basket.PaymentIntentId);
            //}

            var order = new Order(items, req.BuyerEmail, req.ShippingAddress, deliveryMethod, subtotal,
                basket.PaymentIntentId);

            _unitOfWork.Repository<Order>().Add(order);

            // save to db
            var result = await _unitOfWork.Complete();

            return result <= 0 ? null : order;
        }
    }
}