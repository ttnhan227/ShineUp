using System.ComponentModel.DataAnnotations;

namespace Server.DTOs;

public class ContestDTO
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

    public bool IsClosed { get; set; }
}