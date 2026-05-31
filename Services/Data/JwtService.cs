using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ProductManagementSystem.Services.Data
{
    public class JwtService : IJwtService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        public JwtService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }
        public int GetUserIdFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("Token is missing");

            var handler = new JwtSecurityTokenHandler();
            try
            {
                // Define the validation parameters
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:key"]))
                };

                // Validate the token and retrieve the claims
                var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

                // Check if the token is indeed a JWT token
                if (!(validatedToken is JwtSecurityToken jwtToken))
                    throw new UnauthorizedAccessException("Invalid token");

                var userId = Convert.ToInt32(jwtToken?.Claims?.FirstOrDefault(c => c.Type == "userId")?.Value ?? "0");
                if (userId <= 0)
                    throw new UnauthorizedAccessException("Invalid token");
                return userId;
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Invalid or expired token");
            }
        }

        public int GetCurrentUser()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null || !httpContext.Items.ContainsKey("UserId"))
            {
                throw new UnauthorizedAccessException("Access denied. Invalid or missing token.");
            }

            return (int)httpContext.Items["UserId"];
        }
    }
}
