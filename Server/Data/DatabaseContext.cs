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
    public DbSet<Vote> Votes { get; set; } //anh

    public DbSet<Notification> Notifications { get; set; } // Phat
    public DbSet<Share> Shares { get; set; } // Phat
    public DbSet<UserConversation> UserConversations { get; set; }
    public DbSet<Conversation> Conversations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
     //HasData seeding for roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, Name = "User" },
            new Role { RoleID = 2, Name = "Admin" }
        );
        // Configure relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.Videos)
            .WithOne(v => v.User)
            .HasForeignKey(v => v.UserID);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Comments)
            .WithOne(c => c.User)
            .HasForeignKey(c => c.UserID);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Likes)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserID);

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
        

        modelBuilder.Entity<Category>()
            .HasMany(c => c.Videos)
            .WithOne(v => v.Category)
            .HasForeignKey(v => v.CategoryID);

        modelBuilder.Entity<Video>()
            .HasMany(v => v.Comments)
            .WithOne(c => c.Video)
            .HasForeignKey(c => c.VideoID);

        // Many-to-many relationship between User and Video via Likes is handled by the Likes entity

        // Configure the many-to-many relationship between User and Video through the Like entity
        modelBuilder.Entity<Like>()
            .HasKey(l => l.LikeID); // Assuming LikeID is the primary key

        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserID);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.Video)
            .WithMany(v => v.Likes)
            .HasForeignKey(l => l.VideoID);

        // Configure the one-to-many relationship between Role and User
        modelBuilder.Entity<Role>()
            .HasMany(r => r.Users)
            .WithOne(u => u.Role)
            .HasForeignKey(u => u.RoleID);

        // Configure the one-to-many relationship between Privacy and Video
        modelBuilder.Entity<Privacy>()
            .HasMany(p => p.Videos)
            .WithOne(v => v.Privacy)
            .HasForeignKey(v => v.PrivacyID);

        modelBuilder.Entity<Vote>() //anh
            .HasOne(v => v.ContestEntry)
            .WithMany()
            .HasForeignKey(v => v.EntryID);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserID);

        // Configure ForgetPasswordOTPs
        modelBuilder.Entity<OTPs>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OTPs>()
            .HasIndex(f => f.Email);  // Index for faster email lookups
        
        modelBuilder.Entity<Message>()
            .HasKey(m => m.Id); // Đảm bảo EF hiểu rõ khoá chính

        modelBuilder.Entity<Message>()
            .HasIndex(m => m.ConversationId);

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ConversationId, m.SentAt });

        
        modelBuilder.Entity<UserConversation>()
            .HasKey(uc => new { uc.UserId, uc.ConversationId }); 

        
        // Quan hệ Conversation - UserConversation
        modelBuilder.Entity<UserConversation>()
            .HasOne(uc => uc.Conversation)
            .WithMany(c => c.Participants)
            .HasForeignKey(uc => uc.ConversationId);

        modelBuilder.Entity<UserConversation>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.Conversations)
            .HasForeignKey(uc => uc.UserId);

        // Quan hệ Conversation - Message
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .HasPrincipalKey(u => u.UserID);
    }
}
