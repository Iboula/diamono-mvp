using Diamono.Domain.Bookings;
using Diamono.Domain.Facilities;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class DiamonoDbContext(DbContextOptions<DiamonoDbContext> options) : DbContext(options)
{
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingBlock> BookingBlocks => Set<BookingBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resource>(b =>
        {
            b.ToTable("resources");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        });

        modelBuilder.Entity<Booking>(b =>
        {
            b.ToTable("bookings");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Reference).IsUnique();
            b.HasIndex(x => new { x.ResourceId, x.StartsAt, x.EndsAt });
            b.Property(x => x.Reference).HasMaxLength(32).IsRequired();
            b.Property(x => x.CustomerName).HasMaxLength(160).IsRequired();
            b.Property(x => x.Phone).HasMaxLength(40).IsRequired();
            b.Property(x => x.ActivityType).HasMaxLength(100).IsRequired();
            b.Ignore(x => x.TotalAmount);
        });

        modelBuilder.Entity<BookingBlock>(b =>
        {
            b.ToTable("booking_blocks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ResourceId, x.StartsAt, x.EndsAt });
            b.Property(x => x.Reason).HasMaxLength(250).IsRequired();
        });
    }
}
