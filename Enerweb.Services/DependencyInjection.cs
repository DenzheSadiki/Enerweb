using Enerweb.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Enerweb.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddEnerwebServices(this IServiceCollection services)
    {
        services.AddScoped<ISiteService, SiteService>();
        services.AddScoped<IMeterService, MeterService>();
        services.AddScoped<IReadingService, ReadingService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IContactPersonService, ContactPersonService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ILogService, LogService>();
        return services;
    }
}
