using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application;
using TaskManager.Domain;

namespace TaskManager.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            DomainValidationException => 400,
            InvalidCredentialsException => 401,
            NotFoundException => 404,
            DuplicateEmailException or DuplicateTaskException => 409,
            _ => 500
        };
        if (status == 500) logger.LogError(exception, "Unhandled API error. Trace {TraceId}", context.TraceIdentifier);
        ProblemDetails problem = exception is DomainValidationException validation
            ? new ValidationProblemDetails(validation.Errors.ToDictionary(kv => kv.Key, kv => kv.Value)) : new ProblemDetails();
        problem.Status = status;
        problem.Title = status switch { 400 => "Validation failed", 401 => "Invalid credentials", 404 => "Resource not found", 409 => exception is DuplicateEmailException ? "Email already registered" : "Task already exists", _ => "An unexpected error occurred" };
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        await problems.WriteAsync(new() { HttpContext = context, ProblemDetails = problem });
        return true;
    }
}
