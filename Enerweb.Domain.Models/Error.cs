using System;

namespace Enerweb.Domain.Models;

public sealed class Error
{
    public Guid Id { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
}
