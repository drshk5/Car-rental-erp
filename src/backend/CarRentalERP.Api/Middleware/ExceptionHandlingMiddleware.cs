using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalERP.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemDetailsAsync(context, correlationId, exception);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = context.TraceIdentifier;
        }

        context.TraceIdentifier = correlationId;
        return correlationId;
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, string correlationId, Exception exception)
    {
        var (statusCode, title, type) = exception switch
        {
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Request validation failed", "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found", "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", "https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path} with correlation ID {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed for {Method} {Path} with correlation ID {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);
        }

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Cannot write problem details response because the response has already started.");
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = statusCode >= StatusCodes.Status500InternalServerError
                ? "The server failed to process the request."
                : exception.Message,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = correlationId;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, SerializerOptions));
    }
}
