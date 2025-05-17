namespace Server.DTOs
{
    public class EntryResultDTO
    {
        public int EntryID { get; set; }
        public string VideoTitle { get; set; }
        public string VideoURL { get; set; }
        public string UserName { get; set; }
        public int VoteCount { get; set; }
    }
}
//(tuỳ chọn – thay cho object ẩn danh)
//Tạo một DTO nếu bạn không muốn trả về object ẩn danh