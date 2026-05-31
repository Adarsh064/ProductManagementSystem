using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;
using ProductManagementSystem.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProductManagementSystem.Services.Data
{
    public class TokenService : ITokenService
    {
        private readonly ITokenRepository _tokenRepository;
        private readonly IConfiguration _configuration;
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<TokenService> _logger;
        private readonly IJwtService _jwtService;
        public TokenService(
           ITokenRepository tokenRepository,
           IConfiguration configuration,
           IJwtService jwtService,
           IAuthRepository authRepository,
           ILogger<TokenService> logger)
        {
            _tokenRepository = tokenRepository;
            _configuration = configuration;
            _jwtService = jwtService;
            _authRepository=authRepository;
            _logger=logger;
        }
        public async Task<TokenResponseDto> GenerateTokenForUserAsync(int userId, HttpContext httpContext)
        {
            // First revoke any existing active tokens for this user
            // This enforces the single device login policy
            await _tokenRepository.RevokeAllUserTokensAsync(userId);
            var user=await _authRepository.GetByUserIdAsync(userId);
            if (user == null) {
                _logger.LogError(
                    "GenerateTokenForUserAsync: user {UserId} not found in database", userId);
                throw new InvalidOperationException($"User {userId} was not found.");
            }
            // Generate a new JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:key"]);

            // Add a unique jti (JWT ID) claim to identify this token
            var tokenId = Guid.NewGuid().ToString();

            // Set token expiration time (15 minutes)
            var accessTokenExpiration = DateTime.Now.AddMinutes(10);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.LoginId),                
                new Claim(ClaimTypes.Role, user.UserType ?? "User"),

                new Claim("userId",   user.UserId.ToString()),
                new Claim("userType", user.UserType ?? "User"),
                new Claim("name",     user.Name ?? string.Empty),
                new Claim("jti", tokenId)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = accessTokenExpiration,
                Issuer   = _configuration["Jwt:Issuer"],              // [FIXED]
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Generate refresh token
            var refreshToken = GenerateRefreshToken();
            var refreshTokenId = Guid.NewGuid().ToString();

            // Set refresh token expiration (7 days)
            var refreshTokenExpiration = DateTime.Now.AddMinutes(15);

            // Extract device info and IP address
            var deviceInfo = httpContext.Request.Headers["User-Agent"].ToString();
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            // Store the token in the database
            var userToken = new UserToken
            {
                UserId = userId,
                TokenId = tokenId, // Use the jti as the token identifier
                RefreshTokenId = refreshTokenId,
                RefreshToken = refreshToken,
                CreatedAt = DateTime.Now,
                ExpiresAt = accessTokenExpiration,
                RefreshTokenExpiresAt = refreshTokenExpiration,
                DeviceInfo = deviceInfo,
                IpAddress = ipAddress,
            };
            await _tokenRepository.CreateTokenAsync(userToken);
            _logger.LogInformation(                                    // [ADDED]
                 "Token issued — userId={UserId} role={Role} jti={Jti}",
                 userId, user.UserType, tokenId);
            return new TokenResponseDto
            {
                AccessToken = tokenString,
                RefreshToken = refreshToken,
                ExpiresIn = (int)(accessTokenExpiration - DateTime.Now).TotalSeconds
            };
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken, HttpContext httpContext)
        {
            // Validate the refresh token
            var userToken = await _tokenRepository.GetByRefreshTokenAsync(refreshToken);

            if (userToken == null || !userToken.IsRefreshTokenActive || userToken.RevokedAt != null)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            // Revoke the current token
            await _tokenRepository.RevokeTokenAsync(userToken);

            // Generate new tokens
            return await GenerateTokenForUserAsync(userToken.UserId, httpContext);
        }

       

        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                // Verify the token signature and expiration
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:key"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                // Extract the token ID (jti claim)
                var tokenId = jwtToken.Claims.First(x => x.Type == "jti").Value;

                // Check if the token is active in the database
                return await _tokenRepository.IsTokenActiveAsync(tokenId);
            }
            catch
            {
                return false;
            }
        }
        public async Task RevokeTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            var tokenId = await GetTokenIdFromTokenAsync(token);
            var userToken = await _tokenRepository.GetByTokenIdAsync(tokenId);

            if (userToken != null)
            {
                await _tokenRepository.RevokeTokenAsync(userToken);
            }
        }
        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new ArgumentNullException(nameof(refreshToken));

            var userToken = await _tokenRepository.GetByRefreshTokenAsync(refreshToken);
            if (userToken != null)
            {
                await _tokenRepository.RevokeTokenAsync(userToken);
            }
        }
        public async Task RevokeAllUserTokensAsync(int userId)
        {
            await _tokenRepository.RevokeAllUserTokensAsync(userId);
        }
        public async Task<string> GetTokenIdFromTokenAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.First(claim => claim.Type == "jti").Value;
        }

        public async Task CleanupExpiredTokensAsync()
        {
            await _tokenRepository.RevokeExpiredTokensAsync();
        }


        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
