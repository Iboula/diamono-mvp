using Diamono.Domain.Bookings;
using Diamono.Domain.Facilities;
using Diamono.Domain.Settings;
using Diamono.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class DiamonoDbContext(DbContextOptions<DiamonoDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingBlock> BookingBlocks => Set<BookingBlock>();
    public DbSet<StadiumBookingSettings> StadiumBookingSettings => Set<StadiumBookingSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Obligatoire : construit le modele Identity avant les entites metier.
        base.OnModelCreating(modelBuilder);

        // Noms alignes sur la convention snake_case deja utilisee par le schema metier.
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("identity_users");
            b.Property(x => x.DisplayName).HasMaxLength(160);
        });
        modelBuilder.Entity<ApplicationRole>(b => b.ToTable("identity_roles"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("identity_user_roles"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("identity_user_claims"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("identity_user_logins"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("identity_user_tokens"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("identity_role_claims"));

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
            b.Property(x => x.RejectionReason).HasMaxLength(500);
            b.Property(x => x.CancellationReason).HasMaxLength(500);
            b.Ignore(x => x.TotalAmount);
        });

        modelBuilder.Entity<BookingBlock>(b =>
        {
            b.ToTable("booking_blocks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.ResourceId, x.StartsAt, x.EndsAt });
            b.Property(x => x.Reason).HasMaxLength(250).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.CreatedBy).HasMaxLength(160);
            b.Property(x => x.CancelledBy).HasMaxLength(160);
            b.Ignore(x => x.IsActive);
        });

        modelBuilder.Entity<StadiumBookingSettings>(b =>
        {
            b.ToTable("stadium_booking_settings");
            b.HasKey(x => x.Id);
            b.Property(x => x.UpdatedBy).HasMaxLength(160);
        });
    }
}
