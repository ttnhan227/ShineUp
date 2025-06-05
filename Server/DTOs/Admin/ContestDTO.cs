using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Server.Models;

namespace Server.DTOs.Admin;

public class AdminContestDTO
{
    public int ContestID { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime StartDate { get; set; }
    
    [Required]
    [DataType(DataType.DateTime)]
    public DateTime EndDate { get; set; }
    
  
    
    // Navigation property
    public ICollection<ContestEntry> ContestEntries { get; set; }
}
