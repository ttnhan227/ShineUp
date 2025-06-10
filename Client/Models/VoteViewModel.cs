namespace Client.Models;

// Matches the server's VoteDTO
public class VoteDto
{
    public int VoteID { get; set; }
    public int EntryID { get; set; }
    public int UserID { get; set; }
    public DateTime VotedAt { get; set; }
}

// Request model for voting
public class VoteRequest
{
    public int EntryId { get; set; }
}

// Response model for voting
public class VoteResponse
{
    public string Message { get; set; }
    public int VoteCount { get; set; }
    public bool HasVoted { get; set; }
}

// ViewModel for displaying vote results
public class VoteResultViewModel
{
    public int EntryId { get; set; }
    public int VoteCount { get; set; }
    public bool HasVoted { get; set; }
    public string Message { get; set; }
}