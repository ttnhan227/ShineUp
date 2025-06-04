namespace Client.Models
{
    public enum OpportunityType
    {
        Job = 0,
        Gig = 1,
        Freelance = 2,
        Audition = 3,
        CastingCall = 4,
        Collaboration = 5,
        Internship = 6,
        Volunteer = 7
    }

    public enum OpportunityStatus
    {
        Draft = 0,
        Open = 1,
        InProgress = 2,
        Closed = 3,
        Cancelled = 4
    }

    public enum ApplicationStatus
    {
        Pending = 0,
        Shortlisted = 1,
        Approved = 2,
        Rejected = 3,
        Withdrawn = 4
    }
}
