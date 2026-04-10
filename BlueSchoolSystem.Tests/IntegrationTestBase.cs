using BlueSchoolSystem.Models;
using BlueSchoolSystem.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace BlueSchoolSystem.Tests;

public class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory _factory;
    protected readonly HttpClient _client;

    public IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Helper sinh JWT token nội bộ để test
    /// </summary>
    protected string GenerateTestJwtToken(string role, string userName = "test_user")
    {
        var secretKey = "4vmySLK3dpVl100gVdZsqOrurvmVvyqCnrrXVpeW";
        var issuer = "blueschool";
        var audience = "blueschool_users";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Authenticate the HTTP client with a specific user role.
    /// </summary>
    protected void AuthenticateClient(string role = SD.Role_Admin, string username = "test_user")
    {
        var token = GenerateTestJwtToken(role, username);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clear client's authentication
    /// </summary>
    protected void DeAuthenticateClient()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }
}
