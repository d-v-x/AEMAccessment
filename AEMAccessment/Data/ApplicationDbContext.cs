using AEMAccessment.Models;
using Microsoft.EntityFrameworkCore;

namespace AEMAccessment.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Platform> Platforms { get; set; }
        public DbSet<Well> Wells { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Platform>(entity =>
            {
                entity.ToTable("Platforms");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).ValueGeneratedNever();
                entity.Property(p => p.UniqueName).HasMaxLength(255);
                entity.Property(p => p.Latitude);
                entity.Property(p => p.Longitude);
            });

            modelBuilder.Entity<Well>(entity =>
            {
                entity.ToTable("Wells");
                entity.HasKey(w => w.Id);
                entity.Property(w => w.Id).ValueGeneratedNever();
                entity.Property(w => w.UniqueName).HasMaxLength(255);
                entity.Property(w => w.Latitude);
                entity.Property(w => w.Longitude);
                entity.HasOne(w => w.Platform)
                      .WithMany(p => p.Wells)
                      .HasForeignKey(w => w.PlatformId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}