using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Common;
using ProductManagementSystem.Context;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;

namespace ProductManagementSystem.Repositories.Data
{
    public class AuthRepository:IAuthRepository
    {
        private readonly AppDbContext _context;
        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SysUser> AuthenticateUser(string loginId)
        {
            return await _context.SysUser.AsNoTracking().FirstOrDefaultAsync(u => u.LoginId.ToLower() == loginId.ToLower());
        }
        public async Task<SysUser> CreateAsync(SysUser user)
        {
            await _context.SysUser.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }
        public async Task UpdateUserAsync(SysUser user)
        {
            _context.SysUser.Update(user);
            await _context.SaveChangesAsync();

        }
        public async Task DeleteUserAsync(int id)
        {
            var sysUser = await _context.SysUser.FindAsync(id);
            if (sysUser != null)
            {
                _context.SysUser.Remove(sysUser);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<(IEnumerable<SysUser> Users, int TotalCount)> GetAllUserAsync(OrderParamDto orderParamDto)
        {
            var query = _context.SysUser.AsNoTracking().AsQueryable();

            // Search functionality
            if (!string.IsNullOrEmpty(orderParamDto.SearchText))
            {
                var searchText = orderParamDto.SearchText.ToLower();
                query = query.Where(u => (u.Name.ToLower().Contains(searchText) ||
                                         u.LoginId.ToLower().Contains(searchText)
                                         ));
            }

            //Sorting
            if (!string.IsNullOrEmpty(orderParamDto.OrderColumn))
            {
                query = query.OrderByProperty(orderParamDto.OrderColumn, orderParamDto.Order);
            }

            // Total count
            var totalCount = await query.CountAsync();

            // Pagination
            var users = await query
                .Skip((orderParamDto.PageNumber-1) * orderParamDto.PageSize)
                .Take(orderParamDto.PageSize)
                .ToListAsync();
            return (users, totalCount);
        }
        public async Task<SysUser> GetByUserIdAsync(int userId)
        {
            // Added validation
            if (userId <= 0)
                return null;

            return await _context.SysUser
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
