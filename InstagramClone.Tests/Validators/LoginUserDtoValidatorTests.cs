using FluentAssertions;
using InstagramClone.Application.Features.Auth.DTOs;
using InstagramClone.Application.Features.Auth.Validators;
using InstagramClone.Application.Features.Posts.Validators;
using System.ComponentModel.DataAnnotations;

namespace InstagramClone.Tests.Validators
{
    public class LoginUserDtoValidatorTests
    {
        private readonly LoginUserDtoValidator _validator;

        public LoginUserDtoValidatorTests()
        {
            _validator = new LoginUserDtoValidator();
        }

        [Fact]
        public void ValidDto_ShouldNotHaveAnyErrors()
        {
            var dto = new LoginUserDto
            {
                Identifier = "testuser",
                Password = "Password123"
            };

            var result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void EmptyIdentifier_ShouldHaveError()
        {
            var dto = new LoginUserDto { Identifier = "", Password = "Password123" };
            var result = _validator.Validate(dto);
            result.Errors.Should().Contain(e => e.PropertyName == "Identifier");
        }

        [Fact]
        public void EmptyPassword_ShouldHaveError()
        {
            var dto = new LoginUserDto { Identifier = "user", Password = "" };
            var result = _validator.Validate(dto);
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
        }
        
        [Fact]
        public void PasswordTooShort_ShouldHaveError()
        {
            var dto = new LoginUserDto { Identifier = "user", Password = "123" };
            var result = _validator.Validate(dto);
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
        }
    }
}
