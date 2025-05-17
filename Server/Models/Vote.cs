using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    public class Vote // aaa
    {
        [Key] 
        public int VoteID { get; set; }
        [ForeignKey("ContestEntry")] 
        public int EntryID { get; set; }
        public ContestEntry ContestEntry { get; set; }
        [ForeignKey("User")] 
        public int UserID { get; set; }
        public User User { get; set; }
        public DateTime VotedAt { get; set; }
    }
}
