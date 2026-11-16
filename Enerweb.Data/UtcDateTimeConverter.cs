using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Enerweb.Data;

public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        value => value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc)) { }
}
