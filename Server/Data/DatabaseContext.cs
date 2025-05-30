using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles { get; set; }
    public DbSet<Privacy> Privacies { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<OTPs> OTPs { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Contest> Contests { get; set; }
    public DbSet<ContestEntry> ContestEntries { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Image> Images { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure User entity
        modelBuilder.Entity<User>()
            .Property(u => u.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<User>()
            .Property(u => u.Verified)
            .HasDefaultValue(false);

        // Configure User relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.Videos)
            .WithOne(v => v.User)
            .HasForeignKey(v => v.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Comments)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Likes)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Video>()
            .HasMany(v => v.Likes)
            .WithOne(l => l.Video)
            .HasForeignKey(l => l.VideoID);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ContestEntries)
            .WithOne(ce => ce.User)
            .HasForeignKey(ce => ce.UserID);

        modelBuilder.Entity<Video>()
            .HasMany(v => v.ContestEntries)
            .WithOne(ce => ce.Video)
            .HasForeignKey(ce => ce.VideoID);

        modelBuilder.Entity<Contest>()
            .HasMany(c => c.ContestEntries)
            .WithOne(ce => ce.Contest)
            .HasForeignKey(ce => ce.ContestID);

        modelBuilder.Entity<User>()
            .HasMany(u => u.SentMessages)
            .WithOne(m => m.Sender)
            .HasForeignKey(m => m.SenderID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasMany(u => u.ReceivedMessages)
            .WithOne(m => m.Receiver)
            .HasForeignKey(m => m.ReceiverID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.Videos)
            .WithOne(v => v.Category)
            .HasForeignKey(v => v.CategoryID);

        modelBuilder.Entity<Video>()
            .HasMany(v => v.Comments)
            .WithOne(c => c.Video)
            .HasForeignKey(c => c.VideoID);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Video)
            .WithMany(v => v.Comments)
            .HasForeignKey(c => c.VideoID);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.Video)
            .WithMany(v => v.Likes)
            .HasForeignKey(l => l.VideoID);

        modelBuilder.Entity<ContestEntry>()
            .HasOne(ce => ce.Video)
            .WithMany(v => v.ContestEntries)
            .HasForeignKey(ce => ce.VideoID);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Privacy)
            .WithMany(pr => pr.Posts)
            .HasForeignKey(p => p.PrivacyID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Post>()
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Post>()
            .HasMany(p => p.Likes)
            .WithOne(l => l.Post)
            .HasForeignKey(l => l.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Like>()
            .HasKey(l => l.LikeID);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserID);

        modelBuilder.Entity<Comment>()
            .HasIndex(c => c.CreatedAt);

        modelBuilder.Entity<Comment>()
            .HasIndex(c => c.PostID);

        modelBuilder.Entity<Comment>()
            .HasIndex(c => c.VideoID);

        modelBuilder.Entity<Like>()
            .HasIndex(l => l.CreatedAt);

        modelBuilder.Entity<Like>()
            .HasIndex(l => l.PostID);

        modelBuilder.Entity<Like>()
            .HasIndex(l => l.VideoID);

        // Create a unique constraint for likes to prevent duplicates
        // PostgreSQL uses a different syntax for filtered indexes
        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.UserID, l.PostID, l.VideoID })
            .IsUnique()
            .HasFilter("\"PostID\" IS NOT NULL OR \"VideoID\" IS NOT NULL");

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Users)
            .WithOne(u => u.Role)
            .HasForeignKey(u => u.RoleID);

        // Configure the one-to-many relationship between Privacy and Video
        modelBuilder.Entity<Privacy>()
            .HasMany(p => p.Videos)
            .WithOne(v => v.Privacy)
            .HasForeignKey(v => v.PrivacyID);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.ContestEntry)
            .WithMany()
            .HasForeignKey(v => v.EntryID);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserID);

        // Configure ForgetPasswordOTP
        modelBuilder.Entity<OTPs>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OTPs>()
            .HasIndex(f => f.Email);

        // Configure User-Image relationship
        modelBuilder.Entity<Image>()
            .HasOne(i => i.User)
            .WithMany(u => u.Images)
            .HasForeignKey(i => i.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Privacy-Image relationship
        modelBuilder.Entity<Image>()
            .HasOne(i => i.Privacy)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PrivacyID)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Category-Image relationship
        modelBuilder.Entity<Image>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Images)
            .HasForeignKey(i => i.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Privacy-Video relationship
        modelBuilder.Entity<Video>()
            .HasOne(v => v.Privacy)
            .WithMany(p => p.Videos)
            .HasForeignKey(v => v.PrivacyID)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Category-Video relationship
        modelBuilder.Entity<Video>()
            .HasOne(v => v.Category)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Privacy-Post relationship
        modelBuilder.Entity<Post>()
            .HasOne(p => p.Privacy)
            .WithMany(pr => pr.Posts)
            .HasForeignKey(p => p.PrivacyID)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Category-Post relationship
        modelBuilder.Entity<Post>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Post-Image relationship
        modelBuilder.Entity<Post>()
            .HasMany(p => p.Images)
            .WithOne(i => i.Post)
            .HasForeignKey(i => i.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Post-Video relationship
        modelBuilder.Entity<Post>()
            .HasMany(p => p.Videos)
            .WithOne(v => v.Post)
            .HasForeignKey(v => v.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                CategoryID = 1, CategoryName = "Dance",
                Description = "Posts about dance performances, tutorials, and events"
            },
            new Category
            {
                CategoryID = 2, CategoryName = "Music",
                Description = "Posts about music performances, covers, and events"
            },
            new Category
            {
                CategoryID = 3, CategoryName = "Art",
                Description = "Posts about visual arts, exhibitions, and creative works"
            },
            new Category
            {
                CategoryID = 4, CategoryName = "Photography",
                Description = "Posts about photography, photo shoots, and visual stories"
            },
            new Category
            {
                CategoryID = 5, CategoryName = "Fashion",
                Description = "Posts about fashion shows, style tips, and trends"
            },
            new Category
            {
                CategoryID = 6, CategoryName = "Theater",
                Description = "Posts about theater performances, acting, and drama"
            },
            new Category
            {
                CategoryID = 7, CategoryName = "Comedy", Description = "Posts about comedy shows, stand-up, and humor"
            },
            new Category
            {
                CategoryID = 8, CategoryName = "Poetry",
                Description = "Posts about poetry readings, spoken word, and literary works"
            },
            new Category
            {
                CategoryID = 9, CategoryName = "Film", Description = "Posts about filmmaking, short films, and cinema"
            },
            new Category
                { CategoryID = 10, CategoryName = "Other", Description = "Other creative content and performances" }
        );

        // Seed Privacy Settings
        modelBuilder.Entity<Privacy>().HasData(
            new Privacy { PrivacyID = 1, Name = "Public" },
            new Privacy { PrivacyID = 2, Name = "Friends Only" },
            new Privacy { PrivacyID = 3, Name = "Private" }
        );

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, Name = "User" },
            new Role { RoleID = 2, Name = "Admin" }
        );
    }
}