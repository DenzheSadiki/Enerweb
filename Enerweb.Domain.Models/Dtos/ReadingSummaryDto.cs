using System;
using System.Collections.Generic;

namespace Enerweb.Domain.Models.Dtos;

public sealed class ReadingSummaryDto
{
    public Guid MeterId { get; init; }
    public int ReadingCount { get; init; }
    public DateTime? FirstTimestamp { get; init; }
    public DateTime? LastTimestamp { get; init; }
    public decimal? FirstValue { get; init; }
    public decimal? LastValue { get; init; }
    public decimal? MinimumValue { get; init; }
    public decimal? MaximumValue { get; init; }
    public decimal? AverageValue { get; init; }
    // Cumulative meter usage is the last value minus the first, not their sum.
    public decimal Consumption { get; init; }
    public IReadOnlyList<DailyConsumptionDto> DailyBreakdown { get; init; } = Array.Empty<DailyConsumptionDto>();
}
