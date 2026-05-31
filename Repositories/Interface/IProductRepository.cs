
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;

namespace ProductManagementSystem.Repositories.Interface
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int id);
        Task<(IEnumerable<Product> Products, int TotalCount)> GetAllAsync(OrderParamDto orderParamDto);
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
