using FluentValidation;
using InstagramClone.Application.Features.Auth.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Auth.Validators{
    public class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
    {
        public LoginUserDtoValidator() 
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("NickName Or Email is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        }
    }
}
