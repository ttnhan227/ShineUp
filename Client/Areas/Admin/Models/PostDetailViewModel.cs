using System;

namespace Client.Areas.Admin.Models
{
    public class PostDetailViewModel
    {
        public int PostID { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string CategoryName { get; set; }
        public string PrivacyName { get; set; }
        public string CommunityName { get; set; }
        public int CommentCount { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
