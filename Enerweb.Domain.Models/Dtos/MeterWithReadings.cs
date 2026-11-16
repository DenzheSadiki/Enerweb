using System.Collections.Generic;

namespace Enerweb.Domain.Models.Dtos;

public sealed record MeterWithReadings(Meters Meter, IReadOnlyList<Reading> Readings);
