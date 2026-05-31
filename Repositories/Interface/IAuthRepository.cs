using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;

namespace ProductManagementSystem.Repositories.Interface
{
    public interface IAuthRepository
    {
        Task<SysUser> AuthenticateUser(string loginId);
        Task<SysUser> CreateAsync(SysUser user);
        Task UpdateUserAsync(SysUser user);
        Task DeleteUserAsync(int id);
        Task<(IEnumerable<SysUser> Users, int TotalCount)> GetAllUserAsync(OrderParamDto orderParamDto);
        Task<SysUser> GetByUserIdAsync(int userId);
        
    }
}
