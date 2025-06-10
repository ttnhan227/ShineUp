using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models;

public class CategoryViewModel
{
    public int CategoryID { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters")]
    public string CategoryName { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
    public string Description { get; set; }
}