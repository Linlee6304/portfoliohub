using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PortfolioHub.Server.Models.Entities;

namespace PortfolioHub.Server.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Works> Works { get; set; }

    public DbSet<CreatorProfiles> CreatorProfiles { get; set; }

    public DbSet<WorkFeatures> WorkFeatures { get; set; }

    public DbSet<WorkMedia> WorkMedia { get; set; }

    public DbSet<WorksCreators> WorksCreators { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CreatorProfiles>()
            .HasKey(x => x.CreatorId);

        modelBuilder.Entity<Works>()
            .HasKey(x => x.WorkId);

        modelBuilder.Entity<WorkFeatures>()
            .HasKey(x => x.FeatureId);

        modelBuilder.Entity<WorkMedia>()
            .HasKey(x => x.MediaId);

        modelBuilder.Entity<WorksCreators>()
            .HasKey(x => new
            {
                x.WorkId,
                x.CreatorId
            });


        modelBuilder.Entity<CreatorProfiles>()
        .HasOne(cp => cp.IdentityUser)
        .WithOne(u => u.CreatorProfile)
        .HasForeignKey<CreatorProfiles>(cp => cp.IdentityUserId);


        // Works 1 對多 WorkFeatures
        modelBuilder.Entity<WorkFeatures>()
            .HasOne(x => x.Work)
            .WithMany(x => x.WorkFeatures)
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        // Works 1 對多 WorkMedia
        modelBuilder.Entity<WorkMedia>()
            .HasOne(x => x.Work)
            .WithMany(x => x.WorkMedia)
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        // Works 1 對多 WorksCreators
        modelBuilder.Entity<WorksCreators>()
            .HasOne(x => x.Work)
            .WithMany(x => x.WorksCreators)
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        // CreatorProfiles 1 對多 WorksCreators
        modelBuilder.Entity<WorksCreators>()
            .HasOne(x => x.CreatorProfile)
            .WithMany(x => x.WorksCreators)
            .HasForeignKey(x => x.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}