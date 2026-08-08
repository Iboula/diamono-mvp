using Diamono.Application.Abstractions;
using Diamono.Application.Availability;
using Diamono.Application.Bookings;
using Diamono.Application.Settings;
using Diamono.Domain.Pricing;
using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Diamono.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDiamonoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Diamono")
            ?? throw new InvalidOperationException("Connection string 'Diamono' is missing.");

        services.AddDbContext<DiamonoDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingBlockRepository, BookingBlockRepository>();
        services.AddScoped<IStadiumBookingSettingsRepository, StadiumBookingSettingsRepository>();
        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<BookingApplicationService>();
        services.AddScoped<BookingBlockApplicationService>();
        services.AddScoped<StadiumSettingsApplicationService>();
        services.AddScoped<AvailabilityApplicationService>();
        services.AddSingleton<MvpPricingPolicy>();
        return services;
    }
}
