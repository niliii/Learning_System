using LearningHub.Api.Dtos;
using LearningHub.Api.Models;
using LearningHub.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LearningHub.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _repo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository repo, IPasswordHasher<User> passwordHasher, IConfiguration config)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
            _config = config;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest dto)
        {
            var existing = await _repo.GetByEmailAsync(dto.Email);
            if (existing != null)
                throw new Exception("کاربری با این ایمیل موجود است.");

            var user = new User
            {
                Email = dto.Email,
                UserName = dto.UserName
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            await _repo.AddAsync(user);

            var token = GenerateToken(user);
            return new AuthResponse { Token = token, Expiration = DateTime.UtcNow.AddMinutes(GetExpiry()) };
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null) return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed) return null;

            var token = GenerateToken(user);
            return new AuthResponse { Token = token, Expiration = DateTime.UtcNow.AddMinutes(GetExpiry()) };
        }

        private int GetExpiry() =>
            int.TryParse(_config["JwtSettings:ExpirationMinutes"], out var m) ? m : 60;

        private string GenerateToken(User user)
        {
            var secret = _config["JwtSettings:Secret"]!;
            var issuer = _config["JwtSettings:Issuer"];
            var audience = _config["JwtSettings:Audience"];
            var expiryMinutes = GetExpiry();

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
