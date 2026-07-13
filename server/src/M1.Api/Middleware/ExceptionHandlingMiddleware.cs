using System.Text.Json;
using M1.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace M1.Api.Middleware;

/// <summary>
/// Global error handler: every unhandled exception is logged with full detail
/// and returned to the client as a safe RFC 7807 ProblemDetails payload.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                DomainRuleException => (StatusCodes.Status400BadRequest, exception.Message),
                UnauthorizedException => (StatusCodes.Status401Unauthorized, exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }
}
