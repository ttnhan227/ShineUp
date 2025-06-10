namespace Server.DTOs.Admin;

public class AdminCategoryDTO
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public string Description { get; set; }
}

public class CreateAdminCategoryDTO
{
    public string CategoryName { get; set; }
    public string Description { get; set; }
}