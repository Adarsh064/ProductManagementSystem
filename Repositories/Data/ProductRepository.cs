using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Common;
using ProductManagementSystem.Context;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;

namespace ProductManagementSystem.Repositories.Data
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        public ProductRepository(AppDbContext context) => _context = context;
        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Product.AsNoTracking().Include(p => p.Items).FirstOrDefaultAsync(p => p.ProductId == id);
        }
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetAllAsync(OrderParamDto orderParamDto)
        {
            var query = _context.Product.AsNoTracking().Include(p => p.Items).AsQueryable();
            // Search functionality
            if (!string.IsNullOrEmpty(orderParamDto.SearchText))
            {
                var searchText = orderParamDto.SearchText.ToLower();
                query = query.Where(u => u.ProductName.ToLower().Contains(searchText));
            }
            //Sorting
            if (!string.IsNullOrEmpty(orderParamDto.OrderColumn))
            {
                query = query.OrderByProperty(orderParamDto.OrderColumn, orderParamDto.Order);
            }
            // Total count
            var totalCount = await query.CountAsync();
            // Pagination
            var products = await query
                .Skip((orderParamDto.PageNumber-1) * orderParamDto.PageSize)
                .Take(orderParamDto.PageSize)
                .ToListAsync();
            return (products, totalCount);
        }
        public async Task<Product> CreateAsync(Product product)
        {
            await _context.Product.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }
        public async Task UpdateAsync(Product product)
        {
            _context.Product.Update(product);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var product = await _context.Product.Include(p => p.Items).FirstOrDefaultAsync(p => p.ProductId==id);
            if (product != null)
            {
                _context.Item.RemoveRange(product.Items);
                _context.Product.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<bool> ExistsAsync(int id) => await _context.Product.AsNoTracking().AnyAsync(p => p.ProductId == id);
    }
}
