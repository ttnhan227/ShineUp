namespace Server.DTOs;

// DTO để hiển thị thông tin cuộc thi ra ngoài UI
public class ContestDTO
{
    public int ContestID { get; set; }         // ID cuộc thi
    public string Title { get; set; }          // Tiêu đề
    public string Description { get; set; }    // Mô tả
    public DateTime StartDate { get; set; }    // Ngày bắt đầu
    public DateTime EndDate { get; set; }      // Ngày kết thúc
}