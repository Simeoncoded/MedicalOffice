using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class DoctorDocument : UploadedFile
    {
        [Display(Name = "Doctor")]
        public int DoctorID { get; set; }

        public Doctor? Doctor { get; set; }
    }
}
