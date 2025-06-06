using System;

namespace Client.Areas.Admin.Models
{
    public class ContestEntryViewModel
    {
        public int EntryID { get; set; }
        public int ContestID { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MediaUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsWinner { get; set; }
    }
}
