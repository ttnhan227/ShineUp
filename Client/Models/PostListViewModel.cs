using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class PostListViewModel
{
    public int PostID { get; set; }
    
    [Display(Name = "Title")]
    public string Title { get; set; }
    
    [Display(Name = "Content")]
    public string Content { get; set; }
    
    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; }
    
    [Display(Name = "Author")]
    public string UserName { get; set; }
    
    [Display(Name = "Category")]
    public string? CategoryName { get; set; }
    
    [Display(Name = "Likes")]
    public int LikesCount { get; set; }
    
    [Display(Name = "Comments")]
    public int CommentsCount { get; set; }

    public List<MediaFileViewModel> MediaFiles { get; set; } = new List<MediaFileViewModel>();
}

public class MediaFileViewModel
{
    public string Url { get; set; }
    public string Type { get; set; } // "image" or "video"
    public string PublicId { get; set; }
} 