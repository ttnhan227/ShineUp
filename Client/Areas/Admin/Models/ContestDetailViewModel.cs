using System;
using System.Collections.Generic;

namespace Client.Areas.Admin.Models
{
    public class ContestDetailViewModel
    {
        public int ContestID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IEnumerable<ContestEntryViewModel> Entries { get; set; } = new List<ContestEntryViewModel>();
    }
}
