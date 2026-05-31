using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductManagementSystem.Controllers;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Services.Interface;
using Xunit;

namespace ProductManagementSystem.Tests.Controllers
{
    public class ProductControllerTests
    {
        // ── Shared test infrastructure ────────────────────────────────────────────
        private readonly Mock<IProductService> _serviceMock;
        private readonly Mock<IJwtService> _jwtMock;
        private readonly Mock<IValidator<ProductDto>> _validatorMock;
        private readonly Mock<ILogger<ProductController>> _loggerMock;
        private readonly ProductController _sut;

        public ProductControllerTests()
        {
            _serviceMock   = new Mock<IProductService>();
            _jwtMock       = new Mock<IJwtService>();
            _validatorMock = new Mock<IValidator<ProductDto>>();
            _loggerMock    = new Mock<ILogger<ProductController>>();

            _sut = new ProductController(
                _serviceMock.Object,
                _jwtMock.Object,
                _validatorMock.Object,
                _loggerMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GetById
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_Returns200_WhenProductExists()
        {
            // Arrange
            var dto = new ProductDto { ProductId = 1, ProductName = "Widget" };
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(dto);

            // Act
            var result = await _sut.GetById(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task GetById_Returns404_WhenProductNotFound()
        {
            // Arrange
            _serviceMock.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((ProductDto?)null);

            // Act
            var result = await _sut.GetById(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GetAll
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_Returns400_WhenOrderParamDtoIsNull()
        {
            // Act
            var result = await _sut.GetAll(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetAll_Returns200_WithPaginatedData()
        {
            // Arrange
            var param = new OrderParamDto { PageNumber = 1, PageSize = 5 };
            _serviceMock.Setup(s => s.GetAllAsync(param))
                        .ReturnsAsync(new { Page = 1, Size = 5, Total_Records = 0, Data = Array.Empty<ProductDto>() });

            // Act
            var result = await _sut.GetAll(param);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Create
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_Returns400_WhenDtoIsNull()
        {
            // Act
            var result = await _sut.Create(null!);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_Returns400_WhenValidationFails()
        {
            // Arrange
            var dto = new ProductDto { ProductName = "" };  // empty name should fail
            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("ProductName", "Product name is required.")
            };

            _validatorMock
                .Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new ValidationResult(failures));

            // Act
            var result = await _sut.Create(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Create_Returns201_WhenProductIsCreatedSuccessfully()
        {
            // Arrange
            var dto = new ProductDto { ProductName = "New Product" };
            var created = new ProductDto {ProductId = 10, ProductName = "New Product" };

            _validatorMock
                .Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new ValidationResult());   // no errors

            _jwtMock.Setup(j => j.GetCurrentUser()).Returns(1);

            _serviceMock
                .Setup(s => s.CreateAsync(dto, 1))
                .ReturnsAsync(created);

            // Act
            var result = await _sut.Create(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Update
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_Returns404_WhenProductDoesNotExist()
        {
            // Arrange
            var dto = new ProductDto { ProductName = "Updated" };

            _validatorMock
                .Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new ValidationResult());

            _jwtMock.Setup(j => j.GetCurrentUser()).Returns(1);

            _serviceMock
                .Setup(s => s.UpdateAsync(99, dto, 1))
                .ThrowsAsync(new KeyNotFoundException("Product not found."));

            // Act
            var result = await _sut.Update(99, dto);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Update_Returns200_WhenUpdateIsSuccessful()
        {
            // Arrange
            var dto = new ProductDto { ProductName = "Updated" };
            var updated = new ProductDto { ProductId = 3, ProductName = "Updated" };

            _validatorMock
                .Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new ValidationResult());

            _jwtMock.Setup(j => j.GetCurrentUser()).Returns(1);

            _serviceMock
                .Setup(s => s.UpdateAsync(3, dto, 1))
                .ReturnsAsync(updated);

            // Act
            var result = await _sut.Update(3, dto);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Delete
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Delete_Returns404_WhenProductDoesNotExist()
        {
            // Arrange
            _serviceMock
                .Setup(s => s.DeleteAsync(88))
                .ThrowsAsync(new KeyNotFoundException("Product not found."));

            // Act
            var result = await _sut.Delete(88);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Delete_Returns204_WhenDeleteIsSuccessful()
        {
            // Arrange
            _serviceMock.Setup(s => s.DeleteAsync(4)).Returns(Task.CompletedTask);

            // Act
            var result = await _sut.Delete(4);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
    }
}