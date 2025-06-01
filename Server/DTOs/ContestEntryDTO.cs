namespace Server.DTOs;

// DTO để hiển thị thông tin bài dự thi trong cuộc thi
public class ContestEntryDTO
{
    public int EntryID { get; set; }           // ID bài dự thi
    public int ContestID { get; set; }         // ID cuộc thi
    public Guid VideoID { get; set; }          // Video gửi kèm
    public int UserID { get; set; }            // Người gửi (admin hoặc hệ thống có thể dùng)
    public string Caption { get; set; }        // Mô tả nội dung video
    public DateTime SubmittedAt { get; set; }  // Ngày giờ gửi bài
}