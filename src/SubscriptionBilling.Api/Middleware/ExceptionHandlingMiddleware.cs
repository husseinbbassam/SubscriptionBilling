using FluentValidation;
using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Api.Middleware;

/// <summary>
/// Translates domain and validation exceptions into ProblemDetails responses
/// so we don't leak stack traces and we return the right status codes.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException vex)
        {
            _logger.LogInformation("Validation failed: {Message}", vex.Message);
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Validation failed", vex.Message);
        }
        catch (DomainException dex)
        {
            _logger.LogInformation("Domain rule violated: {Message}", dex.Message);
            await WriteProblem(context, StatusCodes.Status409Conflict, "Domain rule violated", dex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Internal server error", "An unexpected error occurred.");
        }
    }

    private static Task WriteProblem(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        return Results.Problem(title: title, detail: detail, statusCode: statusCode)
            .ExecuteAsync(context);
    }
}
