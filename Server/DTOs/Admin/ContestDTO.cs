using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Server.DTOs;

namespace Server.DTOs.Admin;

public class AdminContestDTO
{
    public int ContestID { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; }
    
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; }
    
    public bool IsClosed { get; set; }
    
    // Navigation property with DTO
    public ICollection<ContestEntryDTO> ContestEntries { get; set; } = new List<ContestEntryDTO>();
}
