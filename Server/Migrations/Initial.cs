#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Server.Migrations;

/// <inheritdoc />
public partial class Initial : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "Categories",
            table => new
            {
                CategoryID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CategoryName = table.Column<string>("text", nullable: false),
                Description = table.Column<string>("text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Categories", x => x.CategoryID); });

        migrationBuilder.CreateTable(
            "Contests",
            table => new
            {
                ContestID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Title = table.Column<string>("text", nullable: false),
                Description = table.Column<string>("text", nullable: false),
                StartDate = table.Column<DateTime>("timestamp with time zone", nullable: false),
                EndDate = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Contests", x => x.ContestID); });

        migrationBuilder.CreateTable(
            "Privacies",
            table => new
            {
                PrivacyID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>("text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Privacies", x => x.PrivacyID); });

        migrationBuilder.CreateTable(
            "Roles",
            table => new
            {
                RoleID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>("text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Roles", x => x.RoleID); });

        migrationBuilder.CreateTable(
            "Users",
            table => new
            {
                UserID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Username = table.Column<string>("text", nullable: false),
                Email = table.Column<string>("text", nullable: false),
                PasswordHash = table.Column<string>("text", nullable: false),
                Bio = table.Column<string>("text", nullable: false),
                ProfileImageURL = table.Column<string>("text", nullable: false),
                RoleID = table.Column<int>("integer", nullable: false),
                TalentArea = table.Column<string>("text", nullable: false),
                CreatedAt = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.UserID);
                table.ForeignKey(
                    "FK_Users_Roles_RoleID",
                    x => x.RoleID,
                    "Roles",
                    "RoleID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Messages",
            table => new
            {
                MessageID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SenderID = table.Column<int>("integer", nullable: false),
                ReceiverID = table.Column<int>("integer", nullable: false),
                MessageContent = table.Column<string>("text", nullable: false),
                SentAt = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Messages", x => x.MessageID);
                table.ForeignKey(
                    "FK_Messages_Users_ReceiverID",
                    x => x.ReceiverID,
                    "Users",
                    "UserID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Messages_Users_SenderID",
                    x => x.SenderID,
                    "Users",
                    "UserID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            "Videos",
            table => new
            {
                VideoID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserID = table.Column<int>("integer", nullable: false),
                CategoryID = table.Column<int>("integer", nullable: false),
                Title = table.Column<string>("text", nullable: false),
                Description = table.Column<string>("text", nullable: false),
                VideoURL = table.Column<string>("text", nullable: false),
                ThumbnailURL = table.Column<string>("text", nullable: false),
                PrivacyID = table.Column<int>("integer", nullable: false),
                UploadDate = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Videos", x => x.VideoID);
                table.ForeignKey(
                    "FK_Videos_Categories_CategoryID",
                    x => x.CategoryID,
                    "Categories",
                    "CategoryID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_Videos_Privacies_PrivacyID",
                    x => x.PrivacyID,
                    "Privacies",
                    "PrivacyID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_Videos_Users_UserID",
                    x => x.UserID,
                    "Users",
                    "UserID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Comments",
            table => new
            {
                CommentID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                VideoID = table.Column<int>("integer", nullable: false),
                UserID = table.Column<int>("integer", nullable: false),
                Content = table.Column<string>("text", nullable: false),
                CreatedAt = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.CommentID);
                table.ForeignKey(
                    "FK_Comments_Users_UserID",
                    x => x.UserID,
                    "Users",
                    "UserID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_Comments_Videos_VideoID",
                    x => x.VideoID,
                    "Videos",
                    "VideoID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ContestEntries",
            table => new
            {
                EntryID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ContestID = table.Column<int>("integer", nullable: false),
                VideoID = table.Column<int>("integer", nullable: false),
                UserID = table.Column<int>("integer", nullable: false),
                SubmissionDate = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContestEntries", x => x.EntryID);
                table.ForeignKey(
                    "FK_ContestEntries_Contests_ContestID",
                    x => x.ContestID,
                    "Contests",
                    "ContestID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ContestEntries_Users_UserID",
                    x => x.UserID,
                    "Users",
                    "UserID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ContestEntries_Videos_VideoID",
                    x => x.VideoID,
                    "Videos",
                    "VideoID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "Likes",
            table => new
            {
                LikeID = table.Column<int>("integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                VideoID = table.Column<int>("integer", nullable: false),
                UserID = table.Column<int>("integer", nullable: false),
                CreatedAt = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Likes", x => x.LikeID);
                table.ForeignKey(
                    "FK_Likes_Users_UserID",
                    x => x.UserID,
                    "Users",
                    "UserID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_Likes_Videos_VideoID",
                    x => x.VideoID,
                    "Videos",
                    "VideoID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_Comments_UserID",
            "Comments",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_Comments_VideoID",
            "Comments",
            "VideoID");

        migrationBuilder.CreateIndex(
            "IX_ContestEntries_ContestID",
            "ContestEntries",
            "ContestID");

        migrationBuilder.CreateIndex(
            "IX_ContestEntries_UserID",
            "ContestEntries",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_ContestEntries_VideoID",
            "ContestEntries",
            "VideoID");

        migrationBuilder.CreateIndex(
            "IX_Likes_UserID",
            "Likes",
            "UserID");

        migrationBuilder.CreateIndex(
            "IX_Likes_VideoID",
            "Likes",
            "VideoID");

        migrationBuilder.CreateIndex(
            "IX_Messages_ReceiverID",
            "Messages",
            "ReceiverID");

        migrationBuilder.CreateIndex(
            "IX_Messages_SenderID",
            "Messages",
            "SenderID");

        migrationBuilder.CreateIndex(
            "IX_Users_RoleID",
            "Users",
            "RoleID");

        migrationBuilder.CreateIndex(
            "IX_Videos_CategoryID",
            "Videos",
            "CategoryID");

        migrationBuilder.CreateIndex(
            "IX_Videos_PrivacyID",
            "Videos",
            "PrivacyID");

        migrationBuilder.CreateIndex(
            "IX_Videos_UserID",
            "Videos",
            "UserID");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "Comments");

        migrationBuilder.DropTable(
            "ContestEntries");

        migrationBuilder.DropTable(
            "Likes");

        migrationBuilder.DropTable(
            "Messages");

        migrationBuilder.DropTable(
            "Contests");

        migrationBuilder.DropTable(
            "Videos");

        migrationBuilder.DropTable(
            "Categories");

        migrationBuilder.DropTable(
            "Privacies");

        migrationBuilder.DropTable(
            "Users");

        migrationBuilder.DropTable(
            "Roles");
    }
}