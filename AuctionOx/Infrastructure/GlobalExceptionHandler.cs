using AuctionOx.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AuctionOx.Infrastructure
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            if (exception is BusinessRuleException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Business Rule Violation";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = exception.Message;
            }
            else if (exception is NotFoundException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Detail = exception.Message;
            }
            else if (exception is UnauthorizedAccessException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                problemDetails.Title = "Unauthorized";
                problemDetails.Status = StatusCodes.Status401Unauthorized;
                problemDetails.Detail = exception.Message;
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Server Error";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = "An unexpected error occurred.";
            }

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; // We handled it
        }
    }
}
