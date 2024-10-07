using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedicalOffice.Data;
using MedicalOffice.Models;
using MedicalOffice.ViewModels;
using Microsoft.EntityFrameworkCore.Storage;
using MedicalOffice.Utilities;

namespace MedicalOffice.Controllers
{
    public class DoctorController : Controller
    {
        private readonly MedicalOfficeContext _context;

        public DoctorController(MedicalOfficeContext context)
        {
            _context = context;
        }

        // GET: Doctor
        public async Task<IActionResult> Index(int? page)
        {
            var doctors = _context.Doctors
               .Include(d => d.DoctorSpecialties).ThenInclude(d => d.Specialty)
               .AsNoTracking();

            int pageSize = 10;//Change as required
            var pagedData = await PaginatedList<Doctor>.CreateAsync(doctors.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: Doctor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Patients)
                 .Include(d => d.DoctorSpecialties).ThenInclude(d => d.Specialty)
                 .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // GET: Doctor/Create
        public IActionResult Create()
        {
            Doctor doctor = new Doctor();
            PopulateAssignedSpecialtyData(doctor);
            return View();
        }

        // POST: Doctor/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FirstName,MiddleName,LastName")] Doctor doctor, string[] selectedOptions)
        {

            try
            {
                UpdateDoctorSpecialties(selectedOptions, doctor);
                if (ModelState.IsValid)
                {
                    _context.Add(doctor);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            catch (RetryLimitExceededException /* dex */)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException)
            {

                ModelState.AddModelError("", "Unable to save changes. Try again, and if problem persists please contact your system administrator");
            }

            //Validation Error so give the user another chance
            PopulateAssignedSpecialtyData(doctor);
         
            return View(doctor);
        }

       

        // GET: Doctor/Edit/5
        public async Task<IActionResult> Edit(int? id) 
        {
            if (id == null)
            {
                return NotFound();
            }


            var doctor = await _context.Doctors
               .Include(d => d.DoctorSpecialties).ThenInclude(d => d.Specialty)
               .FirstOrDefaultAsync(d => d.ID == id);
            if (doctor == null)
            {
                return NotFound();
            }
            PopulateAssignedSpecialtyData(doctor);
            return View(doctor);
        }

       

        // POST: Doctor/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string[] selectedOptions)
        {
            //Go get the Doctor to update
            //Go get the Doctor to update
            var doctorToUpdate = await _context.Doctors
                .Include(d => d.DoctorSpecialties).ThenInclude(d => d.Specialty)
                .FirstOrDefaultAsync(p => p.ID == id);

            //check that you got it or exit with a not found error
            if (doctorToUpdate == null)
            {
                return NotFound();
            }

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Doctor>(doctorToUpdate, "",
                d => d.FirstName, d => d.MiddleName, d => d.LastName))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    //Instead of going back to the Index, why not show the revised
                    //version in full detail?
                    return RedirectToAction("Details", new { doctorToUpdate.ID });
                }
                catch (RetryLimitExceededException /* dex */)
                {
                    ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again, and if the problem persists, see your system administrator.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctorToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if problem persists please contact your system administrator");
                }
            }
            PopulateAssignedSpecialtyData(doctorToUpdate);
            return View(doctorToUpdate);
        }

        // GET: Doctor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                 .Include(d => d.DoctorSpecialties).ThenInclude(d => d.Specialty)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var doctor = await _context.Doctors
                 .Include(d => d.DoctorSpecialties).ThenInclude(d => d.Specialty)
                 .FirstOrDefaultAsync(m => m.ID == id);
             
            try
            {
                
                if (doctor != null) 
                {
                    _context.Doctors.Remove(doctor);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dex)
            {

                if (dex.GetBaseException().Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ModelState.AddModelError("", "Unable to Delete Doctor. Remember, you cannot delete a Doctor that has patients assigned");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if problem persists please contact your system administrator");
                }
            }
            return View(doctor); 
        }

        private void PopulateAssignedSpecialtyData(Doctor doctor)
        {
            //For this to work, you must have Included the child collection in the parent object
            var allOptions = _context.Specialties;
            var currentOptionsHS = new HashSet<int>(doctor.DoctorSpecialties.Select(b => b.SpecialtyID));
            //Instead of one list with a boolean, we will make two lists
            var selected = new List<ListOptionVM>();
            var available = new List<ListOptionVM>();
            foreach (var s in allOptions)
            {
                if (currentOptionsHS.Contains(s.ID))
                {
                    selected.Add(new ListOptionVM
                    {
                        ID = s.ID,
                        DisplayText = s.SpecialtyName
                    });
                }
                else
                {
                    available.Add(new ListOptionVM
                    {
                        ID = s.ID,
                        DisplayText = s.SpecialtyName
                    });
                }
            }

            ViewData["selOpts"] = new MultiSelectList(selected.OrderBy(s => s.DisplayText), "ID", "DisplayText");
            ViewData["availOpts"] = new MultiSelectList(available.OrderBy(s => s.DisplayText), "ID", "DisplayText");
        }
        private void UpdateDoctorSpecialties(string[] selectedOptions, Doctor doctorToUpdate)
        {
            if (selectedOptions == null)
            {
                doctorToUpdate.DoctorSpecialties = new List<DoctorSpecialty>();
                return;
            }

            var selectedOptionsHS = new HashSet<string>(selectedOptions);
            var currentOptionsHS = new HashSet<int>(doctorToUpdate.DoctorSpecialties.Select(b => b.SpecialtyID));
            foreach (var s in _context.Specialties)
            {
                if (selectedOptionsHS.Contains(s.ID.ToString()))//it is selected
                {
                    if (!currentOptionsHS.Contains(s.ID))//but not currently in the Doctor's collection - Add it!
                    {
                        doctorToUpdate.DoctorSpecialties.Add(new DoctorSpecialty
                        {
                            SpecialtyID = s.ID,
                            DoctorID = doctorToUpdate.ID
                        });
                    }
                }
                else //not selected
                {
                    if (currentOptionsHS.Contains(s.ID))//but is currently in the Doctor's collection - Remove it!
                    {
                        DoctorSpecialty? specToRemove = doctorToUpdate.DoctorSpecialties.FirstOrDefault(d => d.SpecialtyID == s.ID);
                        if (specToRemove != null)
                        {
                            _context.Remove(specToRemove);
                        }
                    }
                }
            }
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.ID == id);
        }
    }
}
