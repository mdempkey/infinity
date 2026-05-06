using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id").ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Rating>(e =>
        {
            e.ToTable("ratings");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
            e.Property(r => r.AttractionId).HasColumnName("attraction_id").IsRequired();
            e.Property(r => r.Value).HasColumnName("value").IsRequired();
            e.HasIndex(r => new { r.UserId, r.AttractionId }).IsUnique();
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.ToTable("reviews");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
            e.Property(r => r.AttractionId).HasColumnName("attraction_id").IsRequired();
            e.Property(r => r.Content).HasColumnName("content").HasMaxLength(2000).IsRequired();
            e.Property(r => r.ModifiedAt).HasColumnName("modified_at").HasColumnType("timestamp with time zone").IsRequired();
            e.HasIndex(r => r.AttractionId);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
