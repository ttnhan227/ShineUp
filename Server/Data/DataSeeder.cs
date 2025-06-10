using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data;

public static class DataSeeder
{
    public static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleID = 1, Name = "User" },
            new Role { RoleID = 2, Name = "Admin" },
            new Role { RoleID = 3, Name = "Recruiter" }
        );

        // Seed Privacy Settings
        modelBuilder.Entity<Privacy>().HasData(
            new Privacy { PrivacyID = 1, Name = "Public" },
            new Privacy { PrivacyID = 2, Name = "Friends Only" },
            new Privacy { PrivacyID = 3, Name = "Private" }
        );

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

        // Seed Users
        modelBuilder.Entity<User>().HasData(
            new User
            {
                UserID = 1,
                Username = "recruiter1",
                Email = "recruiter@example.com",
                FullName = "Sarah Johnson",
                PasswordHash =
                    "$2a$12$m8ez5m4AnAyK5WSi7Ec3a.E20i5wXDVRkjSFx01ycdGiCccWFUHD.", // Hardcoded bcrypt hash for "User@123"
                RoleID = 3,
                CreatedAt = new DateTime(2024, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                Verified = true,
                TalentArea = "Talent Acquisition"
            },
            new User
            {
                UserID = 2,
                Username = "dancer1",
                Email = "dancer@example.com",
                FullName = "Alex Chen",
                PasswordHash =
                    "$2a$12$m8ez5m4AnAyK5WSi7Ec3a.E20i5wXDVRkjSFx01ycdGiCccWFUHD.", // Hardcoded bcrypt hash for "User@123"
                RoleID = 1,
                CreatedAt = new DateTime(2024, 12, 17, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                Verified = true,
                TalentArea = "Hip Hop Dance"
            },
            new User
            {
                UserID = 3,
                Username = "musician1",
                Email = "musician@example.com",
                FullName = "Jamal Williams",
                PasswordHash =
                    "$2a$12$m8ez5m4AnAyK5WSi7Ec3a.E20i5wXDVRkjSFx01ycdGiCccWFUHD.", // Hardcoded bcrypt hash for "User@123"
                RoleID = 1,
                CreatedAt = new DateTime(2024, 12, 22, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                Verified = true,
                TalentArea = "Music Production"
            },
            new User
            {
                UserID = 4,
                Username = "creator1",
                Email = "creator@example.com",
                FullName = "Taylor Smith",
                PasswordHash =
                    "$2a$12$m8ez5m4AnAyK5WSi7Ec3a.E20i5wXDVRkjSFx01ycdGiCccWFUHD.", // Hardcoded bcrypt hash for "User@123"
                RoleID = 1,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                Verified = true,
                TalentArea = "Content Creation"
            },
            new User
            {
                UserID = 5,
                Username = "creator2",
                Email = "creator2@example.com",
                FullName = "Tom Hiddleston",
                PasswordHash =
                    "$2a$12$m8ez5m4AnAyK5WSi7Ec3a.E20i5wXDVRkjSFx01ycdGiCccWFUHD.", // Hardcoded bcrypt hash for "User@123"
                RoleID = 1,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                Verified = true,
                TalentArea = "Content Creation"
            },
            new User
            {
                UserID = 6,
                Username = "Administrator",
                Email = "admin@example.com",
                FullName = "Tom Hiddleston",
                PasswordHash =
                    "$2a$12$m8ez5m4AnAyK5WSi7Ec3a.E20i5wXDVRkjSFx01ycdGiCccWFUHD.", // Hardcoded bcrypt hash for "User@123"
                RoleID = 2,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                Verified = true,
                TalentArea = "ShineUp Admin"
            }
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
                StartDate = new DateTime(2025, 5, 26, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 6, 23, 23, 59, 59, DateTimeKind.Utc)
            },
            new Contest
            {
                ContestID = 4,
                Title = "Portrait Photography Contest",
                Description =
                    "Capture the human essence in your portraits. Professional and amateur photographers welcome!",
                StartDate = new DateTime(2025, 6, 9, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 8, 2, 23, 59, 59, DateTimeKind.Utc)
            },
            new Contest
            {
                ContestID = 5,
                Title = "Nature's Beauty Video Contest",
                Description = "Create stunning videos showcasing the beauty of nature. Maximum 3 minutes.",
                StartDate = new DateTime(2025, 6, 16, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2025, 8, 17, 23, 59, 59, DateTimeKind.Utc)
            }
        );

        // Seed Talent Opportunities
        modelBuilder.Entity<TalentOpportunity>().HasData(
            new TalentOpportunity
            {
                Id = 1,
                Title = "Lead Dancer for Summer Festival",
                Description = "Looking for an experienced hip hop dancer to perform at our annual summer festival.",
                Location = "New York, NY",
                IsRemote = false,
                Type = OpportunityType.Gig,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PostedByUserId = 1,
                CategoryId = 1,
                TalentArea = "Hip Hop Dance"
            },
            new TalentOpportunity
            {
                Id = 2,
                Title = "Music Producer for Indie Game",
                Description = "Seeking a music producer to create an original soundtrack for an upcoming indie game.",
                Location = "Remote",
                IsRemote = true,
                Type = OpportunityType.Freelance,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PostedByUserId = 1,
                CategoryId = 2,
                TalentArea = "Music Production"
            },
            new TalentOpportunity
            {
                Id = 3,
                Title = "Street Performer for Downtown District",
                Description = "Looking for talented street performers to entertain visitors in our downtown district.",
                Location = "Chicago, IL",
                IsRemote = false,
                Type = OpportunityType.Gig,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PostedByUserId = 1,
                CategoryId = 1,
                TalentArea = "Hip Hop Dance"
            },
            new TalentOpportunity
            {
                Id = 4,
                Title = "Freelance Graphic Designer Needed",
                Description =
                    "Seeking a creative graphic designer to create social media assets for a new brand launch. Remote work available.",
                Location = "Remote",
                IsRemote = true,
                Type = OpportunityType.Freelance,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = new DateTime(2025, 1, 31, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PostedByUserId = 1,
                CategoryId = 3,
                TalentArea = "Graphic Design"
            },
            new TalentOpportunity
            {
                Id = 5,
                Title = "Summer Art Exhibit - Call for Submissions",
                Description =
                    "Local gallery seeking emerging artists to showcase their work in our summer exhibition. All mediums welcome.",
                Location = "Hanoi",
                IsRemote = false,
                Type = OpportunityType.Collaboration,
                Status = OpportunityStatus.Open,
                ApplicationDeadline = new DateTime(2025, 2, 15, 23, 59, 59, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                PostedByUserId = 4,
                CategoryId = 3,
                TalentArea = "Visual Arts"
            }
        );

        // Seed Opportunity Applications
        modelBuilder.Entity<OpportunityApplication>().HasData(
            new OpportunityApplication
            {
                ApplicationID = 1,
                UserID = 2,
                TalentOpportunityID = 1,
                CoverLetter = "I have 5 years of experience in hip hop dance and have performed at multiple festivals.",
                Status = ApplicationStatus.UnderReview,
                AppliedAt = new DateTime(2024, 12, 30, 0, 0, 0, DateTimeKind.Utc),
                ReviewedAt = null,
                ReviewNotes = null
            },
            new OpportunityApplication
            {
                ApplicationID = 2,
                UserID = 3,
                TalentOpportunityID = 2,
                CoverLetter = "I specialize in creating atmospheric electronic music perfect for games.",
                Status = ApplicationStatus.Pending,
                AppliedAt = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                ReviewedAt = null,
                ReviewNotes = null
            },
            new OpportunityApplication
            {
                ApplicationID = 3,
                UserID = 2,
                TalentOpportunityID = 3,
                CoverLetter = "I'd love to bring my unique style to your downtown district!",
                Status = ApplicationStatus.Pending,
                AppliedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ReviewedAt = null,
                ReviewNotes = null
            }
        );

        // Seed Notifications
        modelBuilder.Entity<Notification>().HasData(
            new Notification
            {
                NotificationID = 1,
                UserID = 1,
                Message = "New application received for Lead Dancer position",
                Type = NotificationType.ApplicationUpdate,
                Status = NotificationStatus.Unread,
                CreatedAt = new DateTime(2024, 12, 31, 22, 0, 0, DateTimeKind.Utc)
            },
            new Notification
            {
                NotificationID = 2,
                UserID = 2,
                Message = "Your application for Lead Dancer is under review",
                Type = NotificationType.ApplicationUpdate,
                Status = NotificationStatus.Read,
                CreatedAt = new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc)
            },
            new Notification
            {
                NotificationID = 3,
                UserID = 3,
                Message = "New music production opportunity matches your profile",
                Type = NotificationType.OpportunityPosted,
                Status = NotificationStatus.Unread,
                CreatedAt = new DateTime(2024, 12, 31, 20, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}