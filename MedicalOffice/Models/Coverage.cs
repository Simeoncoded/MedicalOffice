using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public enum Coverage
    {
        OHIP,
        [Display(Name="Out of Province")]
        OutofProvince,
        International
    }
}
