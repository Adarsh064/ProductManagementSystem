using Microsoft.EntityFrameworkCore;
using ProductManagementSystem.Context;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;

namespace ProductManagementSystem.Repositories.Data
{
    public class TokenRepository : ITokenRepository
    {
        private readonly AppDbContext _dbContext;
        public TokenRepository(AppDbContext dbcontext)
        {
            _dbContext = dbcontext;
        }
        public async Task<UserToken?> CreateTokenAsync(UserToken token)
        {
            await _dbContext.UserToken.AddAsync(token);
            await _dbContext.SaveChangesAsync();
            return token;
        }
        public async Task<UserToken?> GetByTokenIdAsync(string tokenId)
        {
            return await _dbContext.UserToken.AsNoTracking().FirstOrDefaultAsync(t => t.TokenId == tokenId);
        }
        public async Task<UserToken?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _dbContext.UserToken.AsNoTracking().FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
        }

        public async Task<UserToken?> GetActiveTokenForUserAsync(int userId)
        {
            return await _dbContext.UserToken.AsNoTracking()
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.Now)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }
        public async Task RevokeTokenAsync(UserToken token)
        {
            var existing = await _dbContext.UserToken
                .FirstOrDefaultAsync(t => t.Id == token.Id);

            if (existing == null) return;

            existing.RevokedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();
        }
        public async Task RevokeAllUserTokensAsync(int userId)
        {
            var tokens = await _dbContext.UserToken
                .Where(t => t.UserId == userId && t.RevokedAt == null)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();
        }
        public async Task RevokeExpiredTokensAsync()
        {
            var expiredTokens = await _dbContext.UserToken
                .Where(t => t.RevokedAt == null && t.ExpiresAt < DateTime.Now && t.RefreshTokenExpiresAt < DateTime.Now)
                .ToListAsync();

            foreach (var token in expiredTokens)
            {
                token.RevokedAt = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();
        }
        public async Task<bool> IsTokenActiveAsync(string tokenId)
        {
            var token = await _dbContext.UserToken
                .FirstOrDefaultAsync(t => t.TokenId == tokenId);

            return token != null && token.IsActive;
        }
        public async Task<bool> IsRefreshTokenActiveAsync(string refreshToken)
        {
            var token = await _dbContext.UserToken.FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
            return token != null && token.IsRefreshTokenActive;
        }
        public async Task UpdateTokenAsync(UserToken token)
        {
            _dbContext.UserToken.Update(token);
            await _dbContext.SaveChangesAsync();
        }
    }
}
