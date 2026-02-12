using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PCBuilder.Data;
using PCBuilder.Models;

namespace PCBuilder.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public bool UserExists(string username)
        {
            return _context.Users.Any(u => u.Username == username);
        }

        public void Register(string username, string password)
        {
            var user = new User
            {
                Username = username,
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public User? ValidateUser(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return null;
            }

            var resultOfHashing = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password
                );

            return resultOfHashing == PasswordVerificationResult.Success ? user : null;
        }

    }
}
