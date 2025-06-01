using System.ComponentModel.DataAnnotations;

// DTO dùng để tạo mới một cuộc thi
namespace Server.DTOs
{
    public class CreateContestDTO
    {
        public string Title { get; set; }  // Tiêu đề cuộc thi
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}