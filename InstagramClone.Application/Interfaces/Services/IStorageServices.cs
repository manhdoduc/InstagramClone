using InstagramClone.Common.Results;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Interfaces.Services;
public interface IStorageServices
{
    // Trả về URL tương đối của ảnh (ví dụ: /media/user123/1680000_abc.jpg)
    Task<Result<string>> UploadImageAsync(IFormFile formFile, string fileName, string folderType, int maxWidth = 1080, int maxHeight = 1350);

    Task<Result> DeleteFile(string fileUrl);
}

