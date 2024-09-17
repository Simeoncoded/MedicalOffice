using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedicalOffice.Data;
using MedicalOffice.Models;

namespace MedicalOffice.Controllers
{
    public class PatientController : Controller
    {
        private readonly MedicalOfficeContext _context;

        public PatientController(MedicalOfficeContext context)
        {
            _context = context;
        }

        // GET: Patient
        public async Task<IActionResult> Index()
        {
            var patients = _context.Patients
                .Include(p => p.Doctor)
                .AsNoTracking();

            //var patients = from p in _context.Patients
            //               join d in _context.Doctors on p.DoctorID equals d.ID
            //               select new Patient
            //               {
            //                   ID = p.ID,
            //                   OHIP = p.OHIP,
            //                   FirstName = p.FirstName,
            //                   MiddleName = p.MiddleName,
            //                   LastName = p.LastName,
            //                   DOB = p.DOB,
            //                   ExpYrVisits = p.ExpYrVisits,
            //                   Phone = p.Phone,
            //                   Email = p.Email,
            //                   DoctorID = p.DoctorID,
            //                   Doctor = d
            //               };

            return View(await patients.ToListAsync());
        }

        // GET: Patient/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patient/Create
        public IActionResult Create()
        {
            PopulateDropDownLists();
            //ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName");
            return View();
        }

        // POST: Patient/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OHIP,FirstName,MiddleName,LastName,DOB,ExpYrVisits,Phone,Email,Coverage,DoctorID")] Patient patient)
        {

            //wrapping in try catch for database exceptions
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(patient);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException dex)
            {
                if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed: Patients.OHIP"))
                {
                    ModelState.AddModelError("OHIP", "Unable to save changes. Remember, you cannot have duplicate OHIP numbers.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

          
            PopulateDropDownLists(patient);
           // ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName", patient.DoctorID);
            return View(patient);
        }

        // GET: Patient/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }
            PopulateDropDownLists();
            //ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName", patient.DoctorID);
            return View(patient);
        }

        // POST: Patient/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {

            //Go get the patient to update
            var patientToUpdate = await _context.Patients.FirstOrDefaultAsync(p => p.ID == id);

            //check that you got it or exit with a not found error

            if (patientToUpdate == null)
            {
                return NotFound();
            }

            //Try updating it with the values posted

            if (await TryUpdateModelAsync<Patient>(patientToUpdate, "",
             p => p.OHIP, p => p.FirstName, p => p.MiddleName, p => p.LastName, p => p.DOB,
             p => p.ExpYrVisits, p => p.Phone, p => p.Email, p => p.Coverage, p => p.DoctorID))

                {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patientToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException dex)
                {
                    if (dex.GetBaseException().Message.Contains("UNIQUE constraint failed: Patients.OHIP"))
                    {
                        ModelState.AddModelError("OHIP", "Unable to save changes. Remember, you cannot have duplicate OHIP numbers.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }

            }
            PopulateDropDownLists(patientToUpdate);
            //ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName", patient.DoctorID);
            return View(patientToUpdate);
        }

        // GET: Patient/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Doctor)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            var patient = await _context.Patients
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.ID == id);

            try
            {

                if (patient != null)
                {
                    _context.Patients.Remove(patient);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                //Note: there is really no reason a delete should fail if you can "talk" to the database.
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists contact your system administrator");
            }
            return View(patient);


        }

        private void PopulateDropDownLists(Patient? patient = null)
        {
            var dQuery = from d in _context.Doctors
                         orderby d.LastName, d.FirstName
                         select d;
            ViewData["DoctorID"] = new SelectList(dQuery, "ID", "FormalName", patient?.DoctorID);
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.ID == id);
        }
    }
}
