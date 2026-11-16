using Microsoft.AspNetCore.Builder;

namespace Enerweb.Middleware;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseErrorLogging(this IApplicationBuilder app)
        => app.UseMiddleware<ErrorLoggingMiddleware>();
}
