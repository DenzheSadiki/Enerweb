using System;

namespace Enerweb.Domain.Models.Dtos;

public sealed class ReadingSummaryRequest
{
    public Guid MeterId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
