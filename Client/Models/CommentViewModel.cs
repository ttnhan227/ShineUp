using System;
using System.ComponentModel.DataAnnotations;

namespace Client.Models
{
    public class CommentViewModel
    {
        public int CommentID { get; set; }
        public int? PostID { get; set; }
        public string? VideoID { get; set; }
        public int UserID { get; set; }
        public string Username { get; set; }
        public string? ProfileImageURL { get; set; }
        
        [Required(ErrorMessage = "Comment content is required")]
        [StringLength(1000, ErrorMessage = "Comment cannot be longer than 1000 characters")]
        public string Content { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public string TimeAgo => GetTimeAgo();

        private string GetTimeAgo()
        {
            var timeSpan = DateTime.UtcNow - CreatedAt;

            if (timeSpan.TotalSeconds < 60)
                return $"{Math.Floor(timeSpan.TotalSeconds)} seconds ago";
            
            if (timeSpan.TotalMinutes < 60)
                return $"{Math.Floor(timeSpan.TotalMinutes)} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")} ago";
            
            if (timeSpan.TotalHours < 24)
                return $"{Math.Floor(timeSpan.TotalHours)} hour{(timeSpan.TotalHours >= 2 ? "s" : "")} ago";
            
            if (timeSpan.TotalDays < 30)
                return $"{Math.Floor(timeSpan.TotalDays)} day{(timeSpan.TotalDays >= 2 ? "s" : "")} ago";
            
            if (timeSpan.TotalDays < 365)
            {
                var months = Math.Floor(timeSpan.TotalDays / 30);
                return $"{months} month{(months >= 2 ? "s" : "")} ago";
            }
            
            var years = Math.Floor(timeSpan.TotalDays / 365);
            return $"{years} year{(years >= 2 ? "s" : "")} ago";
        }
    }

    public class CreateCommentViewModel
    {
        public int? PostID { get; set; }
        public string? VideoID { get; set; }
        
        [Required(ErrorMessage = "Comment content is required")]
        [StringLength(1000, ErrorMessage = "Comment cannot be longer than 1000 characters")]
        public string Content { get; set; }
    }
}
