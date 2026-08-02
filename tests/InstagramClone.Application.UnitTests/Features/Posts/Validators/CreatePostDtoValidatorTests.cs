using FluentAssertions;
using FluentValidation.TestHelper;
using InstagramClone.Application.Features.Posts.DTOs;
using InstagramClone.Application.Features.Posts.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace InstagramClone.Application.UnitTests.Features.Posts.Validators;

public class CreatePostDtoValidatorTests
{
    private readonly CreatePostDtoValidator _validator;

    public CreatePostDtoValidatorTests()
    {
        _validator = new CreatePostDtoValidator();
    }

    [Fact]
    public void Validate_ShouldNotHaveError_WhenDataIsValid()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var dto = new CreatePostDto
        {
            Content = "Valid content",
            Files = new List<IFormFile> { fileMock.Object }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenContentExceedsMaxLength()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var dto = new CreatePostDto
        {
            Content = new string('A', 2201),
            Files = new List<IFormFile> { fileMock.Object }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Caption cannot exceed 2200 characters.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFilesListIsEmpty()
    {
        // Arrange
        var dto = new CreatePostDto
        {
            Content = "Valid content",
            Files = new List<IFormFile>() // Empty list
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Files)
              .WithErrorMessage("At least one file is required.");
    }

    [Fact]
    public void Validate_ShouldHaveError_WhenFilesListExceedsMaxCount()
    {
        // Arrange
        var files = new List<IFormFile>();
        for (int i = 0; i < 11; i++)
        {
            files.Add(new Mock<IFormFile>().Object);
        }

        var dto = new CreatePostDto
        {
            Content = "Valid content",
            Files = files
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Files)
              .WithErrorMessage("You can upload a maximum of 10 files.");
    }
}
