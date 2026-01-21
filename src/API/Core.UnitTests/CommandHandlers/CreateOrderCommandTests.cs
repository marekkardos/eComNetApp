using Core.CommandHandlers;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications.Base;
using Microsoft.Extensions.Logging;
using Moq;

namespace Core.UnitTests.CommandHandlers;

[TestFixture]
public class CreateOrderCommandTests
{
    private Mock<ILogger<CreateOrderCommand>> _loggerMock = null!;
    private Mock<IBasketRepository> _basketRepoMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<IPaymentService> _paymentServiceMock = null!;
    private Mock<IGenericRepository<Product>> _productRepoMock = null!;
    private Mock<IGenericRepository<DeliveryMethod>> _deliveryMethodRepoMock = null!;
    private Mock<IGenericRepository<Order>> _orderRepoMock = null!;
    private CreateOrderCommand _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<CreateOrderCommand>>();
        _basketRepoMock = new Mock<IBasketRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _productRepoMock = new Mock<IGenericRepository<Product>>();
        _deliveryMethodRepoMock = new Mock<IGenericRepository<DeliveryMethod>>();
        _orderRepoMock = new Mock<IGenericRepository<Order>>();

        _unitOfWorkMock.Setup(u => u.Repository<Product>()).Returns(_productRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<DeliveryMethod>()).Returns(_deliveryMethodRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Order>()).Returns(_orderRepoMock.Object);

        _handler = new CreateOrderCommand(
            _loggerMock.Object,
            _basketRepoMock.Object,
            _unitOfWorkMock.Object,
            _paymentServiceMock.Object);
    }

    [Test]
    public void Handle_NullRequest_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(null!, CancellationToken.None));
    }

    [Test]
    public void Handle_NullBasketId_ThrowsArgumentNullException()
    {
        var request = new CreateOrderRequest(
            "test@example.com",
            1,
            null!,
            CreateTestAddress());

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_NullBuyerEmail_ThrowsArgumentNullException()
    {
        var request = new CreateOrderRequest(
            null!,
            1,
            "basket-123",
            CreateTestAddress());

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public void Handle_NullShippingAddress_ThrowsArgumentNullException()
    {
        var request = new CreateOrderRequest(
            "test@example.com",
            1,
            "basket-123",
            null!);

        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(request, CancellationToken.None));
    }

    [TestCase(0)]
    [TestCase(5)]
    [TestCase(-1)]
    public void Handle_InvalidDeliveryMethodId_ThrowsArgumentOutOfRangeException(int deliveryMethodId)
    {
        var request = new CreateOrderRequest(
            "test@example.com",
            deliveryMethodId,
            "basket-123",
            CreateTestAddress());

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await _handler.Handle(request, CancellationToken.None));
    }

    [Test]
    public async Task Handle_BasketNotFound_ReturnsNull()
    {
        var request = CreateValidRequest();
        _basketRepoMock.Setup(r => r.GetBasketAsync(request.BasketId))
            .ReturnsAsync((CustomerBasket)null!);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Handle_DeliveryMethodNotFound_ReturnsNull()
    {
        var request = CreateValidRequest();
        var basket = CreateTestBasket(request.BasketId);

        _basketRepoMock.Setup(r => r.GetBasketAsync(request.BasketId))
            .ReturnsAsync(basket);
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(CreateTestProduct(1));
        _deliveryMethodRepoMock.Setup(r => r.GetByIdAsync(request.DeliveryMethodId))
            .ReturnsAsync((DeliveryMethod)null!);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Handle_DatabaseSaveFails_ReturnsNull()
    {
        var request = CreateValidRequest();
        SetupSuccessfulDependencies(request);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(0);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Handle_SuccessfulOrderCreation_ReturnsOrder()
    {
        var request = CreateValidRequest();
        SetupSuccessfulDependencies(request);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(1);

        var result = await _handler.Handle(request, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.BuyerEmail, Is.EqualTo(request.BuyerEmail));
            Assert.That(result.ShipToAddress.FirstName, Is.EqualTo(request.ShippingAddress.FirstName));
            Assert.That(result.PaymentIntentId, Is.EqualTo("pi_test_123"));
        });
    }

    [Test]
    public async Task Handle_SuccessfulOrderCreation_AddsOrderToRepository()
    {
        var request = CreateValidRequest();
        SetupSuccessfulDependencies(request);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(1);

        await _handler.Handle(request, CancellationToken.None);

        _orderRepoMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }

    [Test]
    public async Task Handle_SuccessfulOrderCreation_CalculatesCorrectSubtotal()
    {
        var request = CreateValidRequest();
        var basket = CreateTestBasket(request.BasketId,
            new BasketItem { Id = 1, Quantity = 2 },
            new BasketItem { Id = 2, Quantity = 3 });

        _basketRepoMock.Setup(r => r.GetBasketAsync(request.BasketId))
            .ReturnsAsync(basket);
        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(CreateTestProduct(1, 10.00m));
        _productRepoMock.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(CreateTestProduct(2, 20.00m));
        _deliveryMethodRepoMock.Setup(r => r.GetByIdAsync(request.DeliveryMethodId))
            .ReturnsAsync(CreateTestDeliveryMethod());
        _orderRepoMock.Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync((Order)null!);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(1);

        Order? capturedOrder = null;
        _orderRepoMock.Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        await _handler.Handle(request, CancellationToken.None);

        // Subtotal = (10.00 * 2) + (20.00 * 3) = 20.00 + 60.00 = 80.00
        // Total = Subtotal + DeliveryMethod.Price = 80.00 + 5.00 = 85.00
        Assert.That(capturedOrder, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedOrder!.Subtotal, Is.EqualTo(80.00m));
            Assert.That(capturedOrder.GetTotal(), Is.EqualTo(85.00m));
        });
    }

    [Test]
    public async Task Handle_ExistingOrderWithSamePaymentIntent_DeletesExistingOrder()
    {
        var request = CreateValidRequest();
        var basket = CreateTestBasket(request.BasketId);
        var existingOrder = new Order { Id = 999 };

        _basketRepoMock.Setup(r => r.GetBasketAsync(request.BasketId))
            .ReturnsAsync(basket);
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(CreateTestProduct(1));
        _deliveryMethodRepoMock.Setup(r => r.GetByIdAsync(request.DeliveryMethodId))
            .ReturnsAsync(CreateTestDeliveryMethod());
        _orderRepoMock.Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync(existingOrder);
        _paymentServiceMock.Setup(p => p.CreateOrUpdatePaymentIntent(basket.PaymentIntentId))
            .ReturnsAsync(basket);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(1);

        await _handler.Handle(request, CancellationToken.None);

        _orderRepoMock.Verify(r => r.Delete(existingOrder), Times.Once);
        _paymentServiceMock.Verify(p => p.CreateOrUpdatePaymentIntent(basket.PaymentIntentId), Times.Once);
    }

    [Test]
    public async Task Handle_NoExistingOrder_DoesNotDeleteAnyOrder()
    {
        var request = CreateValidRequest();
        SetupSuccessfulDependencies(request);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(1);

        await _handler.Handle(request, CancellationToken.None);

        _orderRepoMock.Verify(r => r.Delete(It.IsAny<Order>()), Times.Never);
        _paymentServiceMock.Verify(p => p.CreateOrUpdatePaymentIntent(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Handle_MultipleBasketItems_CreatesOrderItemsForEach()
    {
        var request = CreateValidRequest();
        var basket = CreateTestBasket(request.BasketId,
            new BasketItem { Id = 1, Quantity = 1 },
            new BasketItem { Id = 2, Quantity = 2 },
            new BasketItem { Id = 3, Quantity = 3 });

        _basketRepoMock.Setup(r => r.GetBasketAsync(request.BasketId))
            .ReturnsAsync(basket);
        _productRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(CreateTestProduct(1));
        _productRepoMock.Setup(r => r.GetByIdAsync(2))
            .ReturnsAsync(CreateTestProduct(2));
        _productRepoMock.Setup(r => r.GetByIdAsync(3))
            .ReturnsAsync(CreateTestProduct(3));
        _deliveryMethodRepoMock.Setup(r => r.GetByIdAsync(request.DeliveryMethodId))
            .ReturnsAsync(CreateTestDeliveryMethod());
        _orderRepoMock.Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync((Order)null!);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(1);

        Order? capturedOrder = null;
        _orderRepoMock.Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        await _handler.Handle(request, CancellationToken.None);

        Assert.That(capturedOrder, Is.Not.Null);
        Assert.That(capturedOrder!.OrderItems, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task Handle_OrderCreation_SetsPaymentIntentIdFromBasket()
    {
        var request = CreateValidRequest();
        var basket = CreateTestBasket(request.BasketId);
        basket.PaymentIntentId = "pi_custom_intent_456";

        _basketRepoMock.Setup(r => r.GetBasketAsync(request.BasketId))
            .ReturnsAsync(basket);
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(CreateTestProduct(1));
        _deliveryMethodRepoMock.Setup(r => r.GetByIdAsync(request.DeliveryMethodId))
            .ReturnsAsync(CreateTestDeliveryMethod());
        _orderRepoMock.Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync((Order)null!);
        _unitOfWorkMock.Setup(u => u.Complete()).ReturnsAsync(1);

        Order? capturedOrder = null;
        _orderRepoMock.Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        await _handler.Handle(request, CancellationToken.None);

        Assert.That(capturedOrder, Is.Not.Null);
        Assert.That(capturedOrder!.PaymentIntentId, Is.EqualTo("pi_custom_intent_456"));
    }

    #region Helper Methods

    private static CreateOrderRequest CreateValidRequest()
    {
        return new CreateOrderRequest(
            "test@example.com",
            1,
            "basket-123",
            CreateTestAddress());
    }

    private static Address CreateTestAddress()
    {
        return new Address("John", "Doe", "123 Test St", "Test City", "TS", "12345");
    }

    private static CustomerBasket CreateTestBasket(string basketId, params BasketItem[] items)
    {
        return new CustomerBasket(basketId)
        {
            PaymentIntentId = "pi_test_123",
            Items = items.Length > 0 ? [.. items] : [new BasketItem { Id = 1, Quantity = 1 }]
        };
    }

    private static Product CreateTestProduct(int id, decimal price = 99.99m)
    {
        return new Product
        {
            Id = id,
            Name = $"Product {id}",
            Price = price,
            PictureUrl = $"images/product{id}.png"
        };
    }

    private static DeliveryMethod CreateTestDeliveryMethod()
    {
        return new DeliveryMethod
        {
            Id = 1,
            ShortName = "Standard",
            DeliveryTime = "3-5 days",
            Description = "Standard delivery",
            Price = 5.00m
        };
    }

    private void SetupSuccessfulDependencies(CreateOrderRequest request)
    {
        var basket = CreateTestBasket(request.BasketId);

        _basketRepoMock.Setup(r => r.GetBasketAsync(request.BasketId))
            .ReturnsAsync(basket);
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(CreateTestProduct(1));
        _deliveryMethodRepoMock.Setup(r => r.GetByIdAsync(request.DeliveryMethodId))
            .ReturnsAsync(CreateTestDeliveryMethod());
        _orderRepoMock.Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Order>>()))
            .ReturnsAsync((Order)null!);
    }

    #endregion
}
