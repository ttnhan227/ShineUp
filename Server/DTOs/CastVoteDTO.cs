using System.ComponentModel.DataAnnotations;

// DTO để bỏ phiếu cho một entry
// Không cần UserID vì sẽ lấy từ token người dùng đăng nhập
namespace Server.DTOs
{
    public class CastVoteDTO
    {
        public int EntryID { get; set; } // ID của entry muốn vote
    }
}
