using System;
using System.Collections.Generic;

namespace Client.Models
{
    public class CommunityDetailsViewModel
    {
        public CommunityViewModel Community { get; set; } = null!;
        public List<CommunityMemberViewModel> Members { get; set; } = new();
        public string UserRole { get; set; } = "None";
    }
}
