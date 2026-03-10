using HelpDeskHero.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskHero.Api.Infrastructure.Persistence;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(b =>
        {
            b.ToTable("Tickets");
            b.HasKey(x => x.Id);

            b.Property(x => x.Number).HasMaxLength(30).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.Priority).HasMaxLength(30).IsRequired();

            b.Property(x => x.RowVersion)
                .IsRowVersion();

            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasKey(x => x.Id);

            b.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
            b.Property(x => x.DeviceName).HasMaxLength(200).IsRequired();
            b.Property(x => x.IpAddress).HasMaxLength(64);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLogs");
            b.HasKey(x => x.Id);

            b.Property(x => x.Action).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            b.Property(x => x.UserName).HasMaxLength(256);
            b.Property(x => x.IpAddress).HasMaxLength(64);
        });
    }
}