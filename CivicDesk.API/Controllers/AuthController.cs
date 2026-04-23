using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CivicDesk.API.Data;
using CivicDesk.API.DTOs;
using CivicDesk.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CivicDesk.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        var admin = _db.AdminUsers.FirstOrDefault(a => a.Username == dto.Username);
        if (admin is null || !BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash))
            return Unauthorized("Invalid credentials.");

        var expiresInHours = double.Parse(_config["Jwt:ExpiresInHours"] ?? "24");
        var expiresAt = DateTime.UtcNow.AddHours(expiresInHours);
        var token = GenerateToken(admin, expiresAt);

        return Ok(new AuthTokenDto(token, expiresAt));
    }

    private string GenerateToken(AdminUser admin, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
