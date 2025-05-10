using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShineUp.Server.Models
{
    public class Privacy
    {
        [Key]
        public int PrivacyID { get; set; }
        public string Name { get; set; }

        // Navigation property
        public ICollection<Video> Videos { get; set; }
    }
}