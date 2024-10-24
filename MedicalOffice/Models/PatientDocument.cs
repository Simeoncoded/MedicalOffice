using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class PatientDocument : UploadedFile
    {

        [Display(Name = "Patient")]
        public int PatientID { get; set; }

        public Patient? Patient { get; set; }
    }
}
