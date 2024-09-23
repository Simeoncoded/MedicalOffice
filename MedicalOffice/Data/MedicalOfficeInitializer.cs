using MedicalOffice.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Drawing;

namespace MedicalOffice.Data
{
    public static class MedicalOfficeInitializer
    {
        /// <summary>
        /// Prepares the Database and seeds data as required
        /// </summary>
        /// <param name="serviceProvider">DI Container</param>
        /// <param name="DeleteDatabase">Delete the database and start from scratch</param>
        /// <param name="UseMigrations">Use Migrations or EnsureCreated</param>
        /// <param name="SeedSampleData">Add optional sample data</param>
        public static void Initialize(IServiceProvider serviceProvider,
            bool DeleteDatabase = false, bool UseMigrations = true, bool SeedSampleData = true)
        {
           
            using (var context = new MedicalOfficeContext(
                serviceProvider.GetRequiredService<DbContextOptions<MedicalOfficeContext>>()))
            {
                //Refresh the database as per the parameter options
                #region Prepare the Database
                try
                {
                    if (UseMigrations)
                    {
                        if (DeleteDatabase)
                        {
                            context.Database.EnsureDeleted(); //Delete the existing version 
                        }
                        context.Database.Migrate(); //Apply all migrations
                    }
                    else
                    {
                        if (DeleteDatabase)
                        {
                            context.Database.EnsureDeleted(); //Delete the existing version 
                            context.Database.EnsureCreated(); //Create and update the database as per the Model
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.GetBaseException().Message);
                }
                #endregion

                //Seed data needed for production and during development
                    #region Seed Required Data
                try
                {
                    //Add some Medical Trials
                    if (!context.MedicalTrials.Any())
                    {
                        context.MedicalTrials.AddRange(
                         new MedicalTrial
                         {
                             TrialName = "UOT - Lukemia Treatment"
                         }, new MedicalTrial
                         {
                             TrialName = "HyGIeaCare Center -  Microbiome Analysis of Constipated Versus Non-constipation Patients"
                         }, new MedicalTrial
                         {
                             TrialName = "Sunnybrook -  Trial of BNT162b2 versus mRNA-1273 COVID-19 Vaccine Boosters in Chronic Kidney Disease and Dialysis Patients With Poor Humoral Response following COVID-19 Vaccination"
                         }, new MedicalTrial
                         {
                             TrialName = "Altimmune -  Evaluate the Effect of Position and Duration on the Safety and Immunogenicity of Intranasal AdCOVID Administration"
                         }, new MedicalTrial
                         {
                             TrialName = "TUK - Hair Loss Treatment"
                         });
                        context.SaveChanges();
                    }
                    //Conditions - Needed for production and during development
                    if (!context.Conditions.Any())
                    {
                        string[] conditions = new string[] { "Asthma", "Cancer", "Cardiac disease", "Diabetes", "Hypertension", "Seizure disorder", "Circulation problems", "Bleeding disorder", "Thyroid condition", "Liver Disease", "Measles", "Mumps" };

                        foreach (string condition in conditions)
                        {
                            Condition c = new Condition
                            {
                                ConditionName = condition
                            };
                            context.Conditions.Add(c);
                        }
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.GetBaseException().Message);
                }
                #endregion

                //Seed meaningless data as sample data during development
                #region Seed Sample Data 
                if (SeedSampleData)
                {
                    //To randomly generate data
                    Random random = new Random();

                    //Seed a few specific Doctors and Patients. We will add more with random values later,
                    //but it can be useful to know we will have some specific records in the sample data.
                    try
                    {
                        // Seed Doctors first since we can't have Patients without Doctors.
                        if (!context.Doctors.Any())
                        {
                            context.Doctors.AddRange(
                            new Doctor
                            {
                                FirstName = "Gregory",
                                MiddleName = "A",
                                LastName = "House"
                            },
                            new Doctor
                            {
                                FirstName = "Doogie",
                                MiddleName = "R",
                                LastName = "Houser"
                            },
                            new Doctor
                            {
                                FirstName = "Charles",
                                LastName = "Xavier"
                            });
                            context.SaveChanges();
                        }
                        // Seed Patients if there aren't any.
                        if (!context.Patients.Any())
                        {
                            context.Patients.AddRange(
                            new Patient
                            {
                                FirstName = "Fred",
                                MiddleName = "Reginald",
                                LastName = "Flintstone",
                                OHIP = "1231231234",
                                DOB = DateTime.Parse("1955-09-01"),
                                ExpYrVisits = 6,
                                Phone = "9055551212",
                                Email = "fflintstone@outlook.com",
                                DoctorID = context.Doctors.FirstOrDefault(static d => d.FirstName == "Gregory" && d.LastName == "House").ID
                            },
                            new Patient
                            {
                                FirstName = "Wilma",
                                MiddleName = "Jane",
                                LastName = "Flintstone",
                                OHIP = "1321321324",
                                DOB = DateTime.Parse("1964-04-23"),
                                ExpYrVisits = 2,
                                Phone = "9055551212",
                                Email = "wflintstone@outlook.com",
                                DoctorID = context.Doctors.FirstOrDefault(d => d.FirstName == "Gregory" && d.LastName == "House").ID
                            },
                            new Patient
                            {
                                FirstName = "Barney",
                                LastName = "Rubble",
                                OHIP = "3213213214",
                                DOB = DateTime.Parse("1964-02-22"),
                                ExpYrVisits = 2,
                                Phone = "9055551213",
                                Email = "brubble@outlook.com",
                                Coverage = Coverage.OHIP,
                                DoctorID = context.Doctors.FirstOrDefault(d => d.FirstName == "Doogie" && d.LastName == "Houser").ID
                            },
                            new Patient //Note that I removed the assignment of an OHIP since we are setting Coverage.OutOfProvince
                            {
                                FirstName = "Jane",
                                MiddleName = "Samantha",
                                LastName = "Doe",
                                ExpYrVisits = 2,
                                Phone = "9055551234",
                                Email = "jdoe@outlook.com",
                                Coverage = Coverage.OutofProvince,
                                DoctorID = context.Doctors.FirstOrDefault(d => d.FirstName == "Charles" && d.LastName == "Xavier").ID
                            });
                            context.SaveChanges();
                        }
                        if (!context.PatientConditions.Any())
                        {
                            context.PatientConditions.AddRange(
                                new PatientCondition
                                {
                                    ConditionID = context.Conditions.FirstOrDefault(c => c.ConditionName == "Cancer").ID,
                                    PatientID = context.Patients.FirstOrDefault(p => p.LastName == "Flintstone" && p.FirstName == "Fred").ID
                                },
                                new PatientCondition
                                {
                                    ConditionID = context.Conditions.FirstOrDefault(c => c.ConditionName == "Cardiac disease").ID,
                                    PatientID = context.Patients.FirstOrDefault(p => p.LastName == "Flintstone" && p.FirstName == "Fred").ID
                                },
                                new PatientCondition
                                {
                                    ConditionID = context.Conditions.FirstOrDefault(c => c.ConditionName == "Diabetes").ID,
                                    PatientID = context.Patients.FirstOrDefault(p => p.LastName == "Flintstone" && p.FirstName == "Wilma").ID
                                });
                            context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.GetBaseException().Message);
                    }

                    //Leave those in place but add more using random values
                    try
                    {
                        //Add more Doctors
                        if (context.Doctors.Count() < 4)//Don't add a second time
                        {
                            string[] firstNames = new string[] { "Woodstock", "Violet", "Charlie", "Lucy", "Linus", "Franklin", "Marcie", "Schroeder" };
                            string[] lastNames = new string[] { "Hightower", "Broomspun", "Jones" };

                            //Loop through names and add more
                            foreach (string lastName in lastNames)
                            {
                                foreach (string firstname in firstNames)
                                {
                                    //Construct some details
                                    Doctor a = new Doctor()
                                    {
                                        FirstName = firstname,
                                        LastName = lastName,
                                        //Take second character of the last name and make it the middle name
                                        MiddleName = lastName[1].ToString().ToUpper(),
                                    };
                                    context.Doctors.Add(a);
                                }
                            }
                            context.SaveChanges();
                        }

                        //So we can gererate random data, create collections of the primary keys
                        int[] doctorIDs = context.Doctors.Select(a => a.ID).ToArray();
                        int doctorIDCount = doctorIDs.Length;// Why does this help efficiency?
                        int[] medicalTrialIDs = context.MedicalTrials.Select(a => a.ID).ToArray();
                        int medicalTrialIDCount = medicalTrialIDs.Length;

                        //Add more Patients.  Now it gets more interesting because we
                        //have Foreign Keys to worry about
                        //and more complicated property values to generate
                        if (context.Patients.Count() < 5)
                        {
                            string[] firstNames = new string[] { "Lyric", "Antoinette", "Kendal", "Vivian", "Ruth", "Jamison", "Emilia", "Natalee", "Yadiel", "Jakayla", "Lukas", "Moses", "Kyler", "Karla", "Chanel", "Tyler", "Camilla", "Quintin", "Braden", "Clarence" };
                            string[] lastNames = new string[] { "Watts", "Randall", "Arias", "Weber", "Stone", "Carlson", "Robles", "Frederick", "Parker", "Morris", "Soto", "Bruce", "Orozco", "Boyer", "Burns", "Cobb", "Blankenship", "Houston", "Estes", "Atkins", "Miranda", "Zuniga", "Ward", "Mayo", "Costa", "Reeves", "Anthony", "Cook", "Krueger", "Crane", "Watts", "Little", "Henderson", "Bishop" };
                            int firstNameCount = firstNames.Length;

                            // Birthdate for randomly produced Patients 
                            // We will subtract a random number of days from today
                            DateTime startDOB = DateTime.Today;// More efficiency?
                            int counter = 1; //Used to help add some Patients to Medical Trials

                            foreach (string lastName in lastNames)
                            {
                                //Choose a random HashSet of 5 (Unique) first names
                                HashSet<string> selectedFirstNames = new HashSet<string>();
                                while (selectedFirstNames.Count() < 5)
                                {
                                    selectedFirstNames.Add(firstNames[random.Next(firstNameCount)]);
                                }

                                foreach (string firstName in selectedFirstNames)
                                {
                                    //Construct some Patient details
                                    Patient patient = new Patient()
                                    {
                                        FirstName = firstName,
                                        LastName = lastName,
                                        MiddleName = lastName[1].ToString().ToUpper(),
                                        OHIP = random.Next(2, 9).ToString() + random.Next(213214131, 989898989).ToString(),
                                        Email = (firstName.Substring(0, 2) + lastName + random.Next(11, 111).ToString() + "@outlook.com").ToLower(),
                                        Phone = random.Next(2, 10).ToString() + random.Next(213214131, 989898989).ToString(),
                                        ExpYrVisits = (byte)random.Next(2, 12),
                                        DOB = startDOB.AddDays(-random.Next(60, 34675)),
                                        DoctorID = doctorIDs[random.Next(doctorIDCount)]
                                    };
                                    if (counter % 3 == 0)//Every third Patient gets assigned to a Medical Trial
                                    {
                                        patient.MedicalTrialID = medicalTrialIDs[random.Next(medicalTrialIDCount)];
                                    }
                                    counter++;
                                    context.Patients.Add(patient);
                                    try
                                    {
                                        //Could be a duplicate OHIP or combination of DOD, First and Last Name
                                        context.SaveChanges();
                                    }
                                    catch (Exception)
                                    {
                                        //Failed so remove it and go on to the next.
                                        //If you don't remove it from the context it
                                        //will keep trying to save it each time you 
                                        //call .SaveChanges() the the save process will stop
                                        //and prevent any other records in the que from getting saved.
                                        context.Patients.Remove(patient);
                                    }
                                }
                            }
                            //Since we didn't guarantee that evey Doctor had
                            //at least one Patient assigned, let's remove Doctors
                            //without any Patients.  We could do this other ways, but it
                            //gives a chance to show how to execute 
                            //raw SQL through our DbContext.
                            string cmd = "DELETE FROM Doctors WHERE NOT EXISTS(SELECT 1 FROM Patients WHERE Doctors.Id = Patients.DoctorID)";
                            context.Database.ExecuteSqlRaw(cmd);
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.GetBaseException().Message);
                    }
                }

                #endregion

            }
        }
        }
    }
