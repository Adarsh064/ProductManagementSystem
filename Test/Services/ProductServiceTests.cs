using AutoMapper;
using Moq;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Mapping;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;
using ProductManagementSystem.Services.Data;
using Xunit;

namespace ProductManagementSystem.Tests.Services
{
    public class ProductServiceTests
    {
        // ── Shared test infrastructure ───────────────────────────────────────────
        private readonly Mock<IProductRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<ProductService>> _loggerMock;
        private readonly ProductService _sut; // System Under Test

        public ProductServiceTests()
        {
            _repoMock   = new Mock<IProductRepository>();
            _loggerMock = new Mock<ILogger<ProductService>>();

            // Use the real MappingProfile so mapping bugs are also caught by tests
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            });
            _mapper = config.CreateMapper();

            _sut = new ProductService(_repoMock.Object, _mapper, _loggerMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GetByIdAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenProductDoesNotExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

            // Act
            var result = await _sut.GetByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMappedDto_WhenProductExists()
        {
            // Arrange
            var product = new Product { ProductId = 1, ProductName = "Widget" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(product);

            // Act
            var result = await _sut.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result!.ProductId);                  // ProductId → Id mapping check
            Assert.Equal("Widget", result.ProductName);
        }

        [Fact]
        public async Task GetByIdAsync_MapsItemsCorrectly_WhenProductHasItems()
        {
            // Arrange
            var product = new Product
            {
                ProductId = 2,
                ProductName = "Bundle",
                Items = new List<Item>
                {
                    new Item { ItemId = 10, Quantity = 5, Price = 9.99m, ProductId = 2 }
                }
            };
            _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(product);

            // Act
            var result = await _sut.GetByIdAsync(2);

            // Assert
            Assert.Single(result!.Items);
            Assert.Equal(10, result.Items[0].ItemId);        // ItemId → Id mapping check
            Assert.Equal(5, result.Items[0].Quantity);
            Assert.Equal(9.99m, result.Items[0].Price);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GetAllAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsPaginatedResult_WithCorrectMetadata()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, ProductName = "Alpha" },
                new Product { ProductId = 2, ProductName = "Beta"  }
            };

            var param = new OrderParamDto { PageNumber = 1, PageSize = 10 };

            _repoMock
                .Setup(r => r.GetAllAsync(param))
                .ReturnsAsync((products, 2));

            // Act
            dynamic result = await _sut.GetAllAsync(param);

            // Assert — cast to anonymous-type via dynamic for property access
            Assert.Equal(1, result.Page);
            Assert.Equal(10, result.Size);
            Assert.Equal(2, result.Total_Records);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // CreateAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateAsync_SetsCreatedAtAndCreatedBy()
        {
            // Arrange
            var dto = new ProductDto { ProductName = "New Product" };
            int userId = 42;

            // Capture the product saved to the repo
            Product? capturedProduct = null;
            _repoMock
                .Setup(r => r.CreateAsync(It.IsAny<Product>()))
                .Callback<Product>(p => capturedProduct = p)
                .ReturnsAsync((Product p) => p);

            // Act
            await _sut.CreateAsync(dto, userId);

            // Assert
            Assert.NotNull(capturedProduct);
            Assert.Equal(userId, capturedProduct!.CreatedBy);
            Assert.NotNull(capturedProduct.CreatedAt);
        }

        [Fact]
        public async Task CreateAsync_MapsItemsWithTimestampsAndOwner()
        {
            // Arrange
            var dto = new ProductDto
            {
                ProductName = "Product With Items",
                Items = new List<ItemDto>
                {
                    new ItemDto { Quantity = 3, Price = 4.99m, ProductId = 0 }
                }
            };

            Product? capturedProduct = null;
            _repoMock
                .Setup(r => r.CreateAsync(It.IsAny<Product>()))
                .Callback<Product>(p => capturedProduct = p)
                .ReturnsAsync((Product p) => p);

            // Act
            await _sut.CreateAsync(dto, 7);

            // Assert
            Assert.Single(capturedProduct!.Items);
            Assert.Equal(7, capturedProduct.Items.First().CreatedBy);
            Assert.NotNull(capturedProduct.Items.First().CreatedAt);
        }

        [Fact]
        public async Task CreateAsync_ReturnsMappedDto_AfterSave()
        {
            // Arrange
            var dto = new ProductDto { ProductName = "Saved Product" };
            var saved = new Product { ProductId = 55, ProductName = "Saved Product" };

            _repoMock
                .Setup(r => r.CreateAsync(It.IsAny<Product>()))
                .ReturnsAsync(saved);

            // Act
            var result = await _sut.CreateAsync(dto, 1);

            // Assert
            Assert.Equal(55, result.ProductId);
            Assert.Equal("Saved Product", result.ProductName);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // UpdateAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ThrowsKeyNotFoundException_WhenProductDoesNotExist()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.UpdateAsync(99, new ProductDto { ProductName = "X" }, 1));
        }

        [Fact]
        public async Task UpdateAsync_SetsUpdatedAtAndUpdatedBy()
        {
            // Arrange
            var existing = new Product { ProductId = 1, ProductName = "Old" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

            // Act
            await _sut.UpdateAsync(1, new ProductDto { ProductName = "New" }, 99);

            // Assert
            Assert.Equal(99, existing.UpdatedBy);
            Assert.NotNull(existing.UpdatedAt);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsMappedDto_WithNewName()
        {
            // Arrange
            var existing = new Product { ProductId = 3, ProductName = "OldName" };
            _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>())).Returns(Task.CompletedTask);

            // Act
            var result = await _sut.UpdateAsync(3, new ProductDto { ProductName = "NewName" }, 1);

            // Assert
            Assert.Equal("NewName", result.ProductName);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // DeleteAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_ThrowsKeyNotFoundException_WhenProductDoesNotExist()
        {
            // Arrange
            _repoMock.Setup(r => r.ExistsAsync(50)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.DeleteAsync(50));
        }

        [Fact]
        public async Task DeleteAsync_CallsRepositoryDelete_WhenProductExists()
        {
            // Arrange
            _repoMock.Setup(r => r.ExistsAsync(5)).ReturnsAsync(true);
            _repoMock.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);

            // Act
            await _sut.DeleteAsync(5);

            // Assert — verify repo.DeleteAsync was called exactly once
            _repoMock.Verify(r => r.DeleteAsync(5), Times.Once);
        }
    }
}