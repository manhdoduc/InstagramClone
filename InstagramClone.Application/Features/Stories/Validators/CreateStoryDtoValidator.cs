using FluentValidation;
using InstagramClone.Application.Features.Stories.DTOs;
using System.IO;
using System.Linq;

namespace InstagramClone.Application.Features.Stories.Validators;

public class CreateStoryDtoValidator : AbstractValidator<CreateStoryDto>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public CreateStoryDtoValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Please select an image for your story.")
            .Must(file => file != null && file.Length > 0).WithMessage("Image file cannot be empty.")
            .Must(file =>
            {
                if (file == null) return false;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return AllowedExtensions.Contains(ext);
            }).WithMessage("File format not allowed. Supported formats: .jpg, .jpeg, .png, .webp");

        RuleFor(x => x.Caption)
            .MaximumLength(500).WithMessage("Caption cannot exceed 500 characters.");
    }
}
