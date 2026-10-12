using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WatchStore.Core.DTOs;
using WatchStore.Core.Entities;
using WatchStore.Core.Exceptions;
using WatchStore.Core.Interfaces;
using WatchStore.Service.Profiles;
using WatchStore.Service.Services;

namespace WatchStore.Tests.Services
{
    // בדיקות הלוגיקה של המשאב המוגבל (מלאי) בשכבת ה-Service, עם repositories מדומים
    public class OrderServiceTests
    {
        private const int StoreId = 1;
        private const int ProductId = 10;

        private readonly Mock<IOrderRepository> _orderRepository = new();
        private readonly Mock<IProductRepository> _productRepository = new();
        private readonly Mock<IStoreRepository> _storeRepository = new();
        private readonly OrderService _service;

        private Order? _savedOrder;

        public OrderServiceTests()
        {
            var mapper = new MapperConfiguration(cfg =>
                    cfg.AddMaps(typeof(OrderProfile).Assembly))
                .CreateMapper();

            _storeRepository
                .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Store { Id = StoreId, Name = "Test Store", IsActive = true });

            _orderRepository
                .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
                .Callback<Order, CancellationToken>((order, _) => _savedOrder = order)
                .Returns(Task.CompletedTask);

            // אחרי השמירה ה-Service טוען את ההזמנה מחדש לצורך התשובה
            _orderRepository
                .Setup(r => r.GetByIdWithItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => _savedOrder);

            _service = new OrderService(
                _orderRepository.Object,
                _productRepository.Object,
                _storeRepository.Object,
                mapper,
                NullLogger<OrderService>.Instance);
        }

        [Fact]
        public async Task CreateAsync_WhenStockIsSufficient_CreatesOrderAndReducesStock()
        {
            // Stock = 5, Requested = 2
            var product = GivenProduct(stock: 5, price: 100m);

            var result = await _service.CreateAsync(StoreId, OrderFor(quantity: 2));

            Assert.Equal(3, product.StockQuantity);
            Assert.Equal(200m, result.TotalPrice);
            Assert.Single(result.Items);
            Assert.Equal(2, result.Items[0].Quantity);
            _orderRepository.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenStockIsInsufficient_RejectsWithoutSaving()
        {
            // Stock = 1, Requested = 2
            var product = GivenProduct(stock: 1, price: 100m);

            await Assert.ThrowsAsync<ConflictException>(
                () => _service.CreateAsync(StoreId, OrderFor(quantity: 2)));

            Assert.Equal(1, product.StockQuantity);
            _orderRepository.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenAnotherStoreTookTheStockFirst_ThrowsConflictWithClearMessage()
        {
            // שתי חנויות קראו מלאי 1. השנייה מגיעה לשמירה אחרי שה-RowVersion כבר השתנה
            GivenProduct(stock: 1, price: 100m);

            _orderRepository
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConcurrencyConflictException(
                    "row changed",
                    new Exception("inner")));

            var ex = await Assert.ThrowsAsync<ConflictException>(
                () => _service.CreateAsync(StoreId, OrderFor(quantity: 1)));

            Assert.Equal(OrderService.StockConflictMessage, ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenProductDoesNotExist_ThrowsNotFound()
        {
            _productRepository
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product>());

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.CreateAsync(StoreId, OrderFor(quantity: 1)));
        }

        [Fact]
        public async Task CreateAsync_WhenProductIsInactive_ThrowsNotFound()
        {
            var product = GivenProduct(stock: 5, price: 100m);
            product.IsActive = false;

            await Assert.ThrowsAsync<NotFoundException>(
                () => _service.CreateAsync(StoreId, OrderFor(quantity: 1)));
        }

        [Fact]
        public async Task CreateAsync_WhenStoreIsInactive_ThrowsForbidden()
        {
            _storeRepository
                .Setup(r => r.GetByIdAsync(StoreId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Store { Id = StoreId, IsActive = false });

            await Assert.ThrowsAsync<ForbiddenException>(
                () => _service.CreateAsync(StoreId, OrderFor(quantity: 1)));
        }

        [Fact]
        public async Task CreateAsync_WhenSameProductAppearsTwice_ChecksTheCombinedQuantity()
        {
            // 2 + 2 = 4 > 3: כל שורה בנפרד הייתה עוברת, אבל ביחד אין מספיק מלאי
            GivenProduct(stock: 3, price: 100m);

            var request = new CreateOrderRequest
            {
                Items =
                {
                    new CreateOrderItemRequest { ProductId = ProductId, Quantity = 2 },
                    new CreateOrderItemRequest { ProductId = ProductId, Quantity = 2 }
                }
            };

            await Assert.ThrowsAsync<ConflictException>(
                () => _service.CreateAsync(StoreId, request));
        }

        [Fact]
        public async Task GetByIdAsync_WhenOrderBelongsToAnotherStore_ThrowsForbidden()
        {
            _orderRepository
                .Setup(r => r.GetByIdWithItemsAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Order { Id = 5, StoreId = 2 });

            await Assert.ThrowsAsync<ForbiddenException>(
                () => _service.GetByIdAsync(5, requesterStoreId: StoreId));
        }

        private Product GivenProduct(int stock, decimal price)
        {
            var product = new Product
            {
                Id = ProductId,
                Name = "Submariner",
                ModelNumber = "ROLEX-001",
                StockQuantity = stock,
                WholesalePrice = price,
                IsActive = true
            };

            _productRepository
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Product> { product });

            return product;
        }

        private static CreateOrderRequest OrderFor(int quantity) => new()
        {
            Items = { new CreateOrderItemRequest { ProductId = ProductId, Quantity = quantity } }
        };
    }
}
