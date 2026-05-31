using AutoMapper;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;
using ProductManagementSystem.Services.Interface;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace ProductManagementSystem.Services.Data
{
    public class AuthService : IAuthService
    {
        private readonly IMapper _mapper;
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        public AuthService(IAuthRepository authRepository, IConfiguration configuration, IMapper mapper, ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _mapper = mapper;
            _configuration = configuration;
            _logger=logger;
        }

        // GetUser ByID
        public async Task GetByIdAsync(int id)
        {
            await _authRepository.GetByUserIdAsync(id);
        }

        //GettAll User
        public async Task<object> GetAllAsync(OrderParamDto orderParamDto)
        {
            _logger.LogInformation("Admin fetching all users — page {Page}", 
                orderParamDto.PageNumber);
            try
            {
                var (users, totalCount) = await _authRepository.GetAllUserAsync(orderParamDto);
                var userList = _mapper.Map<IEnumerable<SysUserDto>>(users);
                return new
                {
                    Page = orderParamDto.PageNumber,
                    Size = orderParamDto.PageSize,
                    Total_Records = totalCount,
                    Data = userList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                throw new Exception("An error occurred while retrieving users.", ex);
            }
        }

        //Update User
        public async Task<SysUser?> UpdateAsync(SysUserDto sysUsersDto, int currentUserId)
        {
            _logger.LogInformation(
                "Updating user {UserId} by admin {AdminId}", sysUsersDto.UserId, currentUserId);
            var existingUser = await _authRepository.GetByUserIdAsync(sysUsersDto.UserId);
            if (existingUser == null)
            {
                _logger.LogWarning("Update failed — user {UserId} not found", sysUsersDto.UserId);
                throw new InvalidOperationException("User not found.");
            }
            var newPassword = sysUsersDto.Password;

            _mapper.Map(sysUsersDto, existingUser);
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var salt = GenerateSalt();
                existingUser.Salt = salt;
                existingUser.Password = HashPassword(newPassword, salt);
            }
            existingUser.UpdatedAt = DateTime.Now;
            existingUser.UpdatedBy = currentUserId;

            await _authRepository.UpdateUserAsync(existingUser);
            return existingUser;
        }

        // Delete User
        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Deleting user {UserId}", id);
            await _authRepository.DeleteUserAsync(id);
        }

       
        
        // Login Method
        public async Task<object> GenerateJwtToken(string loginId, string password)
        {
            _logger.LogInformation("Login attempt for '{LoginId}'", loginId);
            var user = await _authRepository.AuthenticateUser(loginId);
            if (user == null)
            {
                _logger.LogWarning("Login failed — user '{LoginId}' not found", loginId);
                return ("INVALID_CREDENTIALS");
            }

            var isValidCredentials = VerifyPassword(password, user.Password, user.Salt);
            if (!isValidCredentials)
            {
                _logger.LogWarning("Login failed — wrong password for '{LoginId}'", loginId);
                return "INVALID_CREDENTIALS";
            }

            return user;
        }
        
        
        public async Task<object> RegisterAsync(SysUserDto sysUserDto)
        {
            _logger.LogInformation("Registering new user '{LoginId}'", sysUserDto.LoginId);
            var existingUser = await _authRepository.AuthenticateUser(sysUserDto.LoginId);
            if (existingUser != null)
            {
                _logger.LogWarning(
                    "Registration failed — '{LoginId}' already exists", sysUserDto.LoginId);
                throw new InvalidOperationException("A user with the provided username already exists..");
            }

            // Generate salt and hash password
            var salt = GenerateSalt();
            var hashedPassword = HashPassword(sysUserDto.Password, salt);

            
            var newSysUser = _mapper.Map<SysUser>(sysUserDto);
            newSysUser.Password = hashedPassword;
            newSysUser.Salt = salt;
            newSysUser.IsActive = true;
            newSysUser.CreatedAt = DateTime.Now;
            newSysUser.Fcm = sysUserDto.Password; // use only password for seeing purpose

            // Save the user to the database
            var createUser = await _authRepository.CreateAsync(newSysUser);
            _logger.LogInformation("User '{LoginId}' registered successfully", createUser.LoginId);
            return new
            {
                loginid = createUser.LoginId,
                name = createUser.Name,
            };
        }

        public async Task<bool> ValidateTokenAsync(string token)
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

                // Validate the token (only checks if it is valid and not expired)
                handler.ValidateToken(token, validationParameters, out var validatedToken);

                // If we reach here, the token is valid
                return true;
            }
            catch (Exception)
            {
                // If token validation fails (expired, invalid signature, etc.), return false
                return false;
            }
        }

        // Helper methods
        private bool VerifyPassword(string enteredPassword, string storedHash, string salt)
        {
            var hash = HashPassword(enteredPassword, salt);
            return storedHash == hash;
        }
        private string GenerateSalt()
        {
            var saltBytes = new byte[16];
            string saltString = "";
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
                saltString = Convert.ToBase64String(saltBytes);
            }

            return saltString;
        }

        private string HashPassword(string password, string salt)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: Convert.FromBase64String(salt),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8)
                );
        }

    }
}
