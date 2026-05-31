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

        modelBuilder.Entity<WorksCreators>()//這是WorksCreators的複合主鍵
        .HasKey(x => new
        {
            x.WorkId,
            x.CreatorId
        });

        modelBuilder.Entity<WorkFeatures>()//這是WorkFeatures的外鍵關聯
            .HasOne<Works>()
            .WithMany()
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkMedia>()//這是WorkMedia的外鍵關聯
            .HasOne<Works>()
            .WithMany()
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorksCreators>()//這是WorksCreators的外鍵關聯
            .HasOne<Works>()
            .WithMany()
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorksCreators>()//這是WorksCreators的外鍵關聯
            .HasOne<CreatorProfiles>()
            .WithMany()
            .HasForeignKey(x => x.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}