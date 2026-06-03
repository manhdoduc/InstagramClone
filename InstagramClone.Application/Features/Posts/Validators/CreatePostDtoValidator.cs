using FluentValidation;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Posts.Validators{
    public class CreatePostDtoValidator : AbstractValidator<CreatePostDto>
    {
        public CreatePostDtoValidator()
        {
            RuleFor(x => x.Content)
                .MaximumLength(2200).WithMessage("Caption cannot exceed 2200 characters.");

            RuleFor(x => x.Files)
                .NotEmpty().WithMessage("At least one file is required.")
                .Must(x => x.Count <= 10).WithMessage("You can upload a maximum of 10 files.");
        }
    }
}
