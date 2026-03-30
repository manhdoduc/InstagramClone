using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.DTOs.post
{
    public class CreatePostDto
    {
        [MaxLength(2200, ErrorMessage = "Caption cannot exceed 2200 characters.")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one file is required.")]
        [MinLength(1, ErrorMessage = "At least one file is required.")]
        [MaxLength(10, ErrorMessage = "You can upload a maximum of 10 files.")]
        public List<IFormFile> Files { get; set; } = [];
    }
}
