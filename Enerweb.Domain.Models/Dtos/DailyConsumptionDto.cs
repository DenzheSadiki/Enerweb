using System;

namespace Enerweb.Domain.Models.Dtos;

public sealed class DailyConsumptionDto
{
    // UTC calendar date of the later reading in a consecutive pair.
    public DateOnly Date { get; init; }
    public decimal Consumption { get; init; }
}
