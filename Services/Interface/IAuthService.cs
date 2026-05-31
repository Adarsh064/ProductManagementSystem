using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;

namespace ProductManagementSystem.Services.Interface
{
    public interface IAuthService
    {
        Task<object> RegisterAsync(SysUserDto sysUserDto);
        Task<object> GenerateJwtToken(string loginId, string password);
        Task<SysUser?> UpdateAsync(SysUserDto sysUsersDto, int userId);
        Task DeleteAsync(int id);
        Task<object> GetAllAsync(OrderParamDto orderParamDto);
        Task GetByIdAsync(int id);
       
        Task<bool> ValidateTokenAsync(string token);
    }
}
