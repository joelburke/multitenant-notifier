using System.Net;
using System.Text.Json;
using NotificationPlatform.Domain.Exceptions;

namespace NotificationPlatform.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            TenantNotFoundException e       => (HttpStatusCode.NotFound, e.Message),
            RoutingRuleNotFoundException e  => (HttpStatusCode.NotFound, e.Message),
            DuplicateTenantSlugException e  => (HttpStatusCode.Conflict, e.Message),
            RateLimitExceededException e    => (HttpStatusCode.TooManyRequests, e.Message),
            _                               => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        if (statusCode == HttpStatusCode.TooManyRequests)
            context.Response.Headers.RetryAfter = "60";

        var body = JsonSerializer.Serialize(new { error = message }, JsonOptions);
        await context.Response.WriteAsync(body);
    }
}
