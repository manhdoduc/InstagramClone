using FluentAssertions;
using InstagramClone.API.Controllers;
using InstagramClone.Common.Constants;
using InstagramClone.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InstagramClone.Tests.Controllers
{
    public class TestController : BaseApiController
    {
        public ActionResult<string> TestGenericSuccess(string value)
        {
            var result = Result<string>.Success(value);
            return ToActionResult(result);
        }

        public ActionResult TestNonGenericSuccess()
        {
            var result = Result.Success();
            return ToActionResult(result);
        }

        public ActionResult<string> TestGenericFailure(Error[] errors)
        {
            var result = Result<string>.Failure(errors);
            return ToActionResult(result);
        }

        public ActionResult TestNonGenericFailure(Error[] errors)
        {
            var result = Result.Failure(errors);
            return ToActionResult(result);
        }
    }

    public class BaseApiControllerTests
    {
        [Fact]
        public void ToActionResult_GenericSuccess_ReturnsOkWithValue()
        {
            // Arrange
            var controller = new TestController();
            var expectedValue = "test value";

            // Act
            var result = controller.TestGenericSuccess(expectedValue);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void ToActionResult_NonGenericSuccess_ReturnsNoContent()
        {
            // Arrange
            var controller = new TestController();

            // Act
            var result = controller.TestNonGenericSuccess();

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Theory]
        [InlineData(ErrorCodes.NotFound, StatusCodes.Status404NotFound)]
        [InlineData(ErrorCodes.Validation, StatusCodes.Status400BadRequest)]
        [InlineData(ErrorCodes.BadRequest, StatusCodes.Status400BadRequest)]
        [InlineData(ErrorCodes.Conflict, StatusCodes.Status409Conflict)]
        [InlineData(ErrorCodes.Forbid, StatusCodes.Status403Forbidden)]
        [InlineData("UnknownErrorCode", StatusCodes.Status500InternalServerError)]
        public void MapErrorsToResponse_MapsErrorCodesCorrectly(string errorCode, int? expectedStatusCode)
        {
            // Arrange
            var controller = new TestController();
            var errors = new[] { new Error(errorCode, "Test error description") };

            // Act
            var result = controller.TestNonGenericFailure(errors);

            // Assert
            var objectResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
            
            if (errorCode == ErrorCodes.Validation)
            {
                var validationProblem = objectResult.Value.Should().BeAssignableTo<ValidationProblemDetails>().Subject;
                validationProblem.Detail.Should().Contain("Test error description");
            }
            else
            {
                objectResult.StatusCode.Should().Be(expectedStatusCode);
                var problemDetails = objectResult.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
                problemDetails.Detail.Should().Contain("Test error description");
            }
        }

        [Fact]
        public void MapErrorsToResponse_EmptyErrors_ReturnsInternalServerError()
        {
            // Arrange
            var controller = new TestController();
            
            // Act
            var result = controller.TestNonGenericFailure(Array.Empty<Error>());

            // Assert
            var objectResult = result.Should().BeAssignableTo<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        }
    }
}
