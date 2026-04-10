using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlueSchoolSystem.Tests;

/// <summary>
/// Tiện ích tạo JWT token cho integration tests mà không phụ thuộc vào IntegrationTestBase.
/// </summary>
public static class TestTokenHelper
{
    private const string SecretKey = "4vmySLK3dpVl100gVdZsqOrurvmVvyqCnrrXVpeW";
    private const string Issuer    = "blueschool";
    private const string Audience  = "blueschool_users";

    /// <summary>
    /// Tạo JWT token hợp lệ với role và username cho testing.
    /// </summary>
    public static string GenerateToken(string role, string userName = "test_user")
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim("username",      userName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Tạo JWT token đã hết hạn (dùng để test 401 Unauthorized).
    /// </summary>
    public static string GenerateExpiredToken(string role, string userName = "test_user")
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             Issuer,
            audience:           Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow.AddHours(-2),
            expires:            DateTime.UtcNow.AddHours(-1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
