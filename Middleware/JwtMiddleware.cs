using ProductManagementSystem.Services.Interface;

namespace ProductManagementSystem.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context, IServiceScopeFactory serviceScopeFactory)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                // Create a scoped instance of IJwtService
                using (var scope = serviceScopeFactory.CreateScope())
                {
                    var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

                    var userIdString = jwtService.GetUserIdFromToken(token).ToString();

                    if (int.TryParse(userIdString, out int userId))
                    {
                        context.Items["UserId"] = userId; // Store UserId as an int
                    }
                }
            }

            await _next(context);
        }
    }
    }
