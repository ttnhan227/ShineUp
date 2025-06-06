using System;
using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models
{
    public class EditContestViewModel
    {
        public int ContestID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.DateTime)]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date")]
        public DateTime EndDate { get; set; }
    }
}
