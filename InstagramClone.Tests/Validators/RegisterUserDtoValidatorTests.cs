using FluentAssertions;
using InstagramClone.Application.DTOs.Auth;
using InstagramClone.Application.Validators;
using Microsoft.AspNetCore.Http;
using Moq;

namespace InstagramClone.Tests.Validators
{
    public class RegisterUserDtoValidatorTests
    {
        private readonly RegisterUserDtoValidator _validator;

        public RegisterUserDtoValidatorTests()
        {
            _validator = new RegisterUserDtoValidator();
        }

        [Fact]
        public void ValidDto_ShouldNotHaveAnyErrors()
        {
            var dto = new RegisterUserDto
            {
                Email = "test@example.com",
                Password = "Password123",
                FirstName = "John",
                LastName = "Doe",
                NickName = "johndoe"
            };

            var result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void EmptyEmail_ShouldHaveError()
        {
            var dto = new RegisterUserDto { Email = "" };
            var result = _validator.Validate(dto);
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }
        
        [Fact]
        public void InvalidEmail_ShouldHaveError()
        {
            var dto = new RegisterUserDto { Email = "invalid-email" };
            var result = _validator.Validate(dto);
            result.Errors.Should().Contain(e => e.PropertyName == "Email");
        }

        [Fact]
        public void PasswordTooShort_ShouldHaveError()
        {
            var dto = new RegisterUserDto { Password = "Pass1" };
            var result = _validator.Validate(dto);
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
        }

        [Fact]
        public void EmptyNames_ShouldHaveErrors()
        {
            var dto = new RegisterUserDto { FirstName = "", LastName = "", NickName = "" };
            var result = _validator.Validate(dto);
            result.Errors.Should().Contain(e => e.PropertyName == "FirstName");
            result.Errors.Should().Contain(e => e.PropertyName == "LastName");
            result.Errors.Should().Contain(e => e.PropertyName == "NickName");
        }
    }
}
