using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DocumentSystemApi.Data;
using DocumentSystemApi.Dtos;
using DocumentSystemApi.Models;

namespace DocumentSystemApi.Services
{
    public class SystemAuthenticationService
    {
        private readonly DocumentDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public SystemAuthenticationService(
            DocumentDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        //Register
        public async Task<User?> RegisterAsync(RegisterDto dto)
        {
            string email = dto.Email.Trim().ToLower();
            string fullName = dto.FullName.Trim();

            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Full name is required.");
            }

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                return null;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                dto.Password
            );

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return user;
        }

        //Login
        public async Task<User?> LoginAsync(LoginDto dto)
        {
            string email = dto.Email.Trim().ToLowerInvariant();

            // Find the user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            // Check if user exists and is active
            if (user == null || !user.IsActive)
            {
                return null;
            }

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                dto.Password
            );

            // Reject incorrect passwords
            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            // Return user
            return user;
        }
    }
}