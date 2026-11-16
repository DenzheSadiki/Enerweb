namespace Enerweb.Domain.Models
{
    public class Meters
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }

        // Meter identification
        public string MeterNumber { get; set; }
        public string SerialNumber { get; set; }
        public string MeterName { get; set; }

        // Meter classification
        public string MeterType { get; set; } // Electricity, Water, Gas
        public string MeterCategory { get; set; } // Prepaid, Postpaid
        public string UnitOfMeasurement { get; set; } // kWh, m³, Litres

        // Meter readings
        public decimal CurrentReading { get; set; }
        public decimal PreviousReading { get; set; }
        public decimal Consumption { get; set; }

        // Installation details
        public DateTime? InstallationDate { get; set; }
        public DateTime? LastReadingDate { get; set; }

        // Location / ownership
        public string PropertyId { get; set; }
        public string AccountNumber { get; set; }

        // Status
        public bool IsActive { get; set; }
        public string Status { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
