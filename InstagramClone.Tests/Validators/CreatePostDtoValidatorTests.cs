using FluentAssertions;
using InstagramClone.Application.DTOs.post;
using InstagramClone.Application.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Collections.Generic;

namespace InstagramClone.Tests.Validators
{
    public class CreatePostDtoValidatorTests
    {
        private readonly CreatePostDtoValidator _validator;

        public CreatePostDtoValidatorTests()
        {
            _validator = new CreatePostDtoValidator();
        }

        [Fact]
        public void ValidDto_ShouldNotHaveAnyErrors()
        {
            var mockFile = new Mock<IFormFile>();
            var dto = new CreatePostDto
            {
                Content = "This is a valid post content.",
                Files = new List<IFormFile> { mockFile.Object }
            };

            var result = _validator.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void ContentTooLong_ShouldHaveError()
        {
            var mockFile = new Mock<IFormFile>();
            var dto = new CreatePostDto
            {
                Content = new string('a', 2201),
                Files = new List<IFormFile> { mockFile.Object }
            };

            var result = _validator.Validate(dto);

            result.Errors.Should().Contain(e => e.PropertyName == "Content");
        }

        [Fact]
        public void EmptyFilesList_ShouldHaveError()
        {
            var dto = new CreatePostDto
            {
                Content = "Post without files",
                Files = new List<IFormFile>()
            };

            var result = _validator.Validate(dto);

            result.Errors.Should().Contain(e => e.PropertyName == "Files");
        }

        [Fact]
        public void TooManyFiles_ShouldHaveError()
        {
            var files = new List<IFormFile>();
            for (int i = 0; i < 11; i++)
            {
                files.Add(new Mock<IFormFile>().Object);
            }

            var dto = new CreatePostDto
            {
                Content = "Post with too many files",
                Files = files
            };

            var result = _validator.Validate(dto);

            result.Errors.Should().Contain(e => e.PropertyName == "Files");
        }
    }
}
