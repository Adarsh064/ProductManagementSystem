using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.Common;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;
using ProductManagementSystem.Services.Interface;
using System.Net;

namespace ProductManagementSystem.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        private readonly IValidator<SysUserDto> _sysUserValidator;
        private readonly IValidator<LoginDto> _loginDtoValidator;
        private readonly ILogger<AuthController> _logger;
        private readonly IJwtService _jwtService;
        public AuthController(IAuthService authService, IValidator<SysUserDto> sysUserValidator,
            IValidator<LoginDto> loginDtoValidator, IJwtService jwtService, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _sysUserValidator = sysUserValidator;
            _loginDtoValidator = loginDtoValidator;
            _authService = authService;
            _jwtService = jwtService;
            _tokenService=tokenService;
            _logger=logger;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] SysUserDto sysUserDto)
        {if (sysUserDto == null)
            {
                return this.ApiResponse<object>("Invalid user data", HttpStatusCode.BadRequest);
            }
                var validationResult = await _sysUserValidator.ValidateAsync(sysUserDto);
                if (!validationResult.IsValid)
                {
                return this.ValidationError(validationResult.Errors);
            }

            var registerUser = await _authService.RegisterAsync(sysUserDto);
            return this.ApiResponse<object>("Registration successful.", HttpStatusCode.OK, registerUser);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Validate the DTO
            var validationResult = await _loginDtoValidator.ValidateAsync(loginDto);
            if (!validationResult.IsValid)
            {
                return this.ValidationError(validationResult.Errors);
            }

            var authResult = await _authService.GenerateJwtToken(loginDto.UserName, loginDto.Password);

            if (authResult!=null && authResult is string strResult && strResult.Equals("INVALID_CREDENTIALS", StringComparison.OrdinalIgnoreCase))
            {
                return this.ApiResponse<object>("Invalid username or password.", HttpStatusCode.Unauthorized);
            }
            var user = (SysUser)authResult;
            var tokenResponse = await _tokenService.GenerateTokenForUserAsync(
                 user.UserId, HttpContext);

            _logger.LogInformation(
                "User {UserId} ({Role}) logged in successfully",
                user.UserId, user.UserType);

            return this.ApiResponse("Login successful.", HttpStatusCode.OK, new
            {
                token = tokenResponse.AccessToken,
                refreshToken = tokenResponse.RefreshToken,
                expiresIn = tokenResponse.ExpiresIn
            });



        }

        [Authorize(Roles = "Admin")]
        [HttpPost("getAll")]
        public async Task<ActionResult> GetAllUsers([FromBody] OrderParamDto orderParamDto)
        {
            if (orderParamDto == null)
            {
                return this.ApiResponse<object>("Pagination Parameters are required.", HttpStatusCode.BadRequest);
            }
            var sysUser = await _authService.GetAllAsync(orderParamDto);
                return this.ApiResponse("Users Fetched Successfully.", HttpStatusCode.OK,sysUser);
            
           
        }

        [Authorize(Roles = "Admin")]
        // Update a user
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromBody] SysUserDto sysUserDto)
        {
            if (sysUserDto.UserId != id)
            {
                return this.ApiResponse<object>("User ID in the body does not match the ID in the URL.", HttpStatusCode.BadRequest);
            }

           
                var userId = _jwtService.GetCurrentUser();
                var result = await _authService.UpdateAsync(sysUserDto, userId);
                return this.ApiResponse("User Updated Successfully.", HttpStatusCode.OK,result);
            
           
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = _jwtService.GetCurrentUser();
            await _authService.DeleteAsync(id);
            return this.ApiResponse<object>("User Deleted Successfully.", HttpStatusCode.OK);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDto logoutDto = null)
        {
            try
            {
                // Extract the token from the Authorization header
                var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                if (string.IsNullOrEmpty(token))
                {
                    return this.ApiResponse<object>("No authentication token provided.", HttpStatusCode.BadRequest);
                }

                // Revoke the token
                await _tokenService.RevokeTokenAsync(token);

                // If refresh token is provided, revoke it too
                if (logoutDto != null && !string.IsNullOrEmpty(logoutDto.RefreshToken))
                {
                    await _tokenService.RevokeRefreshTokenAsync(logoutDto.RefreshToken);
                }
                _logger.LogInformation("Token revoked on logout");
                return this.ApiResponse<object>("Logout successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return this.ApiResponse<object>($"Error during logout: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (string.IsNullOrEmpty(refreshTokenDto.RefreshToken))
            {
                return this.ApiResponse<object>("Refresh token is required.", HttpStatusCode.BadRequest);
            }

            try
            {
                var tokenResponse = await _tokenService.RefreshTokenAsync(refreshTokenDto.RefreshToken, HttpContext);
                return this.ApiResponse("Token refreshed successfully.", HttpStatusCode.OK, new
                {
                    token = tokenResponse.AccessToken,
                    refreshToken = tokenResponse.RefreshToken,
                    expiresIn = tokenResponse.ExpiresIn
                });
            }
            catch (SecurityTokenException ex)
            {
                return this.ApiResponse<object>("Invalid refresh token.", HttpStatusCode.Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token"); // [ADDED]
                return this.ApiResponse<object>($"Error refreshing token: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
    }
}
