using System.ComponentModel.DataAnnotations;

namespace Server.DTOs.Admin;

public class CreateUpdateOpportunityDTO
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Location cannot be longer than 100 characters")]
    public string? Location { get; set; }

    public bool IsRemote { get; set; }

    [Required(ErrorMessage = "Opportunity type is required")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Status is required")]
    public string Status { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? ApplicationDeadline { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(100, ErrorMessage = "Talent area cannot be longer than 100 characters")]
    public string? TalentArea { get; set; }
}