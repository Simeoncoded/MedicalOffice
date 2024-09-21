using MedicalOffice.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalOffice.Data
{
    public class MedicalOfficeContext : DbContext
    {
        public MedicalOfficeContext(DbContextOptions<MedicalOfficeContext> options) 
            : base(options) 
        { 
        }

        public DbSet<Doctor> Doctors { get; set; }

        public DbSet<Patient> Patients { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //prevent cascade delete from doctor to patient
            //so we are prevented from deleting a Doctor with
            //Patients assigned
            modelBuilder.Entity<Doctor>()
                    .HasMany<Patient>(d => d.Patients)
                    .WithOne(p  => p.Doctor)
                    .HasForeignKey(p => p.DoctorID)
                    .OnDelete(DeleteBehavior.Restrict);

            //Add a unique index to the OHIP Number
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.OHIP)
                .IsUnique();
    
            //To deal with multiple births among our patients
            //add a unique index to t
        }
    }

}
