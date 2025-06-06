using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class EditCommunityViewModel
{
    public int CommunityID { get; set; }

    [Required(ErrorMessage = "Tên cộng đồng là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên cộng đồng không được vượt quá 100 ký tự")]
    [Display(Name = "Tên cộng đồng")]
    public string Name { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Ảnh bìa")]
    public IFormFile? CoverImage { get; set; }

    [Display(Name = "URL ảnh bìa hiện tại")]
    public string? CurrentCoverImageUrl { get; set; }

    [Display(Name = "Quyền riêng tư")]
    public int? PrivacyID { get; set; }
}
