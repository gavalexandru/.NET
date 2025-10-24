using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Order_Management_API.Common.Middleware;

    
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationMiddleware> logger)
    {
        var correlationId = GetOrSetCorrelationId(context);
        
        using (logger.BeginScope("{@CorrelationId}", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrSetCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationIdValues) && correlationIdValues.FirstOrDefault() is { } correlationId)
        {
            context.TraceIdentifier = correlationId;
        }
        else
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Add(CorrelationIdHeaderName, correlationId);
        }

        if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
        {
            context.Response.Headers.Add(CorrelationIdHeaderName, context.TraceIdentifier);
        }
        
        return context.TraceIdentifier;
    }
}

