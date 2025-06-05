using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Client.Models;

// Base Community ViewModel
public class CommunityViewModel
{
    public int CommunityID { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public string? CoverImageUrl { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public int CreatedByUserID { get; set; }
    
    public int? PrivacyID { get; set; }
    
    public string? PrivacyName { get; set; }
    
    public List<int> MemberUserIds { get; set; } = new List<int>();
    
    public int MemberCount => MemberUserIds?.Count ?? 0;
    
    public bool IsCurrentUserMember { get; set; }
    
    public bool IsCurrentUserAdmin { get; set; }
}

// Detailed Community ViewModel (for Details page)
public class CommunityDetailsViewModel : CommunityViewModel
{
    public string CreatedByUsername { get; set; } = string.Empty;
    
    public string CreatedByFullName { get; set; } = string.Empty;
    
    public List<CommunityMemberViewModel> Members { get; set; } = new List<CommunityMemberViewModel>();
    
    public List<PostViewModel> Posts { get; set; } = new List<PostViewModel>();
    
    public int PostsCount => Posts?.Count ?? 0;
}

// Create Community ViewModel
public class CreateCommunityViewModel
{
    [Required(ErrorMessage = "Community name is required")]
    [StringLength(100, ErrorMessage = "Community name cannot exceed 100 characters")]
    [Display(Name = "Community Name")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Cover Image")]
    public IFormFile? CoverImage { get; set; }
    
    [Required(ErrorMessage = "Privacy setting is required")]
    [Display(Name = "Privacy")]
    public int? PrivacyID { get; set; }
    
    // For dropdown lists
    public SelectList? PrivacyOptions { get; set; }
}

// Edit Community ViewModel
public class EditCommunityViewModel
{
    public int CommunityID { get; set; }
    
    [Required(ErrorMessage = "Community name is required")]
    [StringLength(100, ErrorMessage = "Community name cannot exceed 100 characters")]
    [Display(Name = "Community Name")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Cover Image")]
    public IFormFile? CoverImage { get; set; }
    
    public string? CurrentCoverImageUrl { get; set; }
    
    [Required(ErrorMessage = "Privacy setting is required")]
    [Display(Name = "Privacy")]
    public int PrivacyID { get; set; }
    
    // For dropdown lists
    public SelectList? PrivacyOptions { get; set; }
}

// Community Member ViewModel
public class CommunityMemberViewModel
{
    public int UserID { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    
    public string Username { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string Role { get; set; } = string.Empty;
    
    public DateTime JoinedAt { get; set; }
    
    public string? ProfileImageURL { get; set; }
    
    public bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
}




// Community Statistics ViewModel
public class CommunityStatsViewModel
{
    public int TotalCommunities { get; set; }
    
    public int PublicCommunities { get; set; }
    
    public int PrivateCommunities { get; set; }
    
    public int TotalMembers { get; set; }
    
    public int TotalPosts { get; set; }
    
    public List<CommunityViewModel> PopularCommunities { get; set; } = new List<CommunityViewModel>();
    
    public List<CommunityViewModel> RecentCommunities { get; set; } = new List<CommunityViewModel>();
}

// Join Community Request ViewModel
public class JoinCommunityRequestViewModel
{
    public int CommunityID { get; set; }
    
    public string CommunityName { get; set; } = string.Empty;
    
    public string? Message { get; set; }
}

// Community Search ViewModel
public class CommunitySearchViewModel
{
    [Display(Name = "Search")]
    public string? SearchTerm { get; set; }
    
    [Display(Name = "Privacy")]
    public int? PrivacyFilter { get; set; }
    
    [Display(Name = "Sort By")]
    public string SortBy { get; set; } = "name";
    
    public List<CommunityViewModel> Results { get; set; } = new List<CommunityViewModel>();
    
    public SelectList? PrivacyOptions { get; set; }
    
    public SelectList? SortOptions { get; set; }
}