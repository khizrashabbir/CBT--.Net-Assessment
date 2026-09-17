using System.Net;
using System.Text.Json;
using KoperasiTentera.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace KoperasiTentera.API.Middleware;

/// <summary>
/// Translates <see cref="AppException"/> (and any unhandled exception) into an
/// RFC 7807 <see cref="ProblemDetails"/> response carrying the stable error code
/// the mobile app keys on, via the "code" extension.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException appException)
        {
            _logger.LogWarning(appException, "Handled application exception with code {Code}", appException.Code);
            await WriteProblemDetailsAsync(context, (int)appException.StatusCode, appException.Code, appException.Message, appException.Details);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception");
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "UNEXPECTED_ERROR", "An unexpected error occurred.", null);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string code, string message, object? details)
    {
        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = code,
            Detail = message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["code"] = code;
        if (details is not null)
        {
            problemDetails.Extensions["details"] = details;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
