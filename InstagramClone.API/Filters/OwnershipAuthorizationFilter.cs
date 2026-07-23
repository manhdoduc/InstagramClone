using InstagramClone.Application.Interfaces.Data;
using InstagramClone.Common.Constants;
using InstagramClone.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InstagramClone.API.Filters;

public class OwnershipAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var attribute = context.ActionDescriptor.EndpointMetadata
            .OfType<AuthorizeOwnershipAttribute>()
            .FirstOrDefault();

        if (attribute == null)
        {
            await next();
            return;
        }

        if (!context.RouteData.Values.TryGetValue(attribute.RouteIdParameterName, out var idObj) ||
            idObj == null ||
            !Guid.TryParse(idObj.ToString(), out var resourceId))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = $"Missing or invalid route parameter '{attribute.RouteIdParameterName}'."
            });
            return;
        }

        var userIdStr = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var currentUserId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var unitOfWork = context.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
        bool isAuthorized = false;

        if (context.HttpContext.User.IsInRole(RoleNames.Administrator))
        {
            isAuthorized = true;
        }
        else
        {
            if (attribute.ResourceType == typeof(Post))
            {
                var post = await unitOfWork.Posts.GetByIdAsync(resourceId);
                if (post == null)
                {
                    context.Result = new NotFoundObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Not Found",
                        Detail = "Post not found."
                    });
                    return;
                }
                isAuthorized = post.UserId == currentUserId;
            }
            else if (attribute.ResourceType == typeof(Comment))
            {
                var comment = await unitOfWork.Comments.GetByIdWithPostAsync(resourceId);
                if (comment == null)
                {
                    context.Result = new NotFoundObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Not Found",
                        Detail = "Comment not found."
                    });
                    return;
                }
                isAuthorized = comment.UserId == currentUserId || comment.Post?.UserId == currentUserId;
            }
            else if (attribute.ResourceType == typeof(Message))
            {
                var message = await unitOfWork.Chats.GetMessageByIdAsync(resourceId);
                if (message == null)
                {
                    context.Result = new NotFoundObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Not Found",
                        Detail = "Message not found."
                    });
                    return;
                }
                isAuthorized = message.SenderId == currentUserId;
            }
        }

        if (!isAuthorized)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = ErrorCodes.Forbid,
                Detail = "You do not have permission to access or modify this resource."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
