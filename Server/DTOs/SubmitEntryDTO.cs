using System.ComponentModel.DataAnnotations;

// DTO dùng để gửi bài dự thi
// Không bao gồm UserID vì được lấy từ token (bảo mật hơn)
namespace Server.DTOs
{
    public class SubmitEntryDTO
    {
        public int ContestID { get; set; }       // ID cuộc thi muốn tham gia
        public Guid VideoID { get; set; }        // Video đã upload sẵn
        public string Caption { get; set; }      // Chú thích ngắn gọn cho video
    }

}
