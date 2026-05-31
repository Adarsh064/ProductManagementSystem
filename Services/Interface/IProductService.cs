using ProductManagementSystem.DTOs;

namespace ProductManagementSystem.Services.Interface
{
    public interface IProductService
    {
        Task<ProductDto?> GetByIdAsync(int id);
        Task<object> GetAllAsync(OrderParamDto orderParamDto);
        Task<ProductDto> CreateAsync(ProductDto productDto, int currentUserId);
        Task<ProductDto> UpdateAsync(int id, ProductDto productDto, int currentUserId);
        Task DeleteAsync(int id);
    }
}
