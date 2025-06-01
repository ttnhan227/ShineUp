// DTO để hiển thị kết quả vote của từng entry
namespace Server.DTOs
{
    public class VoteResultDTO
    {
        public int EntryID { get; set; }         // ID entry
        public string VideoTitle { get; set; }   // Tên video
        public string Caption { get; set; }      // Chú thích
        public int VoteCount { get; set; }       // Số lượng vote nhận được
    }
}
