using System;
using System.Collections.Generic;

namespace Client.Models
{
    public class CommunityDetailsViewModel
    {
        public CommunityViewModel Community { get; set; } = null!;
        public List<CommunityMemberViewModel> Members { get; set; } = new();
        public CommunityRole CommunityRole { get; set; } = CommunityRole.None;
    }
}
