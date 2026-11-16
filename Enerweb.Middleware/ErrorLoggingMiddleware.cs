using Enerweb.Domain.Models;
using System.Diagnostics;
using Enerweb.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Enerweb.Middleware;

public sealed class ErrorLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ErrorLoggingMiddleware> logger;

    public ErrorLoggingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory,
        ILogger<ErrorLoggingMiddleware> logger)
        => (this.next, this.scopeFactory, this.logger) = (next, scopeFactory, logger);

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        Error? error = null;
        ProblemDetails? problem = null;
        int? logStatus = null;
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logStatus = 499;
            throw;
        }
        catch (Exception exception)
        {
            var status = exception switch
            {
                ArgumentException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                InvalidOperationException or DbUpdateException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };
            logger.LogError(exception, "Request failed with status {StatusCode}; trace {TraceId}", status, context.TraceIdentifier);
            error = new Error
            {
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.ToString()
            };
            logStatus = context.Response.HasStarted ? context.Response.StatusCode : status;

            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = status;
            problem = new ProblemDetails
            {
                Status = status,
                Title = status switch
                {
                    500 => "An unexpected error occurred.",
                    409 when exception is DbUpdateException => "The operation conflicts with the current database state.",
                    _ => exception.Message
                }
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
        }
        finally
        {
            stopwatch.Stop();
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ILogService>();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var log = new DatabaseLog
                {
                    TraceId = context.TraceIdentifier,
                    RequestPath = context.Request.Path.Value ?? string.Empty,
                    HttpMethod = context.Request.Method,
                    StatusCode = logStatus ?? context.Response.StatusCode,
                    DurationMilliseconds = stopwatch.ElapsedMilliseconds
                };
                if (error == null) await service.LogRequestAsync(log, timeout.Token);
                else
                {
                    // A response already started may still have a successful HTTP status.
                    if (log.StatusCode < 400) log.StatusCode = 500;
                    await service.LogErrorAsync(error, log, timeout.Token);
                }
            }
            catch (Exception loggingException)
            {
                logger.LogError(loggingException, "Database request logging failed; trace {TraceId}", context.TraceIdentifier);
            }
        }
        if (problem != null)
        {
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
