using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InstagramClone.Application.Features.Stories.DTOs;

public class CreateStoryDto
{
    [Required(ErrorMessage = "Please choose an image for your story")]
    public required IFormFile File { get; set; }

    [MaxLength(500, ErrorMessage = "Caption cannot exceed 500 characters")]
    public string? Caption { get; set; }
}
