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
using MedicalOffice.CustomControllers;

namespace MedicalOffice.Controllers
{
    public class PatientController : ElephantController

    {
        private readonly MedicalOfficeContext _context;

        public PatientController(MedicalOfficeContext context)
        {
            _context = context;
        }

        // GET: Patient
        public async Task<IActionResult> Index(string? SearchString, int? DoctorID, 
            int? MedicalTrialID, int? ConditionID, int? page, int? pageSizeID, string? CoverageFilter,
            string? actionButton, string sortDirection = "asc", string sortField = "Patient"
)
        {
            //List of sort options.
            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Patient", "Age", "Coverage", "Doctor" };

            //Count the number of filters applied - start by assuming no filters
            ViewData["Filtering"] = "btn-outline-secondary";
            int numberFilters = 0;
            //Then in each "test" for filtering, add to the count of Filters applied

            PopulateDropDownLists(); //data for doctor and medicaltrial filter

            //Extra Select List for Conditions
            ViewData["ConditionID"] = new SelectList(_context
                .Conditions
                .OrderBy(c => c.ConditionName), "ID", "ConditionName");

            //SelectList for the Coverage Enum
            if (Enum.TryParse(CoverageFilter, out Coverage selectedCoverage))
            {
                ViewBag.CoverageSelectList = Coverage.OHIP.ToSelectList(selectedCoverage);
            }
            else
            {
                ViewBag.CoverageSelectList = Coverage.OHIP.ToSelectList(null);
            }


            var patients = _context.Patients
                .Include(p => p.Doctor)
                .Include(p => p.MedicalTrial)
                .Include(p => p.PatientConditions).ThenInclude(pc=>pc.Condition)
                .Include(p => p.PatientThumbnail)
                .AsNoTracking();

            //Add as many filters as needed
            if (DoctorID.HasValue)
            {
                patients = patients.Where(p => p.DoctorID == DoctorID);
                numberFilters++;
            }
            if (MedicalTrialID.HasValue)
            {
                patients = patients.Where(p => p.MedicalTrialID == MedicalTrialID);
                numberFilters++;
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                patients = patients.Where(p => p.LastName.ToUpper().Contains(SearchString.ToUpper())
                                       || p.FirstName.ToUpper().Contains(SearchString.ToUpper()));
                numberFilters++;
            }
            if (ConditionID.HasValue)
            {
                patients = patients.Where(p => p.PatientConditions.Any(c => c.ConditionID == ConditionID));
                numberFilters++;
            }
            if (!String.IsNullOrEmpty(CoverageFilter))
            {
                patients = patients.Where(p => p.Coverage == selectedCoverage);
                numberFilters++;
            }

            if (numberFilters != 0)
            {
                //Toggle the Open/Closed state of the collapse depending on if we are filtering
                ViewData["Filtering"] = " btn-danger";
                //Show how many filters have been applied
                ViewData["numberFilters"] = "(" + numberFilters.ToString()
                    + " Filter" + (numberFilters > 1 ? "s" : "") + " Applied)";

                //Keep the Bootstrap collapse open
                @ViewData["ShowFilter"] = " show";
            }

            //Before we sort, see if we have called for a change of filtering or sorting
            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted!
            {
                page = 1;//Reset page to start
                if (sortOptions.Contains(actionButton))//Change of sort is requested
                {
                    if (actionButton == sortField) //Reverse order on same field
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;//Sort by the button clicked
                }
            }

            //Now we know which field and direction to sort by
            if (sortField == "Age")
            {
                if (sortDirection == "asc")
                {
                    patients = patients
                        .OrderByDescending(p => p.DOB);
                }
                else
                {
                    patients = patients
                        .OrderBy(p => p.DOB);
                }
            }
            else if (sortField == "Coverage")
            {
                if (sortDirection == "asc")
                {
                    patients = patients
                        .OrderBy(p => p.Coverage)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    patients = patients
                        .OrderByDescending(p => p.Coverage)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
            }
            else if (sortField == "Doctor")
            {
                if (sortDirection == "asc")
                {
                    patients = patients
                        .OrderBy(p => p.Doctor.LastName)
                        .ThenBy(p => p.Doctor.FirstName)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    patients = patients
                        .OrderByDescending(p => p.Doctor.LastName)
                        .ThenByDescending(p => p.Doctor.FirstName)
                        .ThenBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
            }
            else //Sorting by Patient Name
            {
                if (sortDirection == "asc")
                {
                    patients = patients
                        .OrderBy(p => p.LastName)
                        .ThenBy(p => p.FirstName);
                }
                else
                {
                    patients = patients
                        .OrderByDescending(p => p.LastName)
                        .ThenByDescending(p => p.FirstName);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);
            var pagedData = await PaginatedList<Patient>.CreateAsync(patients.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
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
                .Include(p => p.PatientPhoto)
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
            "ExpYrVisits,Phone,Email,Coverage,MedicalTrialID,DoctorID")] Patient patient, string[] selectedOptions,
            IFormFile? thePicture)
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
                    await AddPicture(patient, thePicture);
                    _context.Add(patient);
                    await _context.SaveChangesAsync();

                    await _context.SaveChangesAsync();
                    //Send on to add appointments
                    return RedirectToAction("Index", "PatientAppointment", new { PatientID = patient.ID });
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
                .Include(p => p.PatientPhoto)
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
        public async Task<IActionResult> Edit(int id, string[] selectedOptions, Byte[] RowVersion,
            string? chkRemoveImage, IFormFile? thePicture)
        {
            var patientToUpdate = await _context.Patients
                .Include(p => p.PatientConditions).ThenInclude(p => p.Condition)
                .Include(p => p.PatientPhoto)
                .FirstOrDefaultAsync(p => p.ID == id);
            if (patientToUpdate == null)
            {
                return NotFound();
            }

            //Update the medical history
            UpdatePatientConditions(selectedOptions, patientToUpdate);

            //Put the original RowVersion value in the OriginalValues collection for the entity
            _context.Entry(patientToUpdate).Property("RowVersion").OriginalValue = RowVersion;

            if (await TryUpdateModelAsync<Patient>(patientToUpdate, "",
                p => p.OHIP, p => p.FirstName, p => p.MiddleName, p => p.LastName, p => p.DOB,
                p => p.ExpYrVisits, p => p.Phone, p => p.Email, p => p.Coverage, p => p.MedicalTrialID,p => p.DoctorID))
            {
                try
                {
                    //For the image
                    if (chkRemoveImage != null)
                    {
                        //If we are just deleting the two versions of the photo, we need to make sure the Change Tracker knows
                        //about them both so go get the Thumbnail since we did not include it.
                        patientToUpdate.PatientThumbnail = _context.PatientThumbnails.Where(p => p.PatientID == patientToUpdate.ID).FirstOrDefault();
                        //Then, setting them to null will cause them to be deleted from the database.
                        patientToUpdate.PatientPhoto = null;
                        patientToUpdate.PatientThumbnail = null;
                    }
                    else
                    {
                        await AddPicture(patientToUpdate, thePicture);
                    }

                    await _context.SaveChangesAsync();

                    await _context.SaveChangesAsync();
                    //Send on to add appointments
                    return RedirectToAction("Index", "PatientAppointment", new { PatientID = patientToUpdate.ID });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Patient)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError("",
                            "Unable to save changes. The Patient was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Patient)databaseEntry.ToObject();
                        if (databaseValues.FirstName != clientValues.FirstName)
                            ModelState.AddModelError("FirstName", "Current value: "
                                + databaseValues.FirstName);
                        if (databaseValues.MiddleName != clientValues.MiddleName)
                            ModelState.AddModelError("MiddleName", "Current value: "
                                + databaseValues.MiddleName);
                        if (databaseValues.LastName != clientValues.LastName)
                            ModelState.AddModelError("LastName", "Current value: "
                                + databaseValues.LastName);
                        if (databaseValues.OHIP != clientValues.OHIP)
                            ModelState.AddModelError("OHIP", "Current value: "
                                + databaseValues.OHIP);
                        if (databaseValues.DOB != clientValues.DOB)
                            ModelState.AddModelError("DOB", "Current value: "
                                + String.Format("{0:d}", databaseValues.DOB));
                        if (databaseValues.Phone != clientValues.Phone)
                            ModelState.AddModelError("Phone", "Current value: "
                                + databaseValues.PhoneFormatted);
                        if (databaseValues.Email != clientValues.Email)
                            ModelState.AddModelError("EMail", "Current value: "
                                + databaseValues.Email);
                        if (databaseValues.ExpYrVisits != clientValues.ExpYrVisits)
                            ModelState.AddModelError("ExpYrVisits", "Current value: "
                                + databaseValues.ExpYrVisits);
                        if (databaseValues.Coverage != clientValues.Coverage)
                            ModelState.AddModelError("Coverage", "Current value: "
                                + databaseValues.Coverage);
                        //For the foreign key, we need to go to the database to get the information to show
                        if (databaseValues.DoctorID != clientValues.DoctorID)
                        {
                            Doctor? databaseDoctor = await _context.Doctors.FirstOrDefaultAsync(i => i.ID == databaseValues.DoctorID);
                            ModelState.AddModelError("DoctorID", $"Current value: {databaseDoctor?.Summary}");
                        }
                        //A little extra work for the nullable foreign key.  No sense going to the database and asking for something
                        //we already know is not there.
                        if (databaseValues.MedicalTrialID != clientValues.MedicalTrialID)
                        {
                            if (databaseValues.MedicalTrialID.HasValue)
                            {
                                MedicalTrial? databaseMedicalTrial = await _context.MedicalTrials.FirstOrDefaultAsync(i => i.ID == databaseValues.MedicalTrialID);
                                ModelState.AddModelError("MedicalTrialID", $"Current value: {databaseMedicalTrial?.TrialName}");
                            }
                            else

                            {
                                ModelState.AddModelError("MedicalTrialID", $"Current value: None");
                            }
                        }
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                                + "was modified by another user after you received your values. The "
                                + "edit operation was canceled and the current values in the database "
                                + "have been displayed. If you still want to save your version of this record, click "
                                + "the Save button again. Otherwise click the 'Back to Patient List' hyperlink.");

                        //Final steps before redisplaying: Update RowVersion from the Database
                        //and remove the RowVersion error from the ModelState
                        patientToUpdate.RowVersion = databaseValues.RowVersion ?? Array.Empty<byte>();
                        ModelState.Remove("RowVersion");
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
            PopulateAssignedConditionData(patientToUpdate);
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
                .Include(p => p.PatientPhoto)
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
                .Include(p => p.PatientPhoto)
                .FirstOrDefaultAsync(m => m.ID == id);

            try
            {
                if (patient != null)
                {
                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();
                }
                var returnUrl = ViewData["returnURL"]?.ToString();
                if (string.IsNullOrEmpty(returnUrl))
                {
                    return RedirectToAction(nameof(Index));
                }
                return Redirect(returnUrl);
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

        private async Task AddPicture(Patient patient, IFormFile thePicture)
        {
            //Get the picture and save it with the Patient (2 sizes)
            if (thePicture != null)
            {
                string mimeType = thePicture.ContentType;
                long fileLength = thePicture.Length;
                if (!(mimeType == "" || fileLength == 0))//Looks like we have a file!!!
                {
                    if (mimeType.Contains("image"))
                    {
                        using var memoryStream = new MemoryStream();
                        await thePicture.CopyToAsync(memoryStream);
                        var pictureArray = memoryStream.ToArray();//Gives us the Byte[]

                        //Check if we are replacing or creating new
                        if (patient.PatientPhoto != null)
                        {
                            //We already have pictures so just replace the Byte[]
                            patient.PatientPhoto.Content = ResizeImage.ShrinkImageWebp(pictureArray, 500, 600);

                            //Get the Thumbnail so we can update it.  Remember we didn't include it
                            patient.PatientThumbnail = _context.PatientThumbnails.Where(p => p.PatientID == patient.ID).FirstOrDefault();
                            if (patient.PatientThumbnail != null)
                            {
                                patient.PatientThumbnail.Content = ResizeImage.ShrinkImageWebp(pictureArray, 75, 90);
                            }
                        }
                        else //No pictures saved so start new
                        {
                            patient.PatientPhoto = new PatientPhoto
                            {
                                Content = ResizeImage.ShrinkImageWebp(pictureArray, 500, 600),
                                MimeType = "image/webp"
                            };
                            patient.PatientThumbnail = new PatientThumbnail
                            {
                                Content = ResizeImage.ShrinkImageWebp(pictureArray, 75, 90),
                                MimeType = "image/webp"
                            };
                        }
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
