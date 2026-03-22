using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WorkspaceStressSystem.Api.Models.Entities;

namespace WorkspaceStressSystem.Api.Helpers;

public class JwtHelper
{
    private readonly IConfiguration _configuration;

    public JwtHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public int AccessTokenMinutes
    {
        get
        {
            var raw = _configuration["Jwt:AccessTokenMinutes"];
            return int.TryParse(raw, out var value) ? value : 15;
        }
    }

    public int RefreshTokenDays
    {
        get
        {
            var raw = _configuration["Jwt:RefreshTokenDays"];
            return int.TryParse(raw, out var value) ? value : 7;
        }
    }

    public string GenerateAccessToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key");
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}