using Microsoft.EntityFrameworkCore;
using StripeIngest.Api.Domain.Entities;

namespace StripeIngest.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RawEvent> RawEvents { get; set; }
    public DbSet<CurrentSubscription> CurrentSubscriptions { get; set; }
    public DbSet<SubscriptionHistory> SubscriptionHistory { get; set; }
    public DbSet<MonthlyMrrReport> MonthlyMrrReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // RawEvent
        modelBuilder.Entity<RawEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(50);
            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.ProcessedAt);
        });

        // CurrentSubscription
        modelBuilder.Entity<CurrentSubscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.SubscriptionId).HasMaxLength(50);
            entity.Property(e => e.CustomerId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CurrentProduct).HasMaxLength(50);
            entity.Property(e => e.CurrentPrice).HasMaxLength(50);
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.LastEventId).HasMaxLength(50);
        });

        // SubscriptionHistory
        modelBuilder.Entity<SubscriptionHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubscriptionId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EventId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ChangeType).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.SubscriptionId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Report View
        modelBuilder.Entity<MonthlyMrrReport>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("MonthlyMrrReport");
        });
    }
}
