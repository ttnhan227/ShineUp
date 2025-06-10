using System.ComponentModel.DataAnnotations;

namespace Client.Models;

public class CategoryViewModel
{
    public int CategoryID { get; set; }

    [Display(Name = "Category Name")]
    public string CategoryName { get; set; }

    [Display(Name = "Description")]
    public string Description { get; set; }
}