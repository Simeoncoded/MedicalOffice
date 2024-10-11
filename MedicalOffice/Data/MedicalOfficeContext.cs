using MedicalOffice.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalOffice.Data
{
    public class MedicalOfficeContext : DbContext
    {
        //To give access to IHttpContextAccessor for Audit Data with IAuditable
        private readonly IHttpContextAccessor _httpContextAccessor;

        //Property to hold the UserName value
        public string UserName
        {
            get; private set;
        }

        public MedicalOfficeContext(DbContextOptions<MedicalOfficeContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            if (_httpContextAccessor.HttpContext != null)
            {
                //We have a HttpContext, but there might not be anyone Authenticated
                UserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Unknown";
            }
            else
            {
                //No HttpContext so seeding data
                UserName = "Seed Data";
            }
        }

        public MedicalOfficeContext(DbContextOptions<MedicalOfficeContext> options)
            : base(options)
        {
            _httpContextAccessor = null!;
            UserName = "Seed Data";
        }

        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<MedicalTrial> MedicalTrials { get; set; }
        public DbSet<PatientCondition> PatientConditions { get; set; }
        public DbSet<Condition> Conditions { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; }
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

            //Add this so we are prevented from deleting a Condition
            //that is in a Patients history of  PatientConditions.
            //Note: Allow Cascade Delete from Patient to PatientCondition
            modelBuilder.Entity<PatientCondition>()
                .HasOne(pc => pc.Condition)
                .WithMany(c => c.PatientConditions)
                .HasForeignKey(pc => pc.ConditionID)
                .OnDelete(DeleteBehavior.Restrict);

            //Add this so you don't get Cascade Delete
            modelBuilder.Entity<Specialty>()
                .HasMany<DoctorSpecialty>(p => p.DoctorSpecialties)
                .WithOne(c => c.Specialty)
                .HasForeignKey(c => c.SpecialtyID)
                .OnDelete(DeleteBehavior.Restrict);



            //Add a unique index to the OHIP Number
            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.OHIP)
                .IsUnique();

            //Many to Many Intersection
            modelBuilder.Entity<PatientCondition>()
            .HasKey(t => new { t.ConditionID, t.PatientID });

            //Many to Many Doctor Specialty Primary Key
            modelBuilder.Entity<DoctorSpecialty>()
            .HasKey(t => new { t.DoctorID, t.SpecialtyID });


            //To deal with multiple births among our patients
            //add a unique index to the combination
            //of DOB, Last and First Names
            modelBuilder.Entity<Patient>()
            .HasIndex(p => new { p.DOB, p.LastName, p.FirstName}).
            IsUnique();

        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.Entity is IAuditable trackable)
                {
                    var now = DateTime.UtcNow;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;

                        case EntityState.Added:
                            trackable.CreatedOn = now;
                            trackable.CreatedBy = UserName;
                            trackable.UpdatedOn = now;
                            trackable.UpdatedBy = UserName;
                            break;
                    }
                }
            }
        }
    }

}
