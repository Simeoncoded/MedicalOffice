using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedicalOffice.Data;
using MedicalOffice.Models;
using MedicalOffice.CustomControllers;
using MedicalOffice.Utilities;

namespace MedicalOffice.Controllers
{
    public class PatientAppointmentController : ElephantController
    {
        private readonly MedicalOfficeContext _context;

        public PatientAppointmentController(MedicalOfficeContext context)
        {
            _context = context;
        }

        // GET: PatientAppointment
        // GET: PatientAppointment
        public async Task<IActionResult> Index(int? PatientID, int? page, int? pageSizeID, int? AppointmentReasonID, string actionButton,
            string SearchString, string sortDirection = "desc", string sortField = "Appointment")
        {
            //Get the URL with the last filter, sort and page parameters from THE PATIENTS Index View
            ViewData["returnURL"] = MaintainURL.ReturnURL(HttpContext, "Patient");

            if (!PatientID.HasValue)
            {
                //Go back to the proper return URL for the Patients controller
                return Redirect(ViewData["returnURL"].ToString());
            }

            PopulateDropDownLists();

            //Count the number of filters applied - start by assuming no filters
            ViewData["Filtering"] = "btn-outline-secondary";
            int numberFilters = 0;
            //Then in each "test" for filtering, add to the count of Filters applied

            //NOTE: make sure this array has matching values to the column headings
            string[] sortOptions = new[] { "Appointment", "Appt. Reason", "Extra Fees" };

            var appts = from a in _context.Appointments
                        .Include(a => a.AppointmentReason)
                        .Include(a => a.Patient)
                        .Include(a => a.Doctor)
                        where a.PatientID == PatientID.GetValueOrDefault()
                        select a;

            if (AppointmentReasonID.HasValue)
            {
                appts = appts.Where(p => p.AppointmentReasonID == AppointmentReasonID);
                numberFilters++;
            }
            if (!String.IsNullOrEmpty(SearchString))
            {
                appts = appts.Where(p => p.Notes.ToUpper().Contains(SearchString.ToUpper()));
                numberFilters++;
            }
            //Give feedback about the state of the filters
            if (numberFilters != 0)
            {
                //Toggle the Open/Closed state of the collapse depending on if we are filtering
                ViewData["Filtering"] = " btn-danger";
                //Show how many filters have been applied
                ViewData["numberFilters"] = "(" + numberFilters.ToString()
                    + " Filter" + (numberFilters > 1 ? "s" : "") + " Applied)";
                //Keep the Bootstrap collapse open
                //@ViewData["ShowFilter"] = " show";
            }

            //Before we sort, see if we have called for a change of filtering or sorting
            if (!String.IsNullOrEmpty(actionButton)) //Form Submitted so lets sort!
            {
                page = 1;//Reset back to first page when sorting or filtering

                if (sortOptions.Contains(actionButton))//Change of sort is requested
                {
                    if (actionButton == sortField) //Reverse order on same field
                    {
                        sortDirection = sortDirection == "asc" ? "desc" : "asc";
                    }
                    sortField = actionButton;//Sort by the button clicked
                }
            }
            //Now we know which field and direction to sort by.
            if (sortField == "Appt. Reason")
            {
                if (sortDirection == "asc")
                {
                    appts = appts
                        .OrderBy(p => p.AppointmentReason.ReasonName);
                }
                else
                {
                    appts = appts
                        .OrderByDescending(p => p.AppointmentReason.ReasonName);
                }
            }
            else if (sortField == "Extra Fees")
            {
                if (sortDirection == "asc")
                {
                    appts = appts
                        .OrderBy(p => p.ExtraFee);
                }
                else
                {
                    appts = appts
                        .OrderByDescending(p => p.ExtraFee);
                }
            }
            else //Appointment Date
            {
                if (sortDirection == "asc")
                {
                    appts = appts
                        .OrderByDescending(p => p.StartTime);
                }
                else
                {
                    appts = appts
                        .OrderBy(p => p.StartTime);
                }
            }
            //Set sort for next time
            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            //Now get the MASTER record, the patient, so it can be displayed at the top of the screen
            Patient? patient = await _context.Patients
                .Include(p => p.Doctor)
                .Include(p => p.MedicalTrial)
                .Include(p => p.PatientThumbnail)
                .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
                .Where(p => p.ID == PatientID.GetValueOrDefault())
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.Patient = patient;

            //Handle Paging
            int pageSize = PageSizeHelper.SetPageSize(HttpContext, pageSizeID, ControllerName());
            ViewData["pageSizeID"] = PageSizeHelper.PageSizeList(pageSize);

            var pagedData = await PaginatedList<Appointment>.CreateAsync(appts.AsNoTracking(), page ?? 1, pageSize);

            return View(pagedData);
        }

        // GET: PatientAppointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.AppointmentReason)
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: PatientAppointment/Create
        public IActionResult Create()
        {
            ViewData["AppointmentReasonID"] = new SelectList(_context.AppointmentReasons, "ID", "ReasonName");
            ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName");
            ViewData["PatientID"] = new SelectList(_context.Patients, "ID", "FirstName");
            return View();
        }

        // POST: PatientAppointment/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,StartTime,EndTime,Notes,ExtraFee,DoctorID,PatientID,AppointmentReasonID")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentReasonID"] = new SelectList(_context.AppointmentReasons, "ID", "ReasonName", appointment.AppointmentReasonID);
            ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName", appointment.DoctorID);
            ViewData["PatientID"] = new SelectList(_context.Patients, "ID", "FirstName", appointment.PatientID);
            return View(appointment);
        }

        // GET: PatientAppointment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }
            ViewData["AppointmentReasonID"] = new SelectList(_context.AppointmentReasons, "ID", "ReasonName", appointment.AppointmentReasonID);
            ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName", appointment.DoctorID);
            ViewData["PatientID"] = new SelectList(_context.Patients, "ID", "FirstName", appointment.PatientID);
            return View(appointment);
        }

        // POST: PatientAppointment/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,StartTime,EndTime,Notes,ExtraFee,DoctorID,PatientID,AppointmentReasonID")] Appointment appointment)
        {
            if (id != appointment.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppointmentReasonID"] = new SelectList(_context.AppointmentReasons, "ID", "ReasonName", appointment.AppointmentReasonID);
            ViewData["DoctorID"] = new SelectList(_context.Doctors, "ID", "FirstName", appointment.DoctorID);
            ViewData["PatientID"] = new SelectList(_context.Patients, "ID", "FirstName", appointment.PatientID);
            return View(appointment);
        }

        // GET: PatientAppointment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.AppointmentReason)
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: PatientAppointment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private SelectList AppointmentReasonSelectList(int? id)
        {
            var dQuery = from d in _context.AppointmentReasons
                         orderby d.ReasonName
                         select d;
            return new SelectList(dQuery, "ID", "ReasonName", id);
        }
        private SelectList DoctorSelectList(int? id)
        {
            var dQuery = from d in _context.Doctors
                         orderby d.LastName, d.FirstName
                         select d;
            return new SelectList(dQuery, "ID", "FormalName", id);
        }
        private void PopulateDropDownLists(Appointment? appointment = null)
        {
            ViewData["AppointmentReasonID"] = AppointmentReasonSelectList(appointment?.AppointmentReasonID);
            ViewData["DoctorID"] = DoctorSelectList(appointment?.DoctorID);
        }


        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.ID == id);
        }
    }
}
