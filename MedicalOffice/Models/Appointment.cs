using System.ComponentModel.DataAnnotations;

namespace MedicalOffice.Models
{
    public class Appointment : IValidatableObject
    {
        public int ID { get; set; }

        #region Summary Properties

        [Display(Name = "Date")]
        public string StartDateSummary
        {
            get
            {
                return StartTime.ToString("yyyy-MM-dd");
            }
        }
        [Display(Name = "Start")]
        public string StartTimeSummary
        {
            get
            {
                return StartTime.ToString("h:mm tt");
            }
        }

        [Display(Name = "End")]
        public string EndTimeSummary
        {
            get
            {
                if (EndTime == null)
                {
                    return "Unknown";
                }
                else
                {
                    string endtime = EndTime.GetValueOrDefault().ToString("h:mm tt");
                    TimeSpan difference = ((TimeSpan)(EndTime - StartTime));
                    int days = difference.Days;
                    if (days > 0)
                    {
                        return endtime + " (" + days + " day" + (days > 1 ? "s" : "") + " later)";
                    }
                    else
                    {
                        return endtime;
                    }
                }
            }
        }
        [Display(Name = "Duration")]
        public string DurationSummary
        {
            get
            {
                if (EndTime == null)
                {
                    return "";
                }
                else
                {
                    TimeSpan d = ((TimeSpan)(EndTime - StartTime));
                    string duration = "";
                    if (d.Minutes > 0) //Show the minutes if there are any
                    {
                        duration = d.Minutes.ToString() + " min";
                    }
                    if (d.Hours > 0) //Show the hours if there are any
                    {
                        duration = d.Hours.ToString() + " hr" + (d.Hours > 1 ? "s" : "")
                            + (d.Minutes > 0 ? ", " + duration : ""); //Put a ", " between hours and minutes if there are both
                    }
                    if (d.Days > 0) //Show the days if there are any
                    {
                        duration = d.Days.ToString() + " day" + (d.Days > 1 ? "s" : "")
                            + (d.Hours > 0 || d.Minutes > 0 ? ", " + duration : ""); //Put a ", " between days and hours/minutes if there are any
                    }
                    return duration;
                }
            }
        }
        #endregion

        [Required(ErrorMessage = "You must enter a start date and time for the appointment.")]
        [Display(Name = "Start")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Display(Name = "End")]
        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        [Required(ErrorMessage = "You must enter some notes for the appointment.")]
        [StringLength(2000, ErrorMessage = "Only 2000 characters for notes.")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; } = "";

        [Required(ErrorMessage = "You must enter an amount for the extra fee.")]
        [Display(Name = "Extra Fee")]
        [DataType(DataType.Currency)]
        public double ExtraFee { get; set; } = 20d;

        [Required(ErrorMessage = "You must select a Primary Care Physician.")]
        [Display(Name = "Doctor")]
        public int DoctorID { get; set; }

        [Display(Name = "Doctor")]
        public Doctor? Doctor { get; set; }

        [Required(ErrorMessage = "You must select the Patient.")]
        [Display(Name = "Patient")]
        public int PatientID { get; set; }

        [Display(Name = "Patient")]
        public Patient? Patient { get; set; }

        //Note: Reason is not required
        [Display(Name = "Reason for Appointment")]
        public int? AppointmentReasonID { get; set; }

        [Display(Name = "Reason for Appointment")]
        public AppointmentReason? AppointmentReason { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime < StartTime)
            {
                yield return new ValidationResult("Appointment cannot end before it starts.", new[] { "EndTime" });
            }
        }
    }
}
