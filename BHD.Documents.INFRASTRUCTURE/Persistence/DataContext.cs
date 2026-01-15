using Domain.Entities;
namespace Infraestructure.DbContext;

using Microsoft.EntityFrameworkCore;

public class DataContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {

    }

    public DbSet<DocumentAsset> Documents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentAsset>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Filename).IsRequired().HasMaxLength(500);
            entity.Property(d => d.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(d => d.DocumentType).IsRequired();
            entity.Property(d => d.Channel).IsRequired();
            entity.Property(d => d.DocumentStatus).IsRequired();
            entity.Property(d => d.UploadDate).IsRequired();
            entity.Property(d => d.CustomerId).HasMaxLength(100);
            entity.Property(d => d.CorrelationId).HasMaxLength(100);
            entity.Property(d => d.Url).HasMaxLength(1000);
            entity.Property(d => d.EncodedFile)
                .IsRequired(false);
            entity.HasIndex(d => d.UploadDate);
            entity.HasIndex(d => d.DocumentStatus);
            entity.HasIndex(d => d.CustomerId);
        });
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.PasswordHash)
                .IsRequired();

            entity.HasMany(e => e.RefreshTokens)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(256);
        });

        base.OnModelCreating(modelBuilder);

    }
}

   
