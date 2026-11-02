using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace TaskManager.Api;

internal static class ProblemResponses
{
    public static async ValueTask WriteAsync(HttpContext context, ProblemDetails problem, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        problem.Title ??= ReasonPhrases.GetReasonPhrase(context.Response.StatusCode);
        problem.Instance ??= context.Request.Path;
        problem.Extensions.TryAdd("traceId", context.TraceIdentifier);

        var service = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        if (await service.TryWriteAsync(new() { HttpContext = context, ProblemDetails = problem })) return;

        // Error responses deliberately fall back to Problem JSON when Accept cannot be satisfied.
        // Serialize the runtime type so ValidationProblemDetails retains its errors dictionary.
        await context.Response.WriteAsJsonAsync((object)problem, options: null,
            contentType: "application/problem+json", cancellationToken: cancellationToken);
    }
}
