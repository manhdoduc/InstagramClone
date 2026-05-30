using InstagramClone.Application.Interfaces.Services;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace InstagramClone.Infrastructure.Services
{
    public class LocalStorageServices(IWebHostEnvironment webHostEnvironment) : IStorageServices
    {
        public readonly string[] _allowExtention = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        public async Task<Result<string>> UploadImageAsync(IFormFile file, string userId, string folderType, int maxWidth, int maxHeight)
        {
            // 1/ Kiểm tra file có tồn tại và không rỗng
            if (file is null || file.Length <= 0)
            {
                return Result<string>.Failure(new Error(ErrorCodes.Failure, "File empty, file not empty"));
            }

            if(file.Length > 5*1024*1024)
            {
                return Result<string>.Failure(new Error(ErrorCodes.Failure, "File to Large, file < 5mb"));
            }
            // 2. Kiểm tra định dạng file có hợp lệ không (chỉ cho phép .jpg, .jpeg, .png, .webp)
            var extention = Path.GetExtension(file.FileName).ToLowerInvariant();
            if(!_allowExtention.Contains(extention) || !file.ContentType.StartsWith("image/"))
            {
                return Result<string>.Failure(new Error(ErrorCodes.Failure, "File extention not allowed, file extention must be .jpg, .jpeg, .png, .webp"));
            }

            try
            {
                // 3. Tạo đường dẫn lưu trữ: wwwroot/media/{folderType}/{userId}/
                // Ví dụ: wwwroot/media/avatars/user123/
                var folderPath = Path.Combine(webHostEnvironment.WebRootPath, "media", folderType, userId);
                if(!Directory.Exists(folderPath)) 
                {
                    Directory.CreateDirectory(folderPath);
                }
                // 4. Tạo tên file theo chuẩn: timestamp_random.jpg
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var random = Guid.NewGuid().ToString("N").Substring(0, 8);
                var fileName = $"{timestamp}_{random}.jpg";
                var filePath = Path.Combine(folderPath, fileName);

                // 5. xử lý lưu file vào đường dẫn đã tạo (ImageSharp)
                using (var stream = file.OpenReadStream())
                using (var image = await Image.LoadAsync(stream))
                {
                    // Resize nếu ảnh quá lớn (ví dụ: max width hoặc height là 1080px)
                    image.Mutate(x => x.Resize(new ResizeOptions{
                       Mode = ResizeMode.Max,
                       Size = new Size(maxWidth , maxHeight)
                    }));
                     
                    // Lưu ảnh dưới định dạng JPEG với chất lượng 80
                    var encoder = new JpegEncoder { Quality = 80 };
                    await image.SaveAsJpegAsync(filePath, encoder);

                    // 6. Trả về URL tương đối để lưu vào Database
                    // URL sẽ có dạng: /media/avatars/user123/1680000000_abcd1234.jpg
                    var relativeUrl = $"/media/{folderType}/{userId}/{fileName}";
                    return Result<string>.Success(relativeUrl);
                }


            }
            catch (Exception ex)
            {
                return Result<string>.Failure(new Error(ErrorCodes.Failure, $"File upload failed: {ex.Message}"));
            }

        }


        public async Task<Result> DeleteFile(string fileUrl)
        {
            if(string.IsNullOrWhiteSpace(fileUrl))
            {
                return Result.Success();
            }

            // Security: Resolve full path and ensure it stays within wwwroot to prevent path traversal
            var filePath = Path.GetFullPath(
                Path.Combine(webHostEnvironment.WebRootPath, fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

            if (!filePath.StartsWith(webHostEnvironment.WebRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(new Error(ErrorCodes.Failure, "Invalid file path — access denied."));
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Result.Success();
        }

        
    }
}
