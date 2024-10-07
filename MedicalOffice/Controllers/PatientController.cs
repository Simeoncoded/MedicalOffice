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
        public async Task<IActionResult> Index(string? SearchString, int? DoctorID, int? MedicalTrialID)
        {
            PopulateDropDownLists(); //data for doctor and medicaltrial filter

            var patients = _context.Patients
                .Include(p => p.Doctor)
                .Include(p => p.MedicalTrial)
                .Include(p => p.PatientConditions).ThenInclude(pc=>pc.Condition)
                .AsNoTracking();

            //Add as many filters as needed
            if (DoctorID.HasValue)
            {
                patients = patients.Where(p => p.DoctorID == DoctorID);
            }
            if (MedicalTrialID.HasValue)
            {
                patients = patients.Where(p => p.MedicalTrialID == MedicalTrialID);
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                patients = patients.Where(p => p.LastName.ToUpper().Contains(SearchString.ToUpper())
                                       || p.FirstName.ToUpper().Contains(SearchString.ToUpper()));
            }


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
                .Include(p => p.MedicalTrial)
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
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
            Patient patient = new Patient();
            PopulateAssignedConditionData(patient); 

            PopulateDropDownLists();
            return View();
        }

        // POST: Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OHIP,FirstName,MiddleName,LastName,DOB," +
            "ExpYrVisits,Phone,Email,Coverage,MedicalTrialID,DoctorID")] Patient patient, string[] selectedOptions)
        {
            try
            {
                //Add the selected conditions
                if (selectedOptions != null)
                {
                    foreach (var condition in selectedOptions)
                    {
                        var conditionToAdd = new PatientCondition { PatientID = patient.ID, ConditionID = int.Parse(condition) };
                        patient.PatientConditions.Add(conditionToAdd);
                    }
                }

                if (ModelState.IsValid)
                {
                    _context.Add(patient);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (RetryLimitExceededException /* dex */)//This is a Transaction in the Database!
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. " +
                    "Try again, and if the problem persists, see your system administrator.");
            }
            catch (DbUpdateException dex)
            {
                string message = dex.GetBaseException().Message;
                if (message.Contains("UNIQUE") && message.Contains("Patients.OHIP"))
                {
                    ModelState.AddModelError("OHIP", "Unable to save changes. Remember, you cannot have duplicate OHIP numbers.");
                }
                else if (message.Contains("UNIQUE") && message.Contains("Patients.DOB"))
                {
                    ModelState.AddModelError("", "Unable to save changes. Remember, you cannot have duplicate Names and Date of Birth.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            PopulateAssignedConditionData(patient);
            PopulateDropDownLists(patient);
            return View(patient);
        }

        // GET: Patient/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(p => p.Condition)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (patient == null)
            {
                return NotFound();
            }
            PopulateAssignedConditionData(patient);
            PopulateDropDownLists();
            return View(patient);
        }

        // POST: Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string[] selectedOptions)
        {
            var patientToUpdate = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(p => p.Condition)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (patientToUpdate == null)
            {
                return NotFound();
            }

            //Update the medical history
            UpdatePatientConditions(selectedOptions, patientToUpdate);

            if (await TryUpdateModelAsync<Patient>(patientToUpdate, "",
                p => p.OHIP, p => p.FirstName, p => p.MiddleName, p => p.LastName, p => p.DOB,
                p => p.ExpYrVisits, p => p.Phone, p => p.Email, p => p.Coverage, p => p.MedicalTrialID,p => p.DoctorID))
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
                    string message = dex.GetBaseException().Message;
                    if (message.Contains("UNIQUE") && message.Contains("Patients.OHIP"))
                    {
                        ModelState.AddModelError("OHIP", "Unable to save changes. Remember, you cannot have duplicate OHIP numbers.");
                    }
                    else if (message.Contains("UNIQUE") && message.Contains("Patients.DOB"))
                    {
                        ModelState.AddModelError("", "Unable to save changes. Remember, you cannot have duplicate Names and Date of Birth.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    }
                }
            }

            PopulateDropDownLists(patientToUpdate);
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
                .Include(p => p.MedicalTrial)
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
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
                .Include (p => p.MedicalTrial)
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
                .FirstOrDefaultAsync(m => m.ID == id);

            try
            {
                if (patient != null)
                {
                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to delete record. Try again, and if the problem persists, contact your system administrator.");
            }

            return View(patient);
        }

        //This is a twist on the PopulateDropDownLists approach
        //  Create methods that return each SelectList separately
        //  and one method to put them all into ViewData.
        //This approach allows for AJAX requests to refresh
        //DDL Data at a later date.
        private SelectList DoctorSelectList(int? selectedId)
        {
            return new SelectList(_context.Doctors
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName), "ID", "FormalName", selectedId);
        }

        private SelectList MedicalTrialList(int? selectedId)
        {
            return new SelectList(_context
                .MedicalTrials
                .OrderBy(m => m.TrialName), "ID", "TrialName", selectedId);
        }

        private void PopulateDropDownLists(Patient? patient = null)
        {
            ViewData["DoctorID"] = DoctorSelectList(patient?.DoctorID);
            ViewData["MedicalTrialID"] = MedicalTrialList(patient?.MedicalTrialID);
        }

        /// <summary>
        /// Prepare a colleciton of check box ViewModel objects, one for each
        /// condition.  Set Assigned to True for those already in the Patient's
        /// medical history.
        /// </summary>
        /// <param name="patient">the Patient</param>
        private void PopulateAssignedConditionData(Patient patient)
        {
            //For this to work, you must have Included the PatientConditions 
            //in the Patient
            var allOptions = _context.Conditions;
            var currentOptionIDs = new HashSet<int>(patient.PatientConditions.Select(b => b.ConditionID));
            var checkBoxes = new List<CheckOptionVM>();
            foreach (var option in allOptions)
            {
                checkBoxes.Add(new CheckOptionVM
                {
                    ID = option.ID,
                    DisplayText = option.ConditionName,
                    Assigned = currentOptionIDs.Contains(option.ID)
                });
            }
            ViewData["ConditionOptions"] = checkBoxes;
        }

        /// <summary>
        /// Update the PatientConditions for the Patient to match
        /// the selected Check Boxes.
        /// </summary>
        /// <param name="selectedOptions">ID's of the selected options</param>
        /// <param name="patientToUpdate">the Patient</param>
        private void UpdatePatientConditions(string[] selectedOptions, Patient patientToUpdate)
        {
            if (selectedOptions == null)
            {
                //replace with a new empty collection
                patientToUpdate.PatientConditions = new List<PatientCondition>();
                return;
            }

            var selectedOptionsHS = new HashSet<string>(selectedOptions);
            var patientOptionsHS = new HashSet<int>
                (patientToUpdate.PatientConditions.Select(c => c.ConditionID));//IDs of the currently selected conditions
            foreach (var option in _context.Conditions)
            {
                if (selectedOptionsHS.Contains(option.ID.ToString())) //It is checked
                {
                    if (!patientOptionsHS.Contains(option.ID))  //but not currently in the history
                    {
                        patientToUpdate.PatientConditions.Add(new PatientCondition { PatientID = patientToUpdate.ID, ConditionID = option.ID });
                    }
                }
                else
                {
                    //Checkbox Not checked
                    if (patientOptionsHS.Contains(option.ID)) //but it is currently in the history - so remove it
                    {
                        PatientCondition conditionToRemove = patientToUpdate
                            .PatientConditions.SingleOrDefault(c => c.ConditionID == option.ID);
                        _context.Remove(conditionToRemove);
                    }
                }
            }
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.ID == id);
        }
    }
}
