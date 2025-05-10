using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShineUp.Server.Models
{
    public class Contest
    {
        [Key]
        public int ContestID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Navigation property
        public ICollection<ContestEntry> ContestEntries { get; set; }
    }
}