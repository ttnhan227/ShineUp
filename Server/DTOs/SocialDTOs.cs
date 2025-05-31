using System;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    public class CommentDto
    {
        public int CommentID { get; set; }
        public int? PostID { get; set; }
        public string? VideoID { get; set; }
        public int UserID { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
        public string ProfileImageURL { get; set; }
    }

    public class LikeDto
    {
        public int LikeID { get; set; }
        public int? PostID { get; set; }
        public string? VideoID { get; set; }
        public int UserID { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Username { get; set; }
        public string ProfileImageURL { get; set; }
    }

    public class CreateCommentDto
    {
        [Required]
        public int? PostID { get; set; }
        public string? VideoID { get; set; }
        
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; }
    }

    public class CreateLikeDto
    {
        [Required]
        public int? PostID { get; set; }
        public string? VideoID { get; set; }
    }
}
