using System;
using Enerweb.Domain.Models;
using Enerweb.Data.Repositories;
using Enerweb.Interfaces;
using Enerweb.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Enerweb.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddEnerwebData(this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<EnerwebDbContext>(configureDatabase);
        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<IBaseRepository<Site>>(provider => provider.GetRequiredService<ISiteRepository>());
        services.AddScoped<IMeterRepository, MeterRepository>();
        services.AddScoped<IBaseRepository<Meters>>(provider => provider.GetRequiredService<IMeterRepository>());
        services.AddScoped<IReadingRepository, ReadingRepository>();
        services.AddScoped<IBaseRepository<Reading>>(provider => provider.GetRequiredService<IReadingRepository>());
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IBaseRepository<Address>>(provider => provider.GetRequiredService<IAddressRepository>());
        services.AddScoped<IContactPersonRepository, ContactPersonRepository>();
        services.AddScoped<IBaseRepository<ContactPerson>>(provider => provider.GetRequiredService<IContactPersonRepository>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IErrorRepository, ErrorRepository>();
        services.AddScoped<IDatabaseLogRepository, DatabaseLogRepository>();
        services.AddScoped<IBaseRepository<Error>>(provider => provider.GetRequiredService<IErrorRepository>());
        services.AddScoped<IBaseRepository<DatabaseLog>>(provider => provider.GetRequiredService<IDatabaseLogRepository>());
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IBaseRepository<ReportRequest>>(provider => provider.GetRequiredService<IReportRepository>());
        return services;
    }
}
