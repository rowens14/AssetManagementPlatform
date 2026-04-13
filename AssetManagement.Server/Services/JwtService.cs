/*
 * FILE: JwtService.cs
 * PROJECT: AssetManagement.Server / Services
 * PURPOSE: Generates signed JWT tokens on successful login.
 *          Token claims include the user's ID, username, and role so the server
 *          can identify the caller on every subsequent request without a database
 *          lookup. The signing key, issuer, audience, and expiry are all read from
 *          appsettings.json under the "Jwt" section.
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssetManagement.Server.Data;
using Microsoft.IdentityModel.Tokens;

namespace AssetManagement.Server.Services;

public class JwtService(IConfiguration config)
{
    public string GenerateToken(AppUser user)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry      = DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpiryMinutes"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("userId", user.Id.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             config["Jwt:Issuer"],
            audience:           config["Jwt:Audience"],
            claims:             claims,
            expires:            expiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
