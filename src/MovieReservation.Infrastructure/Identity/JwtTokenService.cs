using MovieReservation.Domain.Entities;
using MovieReservation.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace MovieReservation.Infrastructure.Identity;


/// <summary>
/// Cài đặt ITokenService sử dụng JWT để sinh access token và refresh token.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration configuration;
    private readonly TimeProvider timeProvider;
    public JwtTokenService(IConfiguration configuration, TimeProvider timeProvider)
    {
        this.configuration = configuration;
        this.timeProvider = timeProvider;
    }
    /// <summary>
    /// Tạo access token dựa trên thông tin người dùng
    /// </summary>
    public AccessTokenResult GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(configuration.GetValue<int>("Jwt:AccessTokenMinutes"));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: signingCredentials
        );
        return new AccessTokenResult
        (
             new JwtSecurityTokenHandler().WriteToken(token),
             expiresAt
        );
    }
    /// <summary>
    /// Tạo refresh token ngẫu nhiên và băm nó bằng SHA-256
    /// </summary>
    public RefreshTokenResult GenerateRefreshToken()
    {
        
        var refreshToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
        var refreshTokenHash = HashToken(refreshToken);
        var expiresAt = timeProvider.GetUtcNow().UtcDateTime.AddDays(configuration.GetValue<int>("Jwt:RefreshTokenDays"));
        return new RefreshTokenResult(refreshToken, refreshTokenHash, expiresAt);
        
    }
    
    /// <summary>
    /// Băm token bằng SHA-256 và trả về chuỗi Hex   
    /// </summary>
    public string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }
}