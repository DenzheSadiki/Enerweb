using System.Text.Json;
using Enerweb.Domain.Models;
using Enerweb.Interfaces.Services;
using Enerweb.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enerweb.Api.Tests;

public sealed class ErrorLoggingMiddlewareTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnexpectedErrorsHaveSafeResponsesEvenWhenDatabaseLoggingFails(bool failLogging)
    {
        var logService = new RecordingLogService { FailLogging = failLogging };
        using var provider = new ServiceCollection().AddScoped<ILogService>(_ => logService).BuildServiceProvider();
        var middleware = new ErrorLoggingMiddleware(_ => throw new Exception("private failure detail"),
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ErrorLoggingMiddleware>.Instance);
        var context = new DefaultHttpContext { TraceIdentifier = "test-trace" };
        context.Request.Path = "/api/example";
        context.Request.QueryString = new QueryString("?token=private-token");
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        Assert.Equal(500, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.DoesNotContain("private failure detail", body);
        Assert.Contains("test-trace", body);
        Assert.Equal(500, JsonDocument.Parse(body).RootElement.GetProperty("status").GetInt32());
        Assert.Equal("private failure detail", logService.Error!.Message);
        Assert.Equal("/api/example", logService.Log!.RequestPath);
        Assert.Equal("test-trace", logService.Log.TraceId);
        Assert.Equal(1, logService.Calls);
    }

    [Fact]
    public async Task SuccessfulRequestsCreateRequestLogsWithoutErrors()
    {
        var logService = new RecordingLogService();
        using var provider = new ServiceCollection().AddScoped<ILogService>(_ => logService).BuildServiceProvider();
        var middleware = new ErrorLoggingMiddleware(_ => Task.CompletedTask,
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ErrorLoggingMiddleware>.Instance);
        await middleware.InvokeAsync(new DefaultHttpContext());
        Assert.Equal(1, logService.Calls);
        Assert.Null(logService.Error);
        Assert.Equal(200, logService.Log!.StatusCode);
    }

    [Fact]
    public async Task ClientCancellationIsNotRecordedAsAnApplicationFailure()
    {
        var logService = new RecordingLogService();
        using var provider = new ServiceCollection().AddScoped<ILogService>(_ => logService).BuildServiceProvider();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var context = new DefaultHttpContext { RequestAborted = cancellation.Token };
        var middleware = new ErrorLoggingMiddleware(_ => throw new OperationCanceledException(),
            provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ErrorLoggingMiddleware>.Instance);
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
        Assert.Equal(1, logService.Calls);
        Assert.Null(logService.Error);
        Assert.Equal(499, logService.Log!.StatusCode);
    }

    private sealed class RecordingLogService : ILogService
    {
        public bool FailLogging { get; init; }
        public int Calls { get; private set; }
        public Error? Error { get; private set; }
        public DatabaseLog? Log { get; private set; }

        public Task LogRequestAsync(DatabaseLog log, CancellationToken cancellationToken = default)
        {
            Calls++;
            Log = log;
            if (FailLogging) throw new IOException("Database unavailable");
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(Error error, DatabaseLog log, CancellationToken cancellationToken = default)
        {
            Calls++;
            Error = error;
            Log = log;
            if (FailLogging) throw new IOException("Database unavailable");
            return Task.CompletedTask;
        }
    }
}
