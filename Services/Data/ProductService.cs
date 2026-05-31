using AutoMapper;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;
using ProductManagementSystem.Services.Interface;

namespace ProductManagementSystem.Services.Data
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductService> _logger;
        public ProductService(IProductRepository productRepository, IMapper mapper, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _mapper = mapper;
            _logger = logger;
        }
        public async Task<ProductDto?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Fetching product with ID {ProductId}", id);
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} was not found", id);
                return null;
            }
            return _mapper.Map<ProductDto>(product);
        }
        public async Task<object> GetAllAsync(OrderParamDto orderParamDto)
        {
            _logger.LogInformation(
                "Fetching products — page {Page}, size {Size}, search '{Search}'",
                orderParamDto.PageNumber,
                orderParamDto.PageSize,
                orderParamDto.SearchText);
            var (products, totalCount) = await _productRepository.GetAllAsync(orderParamDto);
            var productList = _mapper.Map<IEnumerable<ProductDto>>(products);
            return new
            {
                Page = orderParamDto.PageNumber,
                Size = orderParamDto.PageSize,
                Total_Records = totalCount,
                Data = productList
            };
        }
        public async Task<ProductDto> CreateAsync(ProductDto productDto, int currentUserId)
        {
            _logger.LogInformation(
                "Creating product '{ProductName}' by user {UserId}",
                productDto.ProductName,
                currentUserId);
            var product = _mapper.Map<Product>(productDto);
            product.CreatedAt = DateTime.Now;
            product.CreatedBy = currentUserId;

            if (productDto.Items.Any()==true)
            {
                product.Items=productDto.Items.Select(i =>
                {
                    var item = _mapper.Map<Item>(i);
                    item.CreatedAt = DateTime.Now;
                    item.CreatedBy = currentUserId;
                    return item;
                }).ToList();
            }
            var createdProduct = await _productRepository.CreateAsync(product);
            _logger.LogInformation(
               "Product created with ID {ProductId}", createdProduct.ProductId);

            return _mapper.Map<ProductDto>(createdProduct);
        }
        public async Task<ProductDto> UpdateAsync(int id, ProductDto productDto, int currentUserId)
        {
            _logger.LogInformation(
                "Updating product {ProductId} by user {UserId}", id, currentUserId);
            var existing = await _productRepository.GetByIdAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Update failed — product {ProductId} not found", id);
                throw new KeyNotFoundException($"Product with ID {id} was not found.");
            }

            // Map only mutable fields onto the tracked entity
            _mapper.Map(productDto, existing);
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = currentUserId;

            await _productRepository.UpdateAsync(existing);
            _logger.LogInformation("Product {ProductId} updated successfully", id);
            return _mapper.Map<ProductDto>(existing);
        }
        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting product {ProductId}", id);
            var exists = await _productRepository.ExistsAsync(id);
            if (!exists)
            {
                _logger.LogWarning("Delete failed — product {ProductId} not found", id);
                throw new KeyNotFoundException($"Product with ID {id} was not found.");
            }

            await _productRepository.DeleteAsync(id);
            _logger.LogInformation("Product {ProductId} deleted successfully", id);
        }
    }
}
