using System;
using System.ComponentModel.DataAnnotations;

namespace Client.Areas.Admin.Models
{
    public class CreateContestViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.DateTime)]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    }

    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = (DateTime)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);

            if (property == null)
                throw new ArgumentException("Property with this name not found");

            var comparisonValue = (DateTime)property.GetValue(validationContext.ObjectInstance);

            if (currentValue <= comparisonValue)
                return new ValidationResult(ErrorMessage ?? "End date must be after start date");

            return ValidationResult.Success;
        }
    }
}
