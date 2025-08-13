using Shared.Common.Security;

namespace Auth.Api.Infrastructure.Users
{
    public interface IUserStore
    {
        Task<IUser?> ValidateAsync(string username, string password, CancellationToken ct = default);
    }

    internal sealed class InMemoryUserStore : IUserStore
    {
        private readonly List<UserRecord> _users = new();
        private readonly IPasswordHasher _passwordHasher;

        public InMemoryUserStore(IPasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;

            // Seed demo user
            _users.Add(new UserRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Username = "admin@developerstore.dev",
                Role = "Admin",
                PasswordHash = _passwordHasher.HashPassword("Admin@123") // demo password
            });
        }

        public Task<IUser?> ValidateAsync(string username, string password, CancellationToken ct = default)
        {
            var user = _users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user is null)
                return Task.FromResult<IUser?>(null);

            var ok = _passwordHasher.VerifyPassword(password, user.PasswordHash);
            if (!ok)
                return Task.FromResult<IUser?>(null);

            return Task.FromResult<IUser?>(new AuthUser(user.Id, user.Username, user.Role));
        }

        private sealed class UserRecord
        {
            public string Id { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = "User";
            public string PasswordHash { get; set; } = string.Empty;
        }

        private sealed class AuthUser(string id, string username, string role) : IUser
        {
            public string Id { get; } = id;
            public string Username { get; } = username;
            public string Role { get; } = role;
        }
    }
}

namespace Auth.Api.Infrastructure.Security
{
    public sealed class BCryptPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}

namespace Auth.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Shared.Common.Api;
    using Shared.Common.Security;
    using Auth.Api.Infrastructure.Users;

    [ApiController]
    [Route("auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserStore _userStore;
        private readonly IJwtTokenGenerator _tokenGenerator;

        public AuthController(IUserStore userStore, IJwtTokenGenerator tokenGenerator)
        {
            _userStore = userStore;
            _tokenGenerator = tokenGenerator;
        }

        public sealed class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public sealed class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponseWithData<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Unauthorized(new ApiResponse { Success = false, Message = "Invalid credentials" });
            }

            var user = await _userStore.ValidateAsync(request.Username, request.Password, ct);
            if (user is null)
            {
                return Unauthorized(new ApiResponse { Success = false, Message = "Invalid credentials" });
            }

            var token = _tokenGenerator.GenerateToken(user);
            return Ok(new ApiResponseWithData<LoginResponse>
            {
                Success = true,
                Message = "Authenticated",
                Data = new LoginResponse { Token = token }
            });
        }
    }
}