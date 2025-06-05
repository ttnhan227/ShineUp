using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Client.Models;

// Base Community ViewModel
public class CommunityViewModel
{
[Key]
public int CommunityID { get; set; }

[Required]
[MaxLength(100)]
public string Name { get; set; } = null!;

[MaxLength(500)]
public string? Description { get; set; }

public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

public DateTime? UpdatedAt { get; set; }

[Required]
public int CreatedByUserID { get; set; }

public UserViewModel CreatedBy { get; set; } = null!;

public int? PrivacyID { get; set; }

public PrivacyViewModel? Privacy { get; set; }

public string? CoverImageUrl { get; set; }

// Navigation
public ICollection<PostViewModel> Posts { get; set; } = new List<PostViewModel>();

public ICollection<CommunityMemberViewModel> Members { get; set; } = new List<CommunityMemberViewModel>();
}