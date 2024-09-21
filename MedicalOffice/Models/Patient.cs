using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class Patient : IValidatableObject
    {

        public int ID { get; set; }

        [Display(Name = "Patient")]

        public string Summary
        {
            get
            {
                return FirstName
                    + (string.IsNullOrEmpty(MiddleName) ? "" :
                         (" " + (char?)MiddleName[0] + " ").ToUpper())
                    + LastName;
            }
        }

        public string? Age
        {
            get
            {
                if (DOB == null) { return null; }
                DateTime today = DateTime.Today;
                int? a = today.Year - DOB?.Year
                - ((today.Month < DOB?.Month ||
                    (today.Month == DOB?.Month && today.Day < DOB?.Day) ? 1 : 0));
                return a?.ToString();
            }
        }

        [Display(Name = "Age (DOB)")]

        public string AgeSummary => (DOB == null) ? "Unknown" : Age + "(" + DOB.GetValueOrDefault().ToString("yyyy-mm-dd") + ")";

        [Display(Name = "Phone")]

        public string PhoneFormatted => "(" + Phone?.Substring(0, 3) + ")"
                +Phone?.Substring(3, 3) + "-" + Phone?[6..];

     

        //[Required(ErrorMessage = "You cannot leave the OHIP number blank")]
        [RegularExpression("^\\d{10}$", ErrorMessage = "The OHIP number must be exactly 10 numeric digits.")]
        [StringLength(10)]

        public string? OHIP { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "You cannot leave the first name blank.")]
        [StringLength(50, ErrorMessage = "First name cannot be more than 50 characters long.")]
        public string FirstName { get; set; } = "";

        [Display(Name = "Middle Name")]
        [StringLength(50, ErrorMessage="Middle name cannot be more than 50 characters long.")]
        public string? MiddleName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "You cannot leave the Last name blank.")]
        [StringLength(100, ErrorMessage = "Middle name cannot be more than 50 characters long.")]
        public string LastName { get; set; } = "";

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DOB { get; set; }

        [Display(Name = "Visits/Yr")]
        [Required(ErrorMessage = "You cannot leave the number of expected visits per year blank.")]
        [Range(1, 12, ErrorMessage = "The number of expected visits per year must be between 1 and 12.")]
        public byte ExpYrVisits { get; set; } = 2; //Most common value is 2.

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression("^\\d{10}$", ErrorMessage = "Please enter a valid 10-digit phone number (no spaces).")]
        [DataType(DataType.PhoneNumber)]
        [StringLength(10)]
        public string Phone { get; set; } = "";

        [StringLength(255)]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [Required(ErrorMessage ="You must select the Patient's Health Coverage!")]
        public Coverage Coverage { get; set; }

        [Display(Name = "Medical Trial")]
        public int? MedicalTrialID { get; set; }

        [Display(Name = "Medical Trial")]
        public MedicalTrial? MedicalTrial { get; set; }

        [Required(ErrorMessage ="You must select a Primary Care Physician.")]
        [Display(Name = "Doctor")]
        public int DoctorID { get; set; }
        
        public Doctor? Doctor { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
           //The second argument (memberNames) is a IEnumerable<string> that 
           //identifies which property has a validation error.
           //If the error is with the entire object, use an empty string or
           //leave the argument out and the message will display in the validation summary.
           if(DOB.GetValueOrDefault() > DateTime.Today)
            {
                yield return new ValidationResult("Date of Birth cannot be in the future.", ["DOB"]);
            }

           //OHIP Number required but only if they have OHIP Coverage.
           if(Coverage == Coverage.OHIP)
            {
                if(OHIP == null)
                {
                    yield return new ValidationResult("OHIP cannot be left blank since the patient has OHIP coverage.", ["OHIP"]);
                }
            }
           else
            {
                if(OHIP != null)
                {
                    yield return new ValidationResult("OHIP must be blank since the patient does no have OHIP coverage.", ["OHIP"]);
                }
            }
        }
    }
}
