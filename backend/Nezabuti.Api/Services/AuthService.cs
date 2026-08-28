using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nezabuti.Api.Configuration;
using Nezabuti.Api.DTOs;

namespace Nezabuti.Api.Services;

public interface IAuthService
{
    LoginResponse? Login(LoginRequest request);
}

public sealed class AuthService : IAuthService
{
    private readonly AdminSettings _admin;
    private readonly JwtSettings _jwt;

    public AuthService(IOptions<AdminSettings> admin, IOptions<JwtSettings> jwt)
    {
        _admin = admin.Value;
        _jwt = jwt.Value;
    }

    public LoginResponse? Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(_admin.Username) || string.IsNullOrWhiteSpace(_admin.Password))
        {
            return null;
        }

        var usernameOk = FixedTimeEquals(request.Username, _admin.Username);
        var passwordOk = FixedTimeEquals(request.Password, _admin.Password);
        if (!usernameOk || !passwordOk)
        {
            return null;
        }

        var expires = DateTime.UtcNow.AddHours(_jwt.ExpirationHours);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims:
            [
                new Claim(ClaimTypes.Name, _admin.Username),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            expires: expires,
            signingCredentials: creds);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires
        };
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        if (aBytes.Length != bBytes.Length)
        {
            // Still compare to reduce timing leakage of length differences for short secrets
            CryptographicEquals(aBytes, aBytes);
            return false;
        }

        return CryptographicEquals(aBytes, bBytes);
    }

    private static bool CryptographicEquals(byte[] a, byte[] b)
    {
        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}
