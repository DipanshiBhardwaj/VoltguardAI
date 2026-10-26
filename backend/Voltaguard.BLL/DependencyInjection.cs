using Microsoft.Extensions.DependencyInjection;
using Voltaguard.BLL.Security;
using Voltaguard.BLL.Services;
using Voltaguard.DAL.Repositories;

namespace Voltaguard.BLL;

/// <summary>Registers the business services and the repositories they depend on.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IMeterRepository, MeterRepository>();
        services.AddScoped<ITelemetryRepository, TelemetryRepository>();
        services.AddScoped<IAuditReportRepository, AuditReportRepository>();
        services.AddScoped<ITariffRepository, TariffRepository>();

        services.AddScoped<IMeterService, MeterService>();
        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IAuditReportService, AuditReportService>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
