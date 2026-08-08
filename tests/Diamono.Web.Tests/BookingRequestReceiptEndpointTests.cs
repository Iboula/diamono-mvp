using System.Net;
using Diamono.Domain.Bookings;
using Diamono.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class BookingRequestReceiptEndpointTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd126");

    [Fact]
    public async Task Token_public_valide_retourne_pdf()
    {
        await using var factory = new DiamonoWebApplicationFactory();
        var booking = await SeedBookingAsync(factory);
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync($"/reservation/recu-provisoire/{booking.PublicAccessToken}");
        var content = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(content, 0, 4));
    }

    [Fact]
    public async Task Token_public_invalide_retourne_404_generique()
    {
        await using var factory = new DiamonoWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync("/reservation/recu-provisoire/token-invalide");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Booking> SeedBookingAsync(DiamonoWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiamonoDbContext>();
        var booking = new Booking(
            ResourceId,
            new DateTimeOffset(2026, 8, 20, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 20, 20, 0, 0, TimeSpan.Zero),
            "Awa Diop",
            "+221 77 000 00 00",
            CustomerCategory.Individual,
            "Football",
            50_000m,
            5_000m,
            25_000m);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private sealed class DiamonoWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string databaseName = $"diamono-receipt-endpoint-{Guid.NewGuid()}";

        public DiamonoWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable(
                "DIAMONO_CONNECTION",
                "Host=localhost;Database=diamono_test;Username=test;Password=test");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DiamonoDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<DiamonoDbContext>>();
                services.AddDbContext<DiamonoDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));
            });
        }
    }
}
