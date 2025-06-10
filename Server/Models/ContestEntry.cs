using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models;

public class ContestEntry
{
    private string? _imageId;

    private string? _videoId;

    [Key]
    public int EntryID { get; set; }

    public int ContestID { get; set; }
    public Contest Contest { get; set; }

    // Video entry (optional)
    public string? VideoID
    {
        get => _videoId;
        set
        {
            _videoId = value;
            if (value != null)
            {
                _imageId = null;
            }
        }
    }

    public Video? Video { get; set; }

    // Image entry (optional)
    public string? ImageID
    {
        get => _imageId;
        set
        {
            _imageId = value;
            if (value != null)
            {
                _videoId = null;
            }
        }
    }

    public Image? Image { get; set; }

    public int UserID { get; set; }
    public User User { get; set; }


    [Required]
    [StringLength(100)]
    public string Title { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsWinner { get; set; }

    public DateTime SubmissionDate { get; set; }

    // Helper property to get the media URL regardless of type
    [NotMapped]
    public string MediaUrl => Video?.VideoURL ?? Image?.ImageURL ?? string.Empty;

    // Navigation property for votes
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();

    // Helper property to get or set the media type
    [NotMapped]
    public string MediaType
    {
        get => VideoID != null ? "video" : "image";
        set
        {
            // When setting MediaType, clear the non-selected media type
            if (value == "video")
            {
                if (VideoID == null)
                {
                    VideoID = string.Empty;
                }

                ImageID = null;
            }
            else if (value == "image")
            {
                if (ImageID == null)
                {
                    ImageID = string.Empty;
                }

                VideoID = null;
            }
        }
    }
}