using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class Doctor
    {
        public int ID { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage ="You cannot leave the first name blank.")]
        [StringLength(50, ErrorMessage ="First name cannot be more than 50 characters long.")]
        public string FirstName { get; set; } = "";

        [Display(Name = "Middle Name")]
        [StringLength(50, ErrorMessage = "Middle name cannot be more than 50 characters long.")]
        public string? MiddleName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "You cannot leave the last name blank.")]
        [StringLength(100, ErrorMessage = "First name cannot be more than 100 characters long.")]
        public string LastName { get; set; } = "";

        public ICollection<Patient> Patients { get; set; } = new HashSet<Patient>();


    }
}
