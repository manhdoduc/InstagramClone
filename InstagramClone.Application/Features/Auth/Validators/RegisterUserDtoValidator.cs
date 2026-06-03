using FluentValidation;
using FluentValidation.Validators;
using InstagramClone.Application.Features.Auth.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramClone.Application.Features.Auth.Validators{
    public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserDtoValidator() 
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email not empty")
                .EmailAddress().WithMessage("Email is not in the correct format");

            RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6).WithMessage("The password must be at least 6 characters")
            .Matches(@"[A-Z]").WithMessage("The password must have at least 1 uppercase letter")
            .Matches(@"[0-9]").WithMessage("The password must have at least 1 digit");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100).WithMessage("First name max with 100 character");

            RuleFor(x => x.LastName)
               .NotEmpty()
               .MaximumLength(100).WithMessage("Last name max with 100 character");

            RuleFor(x => x.NickName)
               .NotEmpty()
               .MaximumLength(20).WithMessage("First name max with 100 character");
        }
    }
}
