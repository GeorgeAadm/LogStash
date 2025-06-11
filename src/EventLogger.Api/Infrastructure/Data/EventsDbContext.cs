using EventLogger.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventLogger.Api.Infrastructure.Data
{
    public class EventsDbContext : DbContext
    {
        public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
        {
        }

        public DbSet<EventMetadata> EventMetadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventMetadata>(entity =>
            {
                entity.HasKey(e => e.EventId);
                
                entity.Property(e => e.EventId)
                    .IsRequired();
                
                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.EventType)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.Property(e => e.Timestamp)
                    .IsRequired();
                
                entity.Property(e => e.Source)
                    .HasMaxLength(100);

                entity.HasIndex(e => e.UserId)
                    .HasDatabaseName("IX_EventMetadata_UserId");
                
                entity.HasIndex(e => new { e.UserId, e.EventType })
                    .HasDatabaseName("IX_EventMetadata_UserId_EventType");
                
                entity.HasIndex(e => new { e.UserId, e.Timestamp })
                    .HasDatabaseName("IX_EventMetadata_UserId_Timestamp");
            });
        }
    }
}