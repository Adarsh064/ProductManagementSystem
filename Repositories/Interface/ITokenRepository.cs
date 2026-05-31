using ProductManagementSystem.Models;

namespace ProductManagementSystem.Repositories.Interface
{
    public interface ITokenRepository
    {
        Task<UserToken?> CreateTokenAsync(UserToken token);
        Task<UserToken?> GetByTokenIdAsync(string tokenId);
        Task<UserToken?> GetByRefreshTokenAsync(string refreshToken);
        Task<UserToken?> GetActiveTokenForUserAsync(int userId);
        Task RevokeTokenAsync(UserToken token);
        Task RevokeAllUserTokensAsync(int userId);
        Task RevokeExpiredTokensAsync();
        Task<bool> IsTokenActiveAsync(string tokenId);
        Task<bool> IsRefreshTokenActiveAsync(string refreshToken);
        Task UpdateTokenAsync(UserToken token);
    }
}
