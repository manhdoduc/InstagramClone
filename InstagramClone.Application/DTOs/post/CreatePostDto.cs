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
        public string? Content { get; set; }
        public List<IFormFile> Files { get; set; } = [];
    }
}
