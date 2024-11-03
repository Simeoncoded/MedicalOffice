using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class PatientPhoto
    {
        public int ID { get; set; }

        [ScaffoldColumn(false)]
        public byte[]? Content { get; set; }

        [StringLength(255)]
        public string? MimeType { get; set; }

        public int PatientID { get; set; }
        public Patient? Patient { get; set; }
    }
}
