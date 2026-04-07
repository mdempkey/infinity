using Infinity.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApi.Data;

public class LocationsDbContext : DbContext
{
    public LocationsDbContext(DbContextOptions<LocationsDbContext> options) : base(options) { }

    public DbSet<Park> Parks => Set<Park>();
    public DbSet<Attraction> Attractions => Set<Attraction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AttractionCategory> AttractionCategories => Set<AttractionCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Park ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Park>(e =>
        {
            e.ToTable("parks");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id").HasColumnType("uuid")
                .HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.Name).HasColumnName("name").HasColumnType("varchar(255)").IsRequired();
            e.Property(p => p.Resort).HasColumnName("resort").HasColumnType("varchar(255)");
            e.Property(p => p.City).HasColumnName("city").HasColumnType("varchar(100)");
            e.Property(p => p.Country).HasColumnName("country").HasColumnType("varchar(100)");
            e.Property(p => p.Lat).HasColumnName("lat").HasColumnType("decimal(9,6)");
            e.Property(p => p.Lng).HasColumnName("lng").HasColumnType("decimal(9,6)");
        });

        // ── Category ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id").HasColumnType("uuid")
                .HasDefaultValueSql("gen_random_uuid()");
            e.Property(c => c.Name).HasColumnName("name").HasColumnType("varchar(50)").IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.Description).HasColumnName("description").HasColumnType("text");
        });

        // ── Attraction ────────────────────────────────────────────────────────
        modelBuilder.Entity<Attraction>(e =>
        {
            e.ToTable("attractions");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").HasColumnType("uuid")
                .HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.ParkId).HasColumnName("park_id").HasColumnType("uuid").IsRequired();
            e.Property(a => a.Name).HasColumnName("name").HasColumnType("varchar(255)").IsRequired();
            e.Property(a => a.Description).HasColumnName("description").HasColumnType("text");
            e.Property(a => a.Lat).HasColumnName("lat").HasColumnType("decimal(9,6)");
            e.Property(a => a.Lng).HasColumnName("lng").HasColumnType("decimal(9,6)");
            e.Property(a => a.ImageUrls).HasColumnName("image_urls").HasColumnType("jsonb");
            e.Property(a => a.Tags).HasColumnName("tags").HasColumnType("jsonb");
            e.Property(a => a.AvgRating).HasColumnName("avg_rating").HasColumnType("decimal(3,2)")
                .HasDefaultValue(0m);
            e.Property(a => a.ReviewCount).HasColumnName("review_count").HasColumnType("integer")
                .HasDefaultValue(0);
            e.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp")
                .HasDefaultValueSql("now()");

            e.HasIndex(a => a.ParkId).HasDatabaseName("idx_attractions_park_id");

            e.HasOne(a => a.Park)
                .WithMany(p => p.Attractions)
                .HasForeignKey(a => a.ParkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AttractionCategory (junction) ──────────────────────────────────
        modelBuilder.Entity<AttractionCategory>(e =>
        {
            e.ToTable("attraction_categories");
            e.HasKey(ac => new { ac.AttractionId, ac.CategoryId });
            e.Property(ac => ac.AttractionId).HasColumnName("attraction_id");
            e.Property(ac => ac.CategoryId).HasColumnName("category_id");

            e.HasOne(ac => ac.Attraction)
                .WithMany(a => a.AttractionCategories)
                .HasForeignKey(ac => ac.AttractionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ac => ac.Category)
                .WithMany(c => c.AttractionCategories)
                .HasForeignKey(ac => ac.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
