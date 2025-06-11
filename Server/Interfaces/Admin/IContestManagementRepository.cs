using Server.DTOs.Admin;
using Server.Models;

namespace Server.Interfaces.Admin;

public interface IContestManagementRepository
{
    // Contest Management
    Task<IEnumerable<AdminContestDTO>> GetAllContestsAsync();
    Task<AdminContestDTO?> GetContestByIdAsync(int id);
    Task<AdminContestDTO> CreateContestAsync(AdminCreateContestDTO createContestDto, int createdBy);
    Task<AdminContestDTO?> UpdateContestAsync(int id, AdminUpdateContestDTO updateContestDto);
    Task<bool> DeleteContestAsync(int id);

    // Contest Entry Management
    Task<IEnumerable<ContestEntry>> GetContestEntriesAsync(int contestId);
    Task<ContestEntry?> GetContestEntryByIdAsync(int entryId);
    Task<bool> DeleteContestEntryAsync(int entryId);

    // Contest Stats
    Task<ContestStatsDTO> GetContestStatsAsync(int contestId);

    // Contest Winner
    Task<bool> DeclareWinnerAsync(int entryId);
}

public class ContestStatsDTO
{
    public int TotalEntries { get; set; }
    public int UniqueParticipants { get; set; }

    public DateTime? LastEntryDate { get; set; }
    // Add more stats as needed
}