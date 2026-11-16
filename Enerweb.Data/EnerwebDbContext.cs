using Enerweb.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Enerweb.Data;

public class EnerwebDbContext : DbContext
{
    public EnerwebDbContext(DbContextOptions<EnerwebDbContext> options) : base(options) { }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Meters> Meters => Set<Meters>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<ContactPerson> ContactPeople => Set<ContactPerson>();
    public DbSet<ReportRequest> Reports => Set<ReportRequest>();
    public DbSet<Error> Errors => Set<Error>();
    public DbSet<DatabaseLog> DatabaseLogs => Set<DatabaseLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<System.DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DatabaseLog>().HasOne<Error>().WithMany()
            .HasForeignKey(log => log.ErrorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DatabaseLog>().HasIndex(log => log.Timestamp);
        modelBuilder.Entity<DatabaseLog>().HasIndex(log => log.TraceId);
        modelBuilder.Entity<DatabaseLog>().Property(log => log.TraceId).HasMaxLength(200);
        modelBuilder.Entity<ReportRequest>().Property(report => report.Name).HasMaxLength(200);
        modelBuilder.Entity<ReportRequest>().Property(report => report.Entity).HasMaxLength(100);

        modelBuilder.Entity<Address>().HasKey(address => address.id);
        modelBuilder.Entity<Site>().HasIndex(site => site.SiteCode).IsUnique();
        modelBuilder.Entity<Site>().Property(site => site.SiteCode).HasMaxLength(50);
        modelBuilder.Entity<Meters>().HasIndex(meter => meter.MeterNumber).IsUnique();
        modelBuilder.Entity<Meters>().Property(meter => meter.MeterNumber).HasMaxLength(100);
        modelBuilder.Entity<Site>().HasMany(site => site.Meters).WithOne()
            .HasForeignKey(meter => meter.SiteId).IsRequired().OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reading>().HasOne(reading => reading.Meter).WithMany()
            .HasForeignKey(reading => reading.MeterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reading>().HasIndex(reading => new { reading.MeterId, reading.Timestamp }).IsUnique();

        modelBuilder.Entity<Meters>().Property(meter => meter.CurrentReading).HasPrecision(18, 4);
        modelBuilder.Entity<Meters>().Property(meter => meter.PreviousReading).HasPrecision(18, 4);
        modelBuilder.Entity<Meters>().Property(meter => meter.Consumption).HasPrecision(18, 4);
        modelBuilder.Entity<Reading>().Property(reading => reading.Value).HasPrecision(18, 4);

        DatabaseSeed.Configure(modelBuilder);
    }
}
