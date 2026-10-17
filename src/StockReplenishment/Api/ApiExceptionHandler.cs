using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StockReplenishment.Domain;

namespace StockReplenishment.Api;

/// <summary>Turns domain exceptions into standard problem-details responses.</summary>
public class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            InvalidStateException => (StatusCodes.Status409Conflict, "Not allowed in the current state"),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (0, null),
        };

        if (status == 0)
        {
            return false; // Unexpected: let the default handler return a 500.
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
        });
    }
}