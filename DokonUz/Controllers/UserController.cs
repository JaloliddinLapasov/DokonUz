using DokonUz.Data;
using DokonUz.Helpers;
using DokonUz.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging; // Add this for logging
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DokonUz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DokonUzDbContext _context;
        private readonly AuthSettings _authSettings;
        private readonly ILogger<UserController> _logger; // Declare ILogger

        public UserController(DokonUzDbContext context, IOptions<AuthSettings> authSettings, ILogger<UserController> logger)
        {
            _context = context;
            _authSettings = authSettings.Value;
            _logger = logger; // Initialize ILogger
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            _logger.LogInformation("Register attempt for username: {Username}", user.Username);

            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                _logger.LogWarning("Username already exists: {Username}", user.Username);
                return BadRequest("Username already exists.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.Role = string.IsNullOrEmpty(user.Role) ? "Customer" : user.Role; // Default to Customer

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Username}", user.Username);
            return Ok("User registered successfully.");
        }

        // POST: api/User/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(User user)
        {
            _logger.LogInformation("Login attempt for username: {Username}", user.Username);

            var dbUser = await _context.Users.SingleOrDefaultAsync(u => u.Username == user.Username);

            if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.PasswordHash, dbUser.PasswordHash))
            {
                _logger.LogWarning("Invalid login attempt for username: {Username}", user.Username);
                return Unauthorized("Invalid username or password.");
            }

            var token = GenerateJwtToken(dbUser);
            _logger.LogInformation("User logged in successfully: {Username}", user.Username);

            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_authSettings.Secret!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] 
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role!) // Include role in token
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
