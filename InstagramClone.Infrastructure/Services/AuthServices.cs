using InstagramClone.Application.Features.Auth.DTOs;
using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Models.Config;
using InstagramClone.Common.Results;
using InstagramClone.Domain.Entities;
using InstagramClone.Common.Helper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Infrastructure.Identity;
namespace InstagramClone.Infrastructure.Services{
    public class AuthServices(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtOptions,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork
        ) : IAuthServices
    {

        public async Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto)
        {
            var applicationUser = new ApplicationUser
            {
                UserName = registerUserDto.NickName,
                Email = registerUserDto.Email
            };

            var result = await userManager.CreateAsync(applicationUser, registerUserDto.Password);
            if (!result.Succeeded)
            {
                Log.Warning("Failed to register user {Email}. Errors: {@Errors}", registerUserDto.Email, result.Errors.Select(e => e.Description));
                var error = result.Errors.Select(e => new Error(ErrorCodes.BadRequest, e.Description)).ToArray();
                return Result<RegisteredUserDto>.BadRequest(error);
            }

            var roleResult = await userManager.AddToRoleAsync(applicationUser, RoleNames.User);

            var user = new AppUser(
                applicationUser.Id,
                registerUserDto.NickName,
                registerUserDto.Email,
                registerUserDto.FirstName,
                registerUserDto.LastName,
                RemoveDiacritics.RemoveDiacritic(registerUserDto.FirstName + " " + registerUserDto.LastName)
            );
            
            unitOfWork.Users.Add(user);
            await unitOfWork.SaveChangesAsync();

            var registeredUserDto = new RegisteredUserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = registerUserDto.NickName,
                Role = RoleNames.User
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

            var appUser = await unitOfWork.Users.GetByIdAsync(user.Id);
            if(appUser is null)
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "User profile not found"));

            var accessToken = await GenerateToken(user, appUser);
            var refreshToken = await GenerateRefreshToken();

            // Update Refresh Token in database (saving to AppUser profile or ApplicationUser? Usually stored in Auth/ApplicationUser, but let's assume it's moved to AppUser for simplicity or we should keep it in AppUser since it's mapped there)
            appUser.UpdateRefreshToken(await HashToken(refreshToken), DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryTime));
            unitOfWork.Users.Update(appUser);
            await unitOfWork.SaveChangesAsync();

            var token = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            Log.Information("User {UserId} logged in successfully", user.Id);
            return Result<TokenResponseDto>.Success(token);
        }

        private async Task<string> GenerateToken(ApplicationUser user, AppUser appUser)
        {
            var claim = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new (JwtRegisteredClaimNames.Email, user.Email!),
                new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new (JwtRegisteredClaimNames.Name, appUser.FullName)
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

        private Task<string> GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);
            return Task.FromResult(refreshToken);
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
            if (user == null) return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "User not found"));
            
            var hashedRefreshToken = await HashToken(refreshToken);

            var appUser = await unitOfWork.Users.GetByIdAsync(user.Id);

            if (appUser == null || appUser.RefreshToken != hashedRefreshToken || appUser.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Result<TokenResponseDto>.Failure(new Error(ErrorCodes.BadRequest, "Invalid refresh token"));
            }

            // Tạo access token mới
            var newAccessToken = await GenerateToken(user, appUser);
            var newRefreshToken = await GenerateRefreshToken();
            var hashedNewRefreshToken = await HashToken(newRefreshToken);

            // Cập nhật refresh token mới vào database
            appUser.UpdateRefreshToken(hashedNewRefreshToken, DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpiryTime));

            unitOfWork.Users.Update(appUser);
            var updateResult = await unitOfWork.SaveChangesAsync();
            if (updateResult <= 0)
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

        public async Task<Result<bool>> LogoutAsync()
        {
            if (!Guid.TryParse(currentUser.UserId, out var userId))
                return Result<bool>.Failure(new Error(ErrorCodes.BadRequest, "Invalid User ID"));

            var user = await unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return Result<bool>.Failure(new Error(ErrorCodes.NotFound, "User not found"));

            user.UpdateRefreshToken(null, DateTime.MinValue);

            unitOfWork.Users.Update(user);
            var result = await unitOfWork.SaveChangesAsync();
            if (result <= 0)
                return Result<bool>.Failure(new Error(ErrorCodes.Failure, "Failed to logout user"));

            return Result<bool>.Success(true);
        }
    }
}
