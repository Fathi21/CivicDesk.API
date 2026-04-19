using Microsoft.EntityFrameworkCore;
using CivicDesk.API.Models;

namespace CivicDesk.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ReferenceNumber).IsUnique();
            e.Property(x => x.ReferenceNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(100).IsRequired();
            e.Property(x => x.AddressOrLocation).HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AdminNotes).HasMaxLength(2000);
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SessionId);
            e.Property(x => x.SessionId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.Property(x => x.Content).HasMaxLength(4000).IsRequired();
        });
    }
}