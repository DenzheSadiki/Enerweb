using System;
using Enerweb.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Enerweb.Data;

internal static class DatabaseSeed
{
    internal static void Configure(ModelBuilder modelBuilder)
    {
        // Fixed IDs and dates keep migrations deterministic and prevent duplicate seeds.
        var siteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var meterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var createdAt = new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Site>().HasData(new
        {
            Id = siteId, SiteName = "Demo Site", SiteCode = "DEMO-001",
            IsActive = true, CreatedAt = createdAt
        });
        modelBuilder.Entity<Meters>().HasData(new
        {
            Id = meterId, SiteId = siteId, MeterNumber = "DEMO-MTR-001",
            SerialNumber = "DEMO-SN-001", MeterName = "Demo Electricity Meter",
            MeterType = "Electricity", MeterCategory = "Postpaid", UnitOfMeasurement = "kWh",
            CurrentReading = 1580m, PreviousReading = 1534m, Consumption = 46m,
            InstallationDate = createdAt, LastReadingDate = createdAt.AddHours(9),
            PropertyId = "DEMO-PROP-001", AccountNumber = "DEMO-ACC-001",
            IsActive = true, Status = "Active", CreatedAt = createdAt
        });
        modelBuilder.Entity<Reading>().HasData(new
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            MeterId = meterId, Value = 1250m, Timestamp = createdAt
        });
        var hourlyValues = new[] { 1278m, 1312m, 1341m, 1383m, 1410m, 1456m, 1489m, 1534m, 1580m };
        for (var index = 0; index < hourlyValues.Length; index++)
        {
            modelBuilder.Entity<Reading>().HasData(new
            {
                Id = Guid.Parse($"33333333-3333-3333-3333-{index + 1:D12}"),
                MeterId = meterId,
                Value = hourlyValues[index],
                Timestamp = createdAt.AddHours(index + 1)
            });
        }
        modelBuilder.Entity<Address>().HasData(new
        {
            id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            AddressLine1 = "1 Demo Street", AddressLine2 = "",
            City = "Johannesburg", Province = "Gauteng", PostalCode = "2000",
            Country = "South Africa", CreatedAt = createdAt
        });
        modelBuilder.Entity<ContactPerson>().HasData(new
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "Demo Contact", ContactNumber = "0000000000",
            Email = "demo@example.com", CreatedAt = createdAt
        });
    }
}
