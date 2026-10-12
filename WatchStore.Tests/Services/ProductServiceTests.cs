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
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _productRepository = new();
        private readonly Mock<ICategoryRepository> _categoryRepository = new();
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            var mapper = new MapperConfiguration(cfg =>
                    cfg.AddMaps(typeof(ProductProfile).Assembly))
                .CreateMapper();

            _service = new ProductService(
                _productRepository.Object,
                _categoryRepository.Object,
                mapper,
                NullLogger<ProductService>.Instance);
        }

        [Fact]
        public async Task CreateAsync_WhenCategoryDoesNotExist_ThrowsBusinessRule()
        {
            _categoryRepository
                .Setup(r => r.ExistsAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.CreateAsync(NewProduct(categoryId: 99)));

            _productRepository.Verify(
                r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenModelNumberIsTaken_ThrowsConflict()
        {
            _categoryRepository
                .Setup(r => r.ExistsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _productRepository
                .Setup(r => r.ModelNumberExistsAsync("CASIO-001", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(
                () => _service.CreateAsync(NewProduct(categoryId: 1)));
        }

        [Fact]
        public async Task DeleteAsync_DeactivatesInsteadOfRemoving()
        {
            var product = new Product { Id = 1, IsActive = true };

            _productRepository
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var deleted = await _service.DeleteAsync(1);

            Assert.True(deleted);
            Assert.False(product.IsActive);
            _productRepository.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_HidesInactiveProductFromStores()
        {
            _productRepository
                .Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Product
                {
                    Id = 1,
                    IsActive = false,
                    Category = new Category { Name = "Men" }
                });

            Assert.Null(await _service.GetByIdAsync(1, includeInactive: false));
            Assert.NotNull(await _service.GetByIdAsync(1, includeInactive: true));
        }

        private static CreateProductRequest NewProduct(int categoryId) => new()
        {
            ModelNumber = "CASIO-001",
            Name = "G-Shock",
            Brand = "Casio",
            CategoryId = categoryId,
            WholesalePrice = 100,
            RecommendedRetailPrice = 150,
            StockQuantity = 5
        };
    }
}
