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
    public DbSet<Community> Communities { get; set; }
    public DbSet<CommunityMember> CommunityMembers { get; set; }
    public DbSet<TalentOpportunity> TalentOpportunities { get; set; }
    public DbSet<OpportunityApplication> OpportunityApplications { get; set; }
    public DbSet<Notification> Notifications { get; set; }

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

        // Configure Vote entity
        modelBuilder.Entity<Vote>()
            .HasOne(v => v.ContestEntry)
            .WithMany()
            .HasForeignKey(v => v.EntryID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Vote>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Video)
            .WithMany(v => v.Comments)
            .HasForeignKey(c => c.VideoID);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.Video)
            .WithMany(v => v.Likes)
            .HasForeignKey(l => l.VideoID);

        // ContestEntry configuration
        modelBuilder.Entity<ContestEntry>(entity =>
        {
            entity.HasKey(e => e.EntryID);
            entity.Property(e => e.SubmissionDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Contest relationship
            entity.HasOne(e => e.Contest)
                .WithMany(c => c.ContestEntries)
                .HasForeignKey(e => e.ContestID)
                .OnDelete(DeleteBehavior.Cascade);

            // User relationship
            entity.HasOne(e => e.User)
                .WithMany(u => u.ContestEntries)
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            // Video relationship (optional)
            entity.HasOne(e => e.Video)
                .WithMany(v => v.ContestEntries)
                .HasForeignKey(e => e.VideoID)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Image relationship (optional)
            entity.HasOne(e => e.Image)
                .WithMany()
                .HasForeignKey(e => e.ImageID)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Check constraint to ensure only one of VideoID or ImageID is set
            entity.HasCheckConstraint(
                "CK_ContestEntry_MediaCheck",
                "(\"VideoID\" IS NOT NULL AND \"ImageID\" IS NULL) OR (\"VideoID\" IS NULL AND \"ImageID\" IS NOT NULL)");

            // Indexes
            entity.HasIndex(e => e.VideoID);
            entity.HasIndex(e => e.ImageID);
            entity.HasIndex(e => e.ContestID);
            entity.HasIndex(e => e.UserID);
        });

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

        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.UserID, l.PostID, l.VideoID })
            .IsUnique()
            .HasFilter("\"PostID\" IS NOT NULL OR \"VideoID\" IS NOT NULL");

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Users)
            .WithOne(u => u.Role)
            .HasForeignKey(u => u.RoleID);

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

        modelBuilder.Entity<OTPs>()
            .HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OTPs>()
            .HasIndex(f => f.Email);

        modelBuilder.Entity<Image>()
            .HasOne(i => i.User)
            .WithMany(u => u.Images)
            .HasForeignKey(i => i.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Image>()
            .HasOne(i => i.Privacy)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PrivacyID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Image>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Images)
            .HasForeignKey(i => i.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Video>()
            .HasOne(v => v.Privacy)
            .WithMany(p => p.Videos)
            .HasForeignKey(v => v.PrivacyID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Video>()
            .HasOne(v => v.Category)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Privacy)
            .WithMany(pr => pr.Posts)
            .HasForeignKey(p => p.PrivacyID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TalentOpportunity>()
            .HasOne(to => to.PostedByUser)
            .WithMany(u => u.PostedOpportunities)
            .HasForeignKey(to => to.PostedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TalentOpportunity>()
            .HasOne(to => to.Category)
            .WithMany(c => c.TalentOpportunities)
            .HasForeignKey(to => to.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add index for better query performance
        modelBuilder.Entity<TalentOpportunity>()
            .HasIndex(to => to.TalentArea);
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.TalentArea);

        modelBuilder.Entity<Community>()
            .HasOne(c => c.CreatedBy)
            .WithMany(u => u.CreatedCommunities)
            .HasForeignKey(c => c.CreatedByUserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Community>()
            .HasOne(c => c.Privacy)
            .WithMany()
            .HasForeignKey(c => c.PrivacyID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CommunityMember>()
            .HasKey(cm => new { cm.UserID, cm.CommunityID });

        modelBuilder.Entity<CommunityMember>()
            .HasOne(cm => cm.User)
            .WithMany(u => u.CommunityMemberships)
            .HasForeignKey(cm => cm.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommunityMember>()
            .HasOne(cm => cm.Community)
            .WithMany(c => c.Members)
            .HasForeignKey(cm => cm.CommunityID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Community)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CommunityID)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Post>()
            .HasMany(p => p.Images)
            .WithOne(i => i.Post)
            .HasForeignKey(i => i.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Post>()
            .HasMany(p => p.Videos)
            .WithOne(v => v.Post)
            .HasForeignKey(v => v.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OpportunityApplication>()
            .HasKey(oa => oa.ApplicationID);

        modelBuilder.Entity<OpportunityApplication>()
            .HasOne(oa => oa.User)
            .WithMany(u => u.OpportunityApplications)
            .HasForeignKey(oa => oa.UserID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OpportunityApplication>()
            .HasOne(oa => oa.TalentOpportunity)
            .WithMany(to => to.Applications)
            .HasForeignKey(oa => oa.TalentOpportunityID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasKey(n => n.NotificationID);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserID)
            .OnDelete(DeleteBehavior.Cascade);

        // First, ensure we have the necessary users
        modelBuilder.Entity<User>().HasData(
            // Admin user (ID: 1)
            new User 
            { 
                UserID = 1, 
                Username = "admin",
                Email = "admin@example.com",
                FullName = "Admin User",
                RoleID = 2, // Admin role
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Verified = true,
                TalentArea = "Administration"
            },
            // Talent scout/recruiter (ID: 2)
            new User 
            { 
                UserID = 2, 
                Username = "recruiter1",
                Email = "recruiter@example.com",
                FullName = "Sarah Johnson",
                RoleID = 1,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Verified = true,
                TalentArea = "Talent Acquisition"
            },
            // Talent user 1 (ID: 3)
            new User 
            { 
                UserID = 3, 
                Username = "artist1",
                Email = "artist1@example.com",
                FullName = "Alex Chen",
                RoleID = 1,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Verified = true,
                TalentArea = "Graphic Design"
            },
            // Talent user 2 (ID: 4)
            new User 
            { 
                UserID = 4, 
                Username = "dancer1",
                Email = "dancer1@example.com",
                FullName = "Jamal Williams",
                RoleID = 1,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Verified = true,
                TalentArea = "Hip Hop Dance"
            },
            // Talent user 3 (ID: 5)
            new User 
            { 
                UserID = 5, 
                Username = "creator1",
                Email = "creator@example.com",
                FullName = "Taylor Smith",
                RoleID = 1,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Verified = true,
                TalentArea = "Content Creation"
            }
        );

        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { CategoryID = 1, CategoryName = "Dance", Description = "Posts about dance performances, tutorials, and events" },
            new Category { CategoryID = 2, CategoryName = "Music", Description = "Posts about music performances, covers, and events" },
            new Category { CategoryID = 3, CategoryName = "Art", Description = "Posts about visual arts, exhibitions, and creative works" },
            new Category { CategoryID = 4, CategoryName = "Photography", Description = "Posts about photography, photo shoots, and visual stories" },
            new Category { CategoryID = 5, CategoryName = "Fashion", Description = "Posts about fashion shows, style tips, and trends" },
            new Category { CategoryID = 6, CategoryName = "Theater", Description = "Posts about theater performances, acting, and drama" },
            new Category { CategoryID = 7, CategoryName = "Comedy", Description = "Posts about comedy shows, stand-up, and humor" },
            new Category { CategoryID = 8, CategoryName = "Poetry", Description = "Posts about poetry readings, spoken word, and literary works" },
            new Category { CategoryID = 9, CategoryName = "Film", Description = "Posts about filmmaking, short films, and cinema" },
            new Category { CategoryID = 10, CategoryName = "Other", Description = "Other creative content and performances" }
        );

        // Seed Privacy Settings
        modelBuilder.Entity<Privacy>().HasData(
            new Privacy { PrivacyID = 1, Name = "Public" },
            new Privacy { PrivacyID = 2, Name = "Friends Only" },
            new Privacy { PrivacyID = 3, Name = "Private" }
        );

        // Seed Contests
        modelBuilder.Entity<Contest>().HasData(
    new Contest
    {
        ContestID = 1,
        Title = "Summer Photography Challenge",
        Description = "Capture the essence of summer in your best photos. Open to all photography enthusiasts!",
        StartDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2025, 8, 31, 23, 59, 59, DateTimeKind.Utc)
    },
    new Contest
    {
        ContestID = 2,
        Title = "Short Film Festival",
        Description = "Submit your best short films under 5 minutes. All genres welcome!",
        StartDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2025, 9, 30, 23, 59, 59, DateTimeKind.Utc)
    },
    new Contest
    {
        ContestID = 3,
        Title = "Urban Art Competition",
        Description = "Showcase your street art and urban photography skills.",
        StartDate = new DateTime(2025, 5, 26, 0, 0, 0, DateTimeKind.Utc), // Hardcoded instead of now.AddDays(-7)
        EndDate = new DateTime(2025, 6, 23, 23, 59, 59, DateTimeKind.Utc) // Hardcoded instead of now.AddDays(21)
    },
    new Contest
    {
        ContestID = 4,
        Title = "Portrait Photography Contest",
        Description = "Capture the human essence in your portraits. Professional and amateur photographers welcome!",
        StartDate = new DateTime(2025, 6, 9, 0, 0, 0, DateTimeKind.Utc), // Hardcoded instead of now.AddDays(7)
        EndDate = new DateTime(2025, 8, 2, 23, 59, 59, DateTimeKind.Utc) // Hardcoded instead of now.AddDays(60)
    },
    new Contest
    {
        ContestID = 5,
        Title = "Nature's Beauty Video Contest",
        Description = "Create stunning videos showcasing the beauty of nature. Maximum 3 minutes.",
        StartDate = new DateTime(2025, 6, 16, 0, 0, 0, DateTimeKind.Utc), // Hardcoded instead of now.AddDays(14)
        EndDate = new DateTime(2025, 8, 17, 23, 59, 59, DateTimeKind.Utc) // Hardcoded instead of now.AddDays(75)
    }
);

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, Name = "User" },
            new Role { RoleID = 2, Name = "Admin" }
        );

        // Seed Talent Opportunities
        modelBuilder.Entity<TalentOpportunity>().HasData(
            new TalentOpportunity
            {
                Id = 1,
                Title = "Freelance Graphic Designer Needed",
                Description = "Looking for a creative graphic designer to help with branding and marketing materials.",
                Requirements = "- 3+ years of experience in graphic design\n- Proficiency in Adobe Creative Suite\n- Strong portfolio showcasing design work",
                Responsibilities = "- Create brand assets\n- Design marketing materials\n- Collaborate with marketing team",
                Benefits = "- Flexible hours\n- Remote work\n- Competitive pay",
                Location = "Remote",
                IsRemote = true,
                SalaryMin = 25,
                SalaryMax = 45,
                SalaryCurrency = "USD",
                SalaryPeriod = "per hour",
                Type = OpportunityType.Freelance,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = DateTime.UtcNow.AddDays(30),
                StartDate = DateTime.UtcNow.AddDays(7),
                PostedByUserId = 2, // Posted by recruiter1 (ID: 2)
                CategoryId = 3, // Art
                TalentArea = "Graphic Design"
            },
            new TalentOpportunity
            {
                Id = 2,
                Title = "Music Video Dancers (Hip Hop)",
                Description = "Casting professional hip hop dancers for an upcoming music video shoot.",
                Requirements = "- Strong hip hop dance background\n- Performance experience\n- Available for full day shoot",
                Responsibilities = "- Attend rehearsals\n- Perform in music video\n- Follow choreography",
                Benefits = "- Paid gig\n- Meals provided\n- Credit in video",
                Location = "Los Angeles, CA",
                IsRemote = false,
                SalaryMin = 300,
                SalaryMax = 500,
                SalaryCurrency = "USD",
                SalaryPeriod = "per day",
                Type = OpportunityType.Gig,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = DateTime.UtcNow.AddDays(14),
                StartDate = DateTime.UtcNow.AddDays(21),
                PostedByUserId = 1,
                CategoryId = 1, // Dance
                TalentArea = "Hip Hop Dance"
            },
            new TalentOpportunity
            {
                Id = 3,
                Title = "Social Media Content Creator",
                Description = "Looking for a creative content creator to manage our social media presence.",
                Requirements = "- Experience in social media management\n- Content creation skills\n- Understanding of current trends",
                Responsibilities = "- Create engaging content\n- Schedule posts\n- Engage with audience",
                Benefits = "- Remote work\n- Flexible schedule\n- Creative freedom",
                Location = "Remote",
                IsRemote = true,
                SalaryMin = 2000,
                SalaryMax = 3500,
                SalaryCurrency = "USD",
                SalaryPeriod = "per month",
                Type = OpportunityType.Job,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = DateTime.UtcNow.AddDays(21),
                StartDate = DateTime.UtcNow.AddDays(30),
                PostedByUserId = 1,
                CategoryId = 10, // Other
                TalentArea = "Content Creation"
            }
        );

        // Seed some Opportunity Applications
        modelBuilder.Entity<OpportunityApplication>().HasData(
            new OpportunityApplication
            {
                ApplicationID = 1,
                UserID = 3, // artist1 (ID: 3) applying for the position
                TalentOpportunityID = 1,
                CoverLetter = "I'm a passionate graphic designer with 4 years of experience...",
                PortfolioLink = "https://example.com/portfolio/johndoe",
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            },
            new OpportunityApplication
            {
                ApplicationID = 2,
                UserID = 4, // dancer1 (ID: 4) applying for the position
                TalentOpportunityID = 2,
                CoverLetter = "Professional dancer with 5+ years of experience in hip hop...",
                Status = ApplicationStatus.UnderReview,
                AppliedAt = DateTime.UtcNow.AddDays(-1)
            }
        );

        // Seed some Notifications
        modelBuilder.Entity<Notification>().HasData(
            new Notification
            {
                NotificationID = 1,
                UserID = 3, // artist1 (ID: 3) who applied
                Title = "Application Received",
                Message = "Your application for 'Freelance Graphic Designer' has been received.",
                Type = NotificationType.ApplicationUpdate,
                Status = NotificationStatus.Unread,
                RelatedEntityID = 1, // Application ID
                RelatedEntityType = "Application",
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                NotificationID = 2,
                UserID = 4, // dancer1 (ID: 4) who applied
                Title = "Application Under Review",
                Message = "Your application for 'Music Video Dancers' is now under review.",
                Type = NotificationType.ApplicationUpdate,
                Status = NotificationStatus.Unread,
                RelatedEntityID = 2, // Application ID
                RelatedEntityType = "Application",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        );
    }
}