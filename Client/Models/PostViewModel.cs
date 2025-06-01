using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Client.Models
{

    public class PostViewModel
    {
        public int PostID { get; set; }

        [Display(Name = "Title")]
        public string Title { get; set; }

        [Display(Name = "Content")]
        public string Content { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Author")]
        public string Username { get; set; }

        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Display(Name = "User ID")]
        public int UserID { get; set; }

        [Display(Name = "Profile Image")]
        public string? ProfileImageURL { get; set; }

        [Display(Name = "Category")]
        public string? CategoryName { get; set; }

        [Display(Name = "Likes")]
        public int LikesCount { get; set; }

        [Display(Name = "Comments")]
        public int CommentsCount { get; set; }
        
        public bool HasLiked { get; set; }

        public List<MediaFileViewModel> MediaFiles { get; set; } = new List<MediaFileViewModel>();
    }

    public class MediaFileViewModel
    {
        public string Url { get; set; }
        public string Type { get; set; } // "image" or "video"
        public string PublicId { get; set; }
    }
    public class PostDetailsViewModel
    {
        public int PostID { get; set; }
    
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; }
    
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; }
    
        public DateTime CreatedAt { get; set; }
        // User info
        public int UserID { get; set; }
        public string Username { get; set; } // Changed from Username to Username
        public string FullName { get; set; }
        public string? ProfileImageURL { get; set; }
    
        // Category info
        public int? CategoryID { get; set; }
        public string? CategoryName { get; set; }
    
        // Privacy info
        public int? PrivacyID { get; set; }
        public string? PrivacyName { get; set; }
    
        // Media files
        public List<MediaFileViewModel> MediaFiles { get; set; } = new List<MediaFileViewModel>();
    
        // Social features
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool HasLiked { get; set; }
    }
    public class EditPostViewModel
    {
        public int PostID { get; set; }
    
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; }
    
        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Content")]
        public string Content { get; set; }
    
        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryID { get; set; }
    
        [Required(ErrorMessage = "Privacy setting is required")]
        [Display(Name = "Privacy")]
        public int PrivacyID { get; set; }

        public List<MediaFileViewModel> CurrentMediaFiles { get; set; } = new List<MediaFileViewModel>();
    } 
    public class CreatePostViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; }
    
        [Required(ErrorMessage = "Content is required")]
        [Display(Name = "Content")]
        public string Content { get; set; }
    
        [Display(Name = "Image")]
        public IFormFile? Image { get; set; }
    
        [Display(Name = "Video")]
        public IFormFile? Video { get; set; }
    
        [Display(Name = "Category")]
        public int? CategoryID { get; set; }
    
        [Display(Name = "Privacy")]
        public int? PrivacyID { get; set; }

        public List<IFormFile>? Images { get; set; }
        public List<IFormFile>? Videos { get; set; }

        public IEnumerable<SelectListItem>? Categories { get; set; }
        public IEnumerable<SelectListItem>? PrivacyOptions { get; set; }
    } 
}
