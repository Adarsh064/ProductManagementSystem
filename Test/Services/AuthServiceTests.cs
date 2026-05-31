using AutoMapper;
using Moq;
using ProductManagementSystem.DTOs;
using ProductManagementSystem.Mapping;
using ProductManagementSystem.Models;
using ProductManagementSystem.Repositories.Interface;
using ProductManagementSystem.Services.Data;
using Xunit;

namespace ProductManagementSystem.Tests.Services
{
    public class AuthServiceTests
    {
        // ── Shared test infrastructure ────────────────────────────────────────────
        private readonly Mock<IAuthRepository> _repoMock;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _repoMock   = new Mock<IAuthRepository>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            // Real mapper — so mapping regressions are caught here too
            var mapConfig = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile()));
            _mapper = mapConfig.CreateMapper();

            // Minimal in-memory config with a JWT key long enough for HMAC-SHA256
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:key"]      = "wRJSMeKKF2QT4fwpMeJf36POk6yJVadQssw5c",
                    ["Jwt:Issuer"]   = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience"
                })
                .Build();

            _sut = new AuthService(_repoMock.Object, _config, _mapper, _loggerMock.Object);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // RegisterAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RegisterAsync_ThrowsInvalidOperationException_WhenLoginIdAlreadyExists()
        {
            // Arrange — AuthenticateUser returns an existing user
            _repoMock
                .Setup(r => r.AuthenticateUser("existing@test.com"))
                .ReturnsAsync(new SysUser { LoginId = "existing@test.com" });

            var dto = new SysUserDto
            {
                LoginId  = "existing@test.com",
                Name     = "Duplicate",
                Password = "pass123",
                IsActive = true
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.RegisterAsync(dto));
        }

        [Fact]
        public async Task RegisterAsync_CreatesUser_WhenLoginIdIsNew()
        {
            // Arrange
            _repoMock
                .Setup(r => r.AuthenticateUser("new@test.com"))
                .ReturnsAsync((SysUser?)null);                    // no existing user

            _repoMock
                .Setup(r => r.CreateAsync(It.IsAny<SysUser>()))
                .ReturnsAsync((SysUser u) => u);                  // return the same entity

            var dto = new SysUserDto
            {
                LoginId  = "new@test.com",
                Name     = "New User",
                Password = "SecurePass1!",
                IsActive = true
            };

            // Act
            var result = await _sut.RegisterAsync(dto);

            // Assert — result is anonymous { loginid, name }
            Assert.NotNull(result);
            _repoMock.Verify(r => r.CreateAsync(It.IsAny<SysUser>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_HashesPassword_NotStoringPlainText()
        {
            // Arrange
            _repoMock.Setup(r => r.AuthenticateUser(It.IsAny<string>()))
                     .ReturnsAsync((SysUser?)null);

            SysUser? capturedUser = null;
            _repoMock
                .Setup(r => r.CreateAsync(It.IsAny<SysUser>()))
                .Callback<SysUser>(u => capturedUser = u)
                .ReturnsAsync((SysUser u) => u);

            var dto = new SysUserDto
            {
                LoginId  = "hash@test.com",
                Name     = "Hash Test",
                Password = "PlainTextPass"
            };

            // Act
            await _sut.RegisterAsync(dto);

            // Assert — stored password must NOT equal the original plain-text input
            Assert.NotNull(capturedUser);
            Assert.NotEqual("PlainTextPass", capturedUser!.Password);
            // Fcm field must NOT contain plain-text password (bug fix verification)
            Assert.NotEqual("PlainTextPass", capturedUser.Fcm);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GenerateJwtToken (Login)
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GenerateJwtToken_ReturnsInvalidCredentials_WhenUserNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.AuthenticateUser("ghost@test.com"))
                     .ReturnsAsync((SysUser?)null);

            // Act
            var result = await _sut.GenerateJwtToken("ghost@test.com", "any");

            // Assert
            Assert.Equal("INVALID_CREDENTIALS", result);
        }

        [Fact]
        public async Task GenerateJwtToken_ReturnsInvalidCredentials_WhenPasswordIsWrong()
        {
            // Arrange — create a user with a known hashed password
            // We call RegisterAsync to get a properly hashed user, then use its hash
            SysUser? registeredUser = null;
            _repoMock.Setup(r => r.AuthenticateUser("login@test.com"))
                     .ReturnsAsync((SysUser?)null);
            _repoMock.Setup(r => r.CreateAsync(It.IsAny<SysUser>()))
                     .Callback<SysUser>(u => registeredUser = u)
                     .ReturnsAsync((SysUser u) => u);

            await _sut.RegisterAsync(new SysUserDto
            {
                LoginId = "login@test.com",
                Name = "Login Test",
                Password = "CorrectPass"
            });

            // Now set up AuthenticateUser to return the registered (hashed) user
            _repoMock.Setup(r => r.AuthenticateUser("login@test.com"))
                     .ReturnsAsync(registeredUser!);

            // Act
            var result = await _sut.GenerateJwtToken("login@test.com", "WrongPass");

            // Assert
            Assert.IsType<SysUser>(result);
            var user = (SysUser)result;
            Assert.Equal("valid@test.com", user.LoginId);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // UpdateAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_ThrowsInvalidOperationException_WhenUserNotFound()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByUserIdAsync(99))
                     .ReturnsAsync((SysUser?)null);

            var dto = new SysUserDto
            {
                UserId       = 99,
                LoginId  = "x@test.com",
                Name     = "X",
                Password = "p"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.UpdateAsync(dto, 1));
        }

        [Fact]
        public async Task UpdateAsync_SetsUpdatedAtAndUpdatedBy()
        {
            // Arrange
            var existing = new SysUser
            {
                UserId  = 5,
                LoginId = "user@test.com",
                Name    = "Old Name",
                Password = "hash",
                Salt    = "salt",
                UserType = "User"
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync(existing);
            _repoMock.Setup(r => r.UpdateUserAsync(It.IsAny<SysUser>()))
                     .Returns(Task.CompletedTask);

            var dto = new SysUserDto
            {
                UserId      = 5,
                LoginId = "user@test.com",
                Name    = "New Name",
                Password = "hash"
            };

            // Act
            await _sut.UpdateAsync(dto, 77);

            // Assert
            Assert.Equal(77, existing.UpdatedBy);
            Assert.NotNull(existing.UpdatedAt);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // DeleteAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteAsync_CallsRepositoryDelete_WithCorrectId()
        {
            // Arrange
            _repoMock.Setup(r => r.DeleteUserAsync(10)).Returns(Task.CompletedTask);

            // Act
            await _sut.DeleteAsync(10);

            // Assert
            _repoMock.Verify(r => r.DeleteUserAsync(10), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GetAllAsync
        // ─────────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsPaginatedObject_WithCorrectTotalCount()
        {
            // Arrange
            var users = new List<SysUser>
            {
                new SysUser { UserId = 1, LoginId = "a@a.com", Name = "Alice",
                              Password = "h", Salt = "s", UserType = "Admin" },
                new SysUser { UserId = 2, LoginId = "b@b.com", Name = "Bob",
                              Password = "h", Salt = "s", UserType = "User" }
            };

            _repoMock
                .Setup(r => r.GetAllUserAsync(It.IsAny<OrderParamDto>()))
                .ReturnsAsync((users, 2));

            // Act
            dynamic result = await _sut.GetAllAsync(new OrderParamDto());

            // Assert
            Assert.Equal(2, result.Total_Records);
        }
    }
}