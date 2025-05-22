namespace Client.Models
{
    public class UserViewModel
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? Bio { get; set; }
        public string? ProfileImageURL { get; set; }
        public string? TalentArea { get; set; }
    }
}
