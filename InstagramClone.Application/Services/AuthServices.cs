using InstagramClone.Application.DTOs.Auth;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Models.Config;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using Serilog;
namespace InstagramClone.Application.Services
{
    public class AuthServices(
        UserManager<AppUser> userManager,
        IOptions<JwtSettings> jwtOptions
        ) : IAuthServices
    {

        public async Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto)
        {
            var user = new AppUser
            {
                UserName = registerUserDto.NickName,
                Email = registerUserDto.Email,
                FirstName = registerUserDto.FirstName,
                LastName = registerUserDto.LastName
            };

            var result = await userManager.CreateAsync(user, registerUserDto.Password);
            if (!result.Succeeded)
            {
                Log.Warning("Failed to register user {Email}. Errors: {@Errors}", registerUserDto.Email, result.Errors.Select(e => e.Description));
                var error = result.Errors.Select(e => new Error(ErrorCodes.BadRequest, e.Description)).ToArray();
                return Result<RegisteredUserDto>.BadRequest(error);
            }

            var roleResult = await userManager.AddToRoleAsync(user, registerUserDto.Role);

            var registeredUserDto = new RegisteredUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = registerUserDto.NickName,
                Role = registerUserDto.Role
            };

            Log.Information("User {UserId} registered successfully with email {Email}", user.Id, user.Email);
            return Result<RegisteredUserDto>.Success(registeredUserDto);
        }

        public async Task<Result<TokenResponseDto>> LoginAsync(LoginUserDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Identifier);
            if (user is null)
            {
                user = await userManager.FindByNameAsync(loginDto.Identifier); // Thử tìm theo username nếu không tìm thấy theo email
            }
            if(user is null) 
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid login attempt"));

            var password = await userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!password)
            {
                Log.Warning("Invalid login attempt for {Email}", loginDto.Identifier);
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid login attempt"));
            }

            var accessToken = await GenerateToken(user);
            var refreshToken = await GenerateRefreshToken();

            // Update Refresh Token in database
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryTime); // Set refresh token expiry time
            await userManager.UpdateAsync(user);

            var token = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            Log.Information("User {UserId} logged in successfully", user.Id);
            return Result<TokenResponseDto>.Success(token);
        }

        private async Task<string> GenerateToken(AppUser user)
        {
            var claim = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, user.Id),
                new (JwtRegisteredClaimNames.Email, user.Email!),
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new (JwtRegisteredClaimNames.Name, user.FullName)
            };

            var roles = await userManager.GetRolesAsync(user);

            claim.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // set Jwt Key credentials
            var sercurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key));
            var credentials = new SigningCredentials(sercurityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtOptions.Value.Issuer,
                audience: jwtOptions.Value.Audience,
                claims: claim,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToInt32(jwtOptions.Value.ExpiryInMinutes)),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        //  Logic xử lý Refresh Token
        public async Task<Result<TokenResponseDto>> RefreshTokenAsync(TokenResponseDto tokenResponseDto)
        {
            string accessToken = tokenResponseDto.AccessToken;
            string refreshToken = tokenResponseDto.RefreshToken;

            // Lấy claim từ access token đã hết hạn để lấy email (hoặc id) của user
            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid token"));

            // Thử lấy email từ các claim thông thường (JWT email claim, ClaimTypes.Email, hoặc NameIdentifier)
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                        ?? principal.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid token - Email/Id is null or empty"));

            // Tìm user theo email trước, nếu không có thì thử lấy theo Id
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = await userManager.FindByIdAsync(email);
            }

            // Kiểm tra tính hợp lệ của refresh token
            var hashedRefreshToken = await HashToken(refreshToken);

            if (user == null || user.RefreshToken != hashedRefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid refresh token"));
            }

            // Tạo access token mới
            var newAccessToken = await GenerateToken(user);
            var newRefreshToken = await GenerateRefreshToken();
            var hashedNewRefreshToken = await HashToken(newRefreshToken);

            // Cập nhật refresh token mới vào database
            user.RefreshToken = hashedNewRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryTime);

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.Failure, "Error updating database"));

            var result = new TokenResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            return Result<TokenResponseDto>.Success(result);
        }


        // 4. Hàm phụ trợ: Đọc token đã hết hạn
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Value.Audience,
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Value.Issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key)),
                    ValidateLifetime = false // QUAN TRỌNG: Không check hạn (vì nó đang hết hạn mà)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        // 5. Hash Token để so sánh (nếu muốn tăng cường bảo mật, không lưu refresh token dưới dạng plain text)
        private async Task<string> HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

    }
}
