using Microsoft.EntityFrameworkCore;
using PCBuilder.Models;

namespace PCBuilder.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<UserBuild> UserBuilds { get; set; }
        public DbSet<Build> Builds { get; set; }
        public DbSet<BuildItem> BuildItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<UserBuild>()
                .HasOne(ub => ub.User)
                .WithMany(u => u.UserBuilds)
                .HasForeignKey(ub => ub.UserId);

            modelBuilder.Entity<UserBuild>()
                .HasOne(ub => ub.Product)
                .WithMany(p => p.UserBuilds)
                .HasForeignKey(ub => ub.ProductId);

            modelBuilder.Entity<Build>()
                .HasOne(b => b.User)
                .WithMany(u => u.Builds)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BuildItem>()
                .HasOne(bi => bi.Build)
                .WithMany(b => b.BuildItems)
                .HasForeignKey(bi => bi.BuildId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BuildItem>()
                .HasOne(bi => bi.Product)
                .WithMany()
                .HasForeignKey(bi => bi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Set table names to match your SQL
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Product>().ToTable("products");
            modelBuilder.Entity<Category>().ToTable("categories");
            modelBuilder.Entity<UserBuild>().ToTable("user_builds");
        }
    }
}