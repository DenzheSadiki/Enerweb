using System;

namespace Enerweb.Domain.Models;

public sealed class DatabaseLog
{
    public Guid Id { get; set; }
    public Guid? ErrorId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Error";
    public string TraceId { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMilliseconds { get; set; }
}
