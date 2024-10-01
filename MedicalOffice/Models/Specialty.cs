using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class Specialty
    {
        public int ID { get; set; }

        [Display(Name = "Medical Specialty")]
        [Required(ErrorMessage = "You cannot leave the name of the Specialty blank.")]
        [StringLength(100, ErrorMessage = "Too Big!")]
        public string SpecialtyName { get; set; } = "";

        [Display(Name = "Doctors")]
        public ICollection<DoctorSpecialty> DoctorSpecialties { get; set; } = new HashSet<DoctorSpecialty>();
    }
}
