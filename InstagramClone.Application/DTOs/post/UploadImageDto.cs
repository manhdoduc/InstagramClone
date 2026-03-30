using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InstagramClone.Application.DTOs.post
{
     public class UploadImageDto
     {
        [Required(ErrorMessage ="Please choose image")]
        public required IFormFile File { get; set; }
    }
}
