using ProductManagementSystem.DTOs;

namespace ProductManagementSystem.Services.Interface
{
    public interface ITokenService
    {
        Task<TokenResponseDto> GenerateTokenForUserAsync(int userId, HttpContext httpContext);
        Task<TokenResponseDto> RefreshTokenAsync(string refreshToken, HttpContext httpContext);
        Task<bool> ValidateTokenAsync(string token);
        Task RevokeTokenAsync(string token);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task RevokeAllUserTokensAsync(int userId);
        Task<string> GetTokenIdFromTokenAsync(string token);
        Task CleanupExpiredTokensAsync();
    }
}
