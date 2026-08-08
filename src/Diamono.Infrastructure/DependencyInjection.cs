using Diamono.Application.Abstractions;
using Diamono.Application.Administration;
using Diamono.Application.Audit;
using Diamono.Application.Availability;
using Diamono.Application.Bookings;
using Diamono.Application.Notifications;
using Diamono.Application.Payments;
using Diamono.Application.Settings;
using Diamono.Application.Reporting;
using Diamono.Domain.Pricing;
using Diamono.Infrastructure.Identity;
using Diamono.Infrastructure.Notifications;
using Diamono.Infrastructure.Payments;
using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Diamono.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDiamonoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["DIAMONO_CONNECTION"]
            ?? configuration.GetConnectionString("Diamono")
            ?? throw new InvalidOperationException("Connection string 'Diamono' is missing.");

        services.AddDbContext<DiamonoDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IBookingBlockRepository, BookingBlockRepository>();
        services.AddScoped<IStadiumBookingSettingsRepository, StadiumBookingSettingsRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
        services.AddScoped<IReportingRepository, ReportingRepository>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditReader, AuditReader>();
        services.AddScoped<INotificationProvider, DevelopmentNotificationProvider>();
        services.AddScoped<INotificationService, DevelopmentNotificationService>();
        services.AddScoped<INotificationReader, NotificationReader>();
        services.AddScoped<IPaymentProvider, ManualPaymentProvider>();
        services.AddScoped<IPaymentReceiptRenderer, QuestPdfPaymentReceiptRenderer>();
        services.AddScoped<IPaymentReceiptService, PaymentReceiptService>();
        services.AddScoped<BookingApplicationService>();
        services.AddScoped<PaymentApplicationService>();
        services.AddScoped<BookingBlockApplicationService>();
        services.AddScoped<StadiumSettingsApplicationService>();
        services.AddScoped<AvailabilityApplicationService>();
        services.AddScoped<ReportingApplicationService>();
        services.AddSingleton<MvpPricingPolicy>();
        return services;
    }
}
