using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class AppointmentReason
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "You cannot leave the name of the complaint blank.")]
        [Display(Name = "Reason for Apt.")]
        [StringLength(50, ErrorMessage = "Too Big!")]
        [DisplayFormat(NullDisplayText = "No Reason Given")]
        public string ReasonName { get; set; } = "";

        public ICollection<Appointment> Appointments { get; set; } = new HashSet<Appointment>();
    }

}
