using Microsoft.AspNetCore.Mvc;
using app.SID_in_beurzen;
using app.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace app.Controllers
{
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(SidInBeurzenContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            // Check if user is an admin, marketing team lead or marketing team member
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1" && roleString != "2" && roleString != "3") // Admin, Team Lead or Marketing
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            // Main dashboard page
            return View();
        }

        [HttpGet("program/{id}")]
        public async Task<IActionResult> ProgramDetails(int id)
        {
            // Check if user is an admin, marketing team lead or marketing team member
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1" && roleString != "2" && roleString != "3") // Admin, Team Lead or Marketing
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var program = await _context.Programs
                .FirstOrDefaultAsync(p => p.ProgramId == id);
            
            if (program == null)
            {
                return NotFound();
            }

            // Get all candidates interested in this program
            var candidates = await _context.Registrations
                .Include(r => r.Candidate)
                .Include(r => r.TradeShow)
                .Include(r => r.Programs)
                .Where(r => r.Programs.Any(p => p.ProgramId == id))
                .Select(r => new ProgramCandidateViewModel
                {
                    RegistrationId = r.RegistrationId,
                    CandidateId = r.Candidate.CandidateId,
                    CandidateName = $"{r.Candidate.GivenName} {r.Candidate.Surname}",
                    Institution = r.Candidate.Institution,
                    FieldOfStudy = r.Candidate.FieldOfStudy,
                    TradeShowName = r.TradeShow.Title,
                    TradeShowDate = r.TradeShow.EventDate,
                    RegisteredAt = r.RegisteredAt,
                    Notes = r.Notes
                })
                .ToListAsync();

            var viewModel = new ProgramDetailsViewModel
            {
                ProgramId = program.ProgramId,
                ProgramName = program.ProgramName,
                Candidates = candidates
            };

            return View(viewModel);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> ProgramStatistics()
        {
            // Check if user is a marketing team member (role 2 or 3)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1" && roleString != "2" && roleString != "3") // Admin, Team Lead or Marketing
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            try
            {
                // First get all programs
                var programs = await _context.Programs.ToListAsync();
                
                // Get all trade shows
                var tradeShows = await _context.TradeShows.ToListAsync();
                
                // Create stats with empty breakdown
                var programStats = programs.Select(p => new ProgramStatisticsViewModel
                {
                    ProgramId = p.ProgramId,
                    ProgramName = p.ProgramName,
                    TotalRegistrations = 0,
                    TradeShowBreakdown = new List<TradeShowStatViewModel>()
                }).ToList();
                
                // Get all registrations with their programs and tradeshows
                var registrations = await _context.Registrations
                    .Include(r => r.Programs)
                    .Include(r => r.TradeShow)
                    .ToListAsync();
                
                // Process registrations to count totals and breakdowns
                foreach (var registration in registrations)
                {
                    // Skip registrations with missing data
                    if (registration.TradeShow == null || !registration.Programs.Any())
                        continue;
                        
                    foreach (var program in registration.Programs)
                    {
                        // Find the program in our stats
                        var programStat = programStats.FirstOrDefault(ps => ps.ProgramId == program.ProgramId);
                        if (programStat == null)
                            continue;
                            
                        // Increment total count
                        programStat.TotalRegistrations++;
                        
                        // Find or create trade show breakdown entry
                        var tradeShowStat = programStat.TradeShowBreakdown
                            .FirstOrDefault(ts => ts.TradeShowId == registration.TradeShow.TradeShowId);
                            
                        if (tradeShowStat == null)
                        {
                            tradeShowStat = new TradeShowStatViewModel
                            {
                                TradeShowId = registration.TradeShow.TradeShowId,
                                TradeShowName = registration.TradeShow.Title ?? "",
                                Count = 0
                            };
                            programStat.TradeShowBreakdown.Add(tradeShowStat);
                        }
                        
                        // Increment count for this trade show
                        tradeShowStat.Count++;
                    }
                }

                return View(programStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating program statistics");
                TempData["ErrorMessage"] = "Er is een fout opgetreden bij het genereren van de statistieken.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet("candidate/{id}/edit")]
        public async Task<IActionResult> EditCandidate(int id)
        {
            // Check if user is an admin or a marketing team lead (roles 1 or 2)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1" && roleString != "2") // Admin or Team Lead
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.CandidateId == id);

            if (candidate == null)
            {
                return NotFound();
            }

            var viewModel = new CandidateEditViewModel
            {
                CandidateId = candidate.CandidateId,
                GivenName = candidate.GivenName,
                Surname = candidate.Surname,
                BirthDate = candidate.BirthDate,
                Institution = candidate.Institution,
                FieldOfStudy = candidate.FieldOfStudy
            };

            return View(viewModel);
        }

        [HttpPost("candidate/{id}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCandidate(int id, CandidateEditViewModel model)
        {
            // Check if user is an admin or a marketing team lead (roles 1 or 2)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1" && roleString != "2") // Admin or Team Lead
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (id != model.CandidateId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var candidate = await _context.Candidates.FindAsync(id);
                    if (candidate == null)
                    {
                        return NotFound();
                    }

                    candidate.GivenName = model.GivenName;
                    candidate.Surname = model.Surname;
                    candidate.BirthDate = model.BirthDate;
                    candidate.Institution = model.Institution;
                    candidate.FieldOfStudy = model.FieldOfStudy;

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Marketing Team Lead updated candidate {CandidateId}", id);
                    TempData["SuccessMessage"] = "Kandidaat succesvol bijgewerkt.";
                    return RedirectToAction(nameof(ManageCandidates));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Candidates.AnyAsync(c => c.CandidateId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        [HttpGet("candidates")]
        public async Task<IActionResult> ManageCandidates()
        {
            // Check if user is an admin or a marketing team lead (roles 1 or 2)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1" && roleString != "2") // Admin or Team Lead
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            var candidates = await _context.Candidates
                .Select(c => new CandidateViewModel
                {
                    CandidateId = c.CandidateId,
                    GivenName = c.GivenName,
                    Surname = c.Surname,
                    BirthDate = c.BirthDate,
                    Institution = c.Institution,
                    FieldOfStudy = c.FieldOfStudy,
                    QrcodeLink = c.QrcodeLink
                })
                .ToListAsync();

            return View(candidates);
        }

        [HttpPost("candidates/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCandidates(int[] candidateIds)
        {
            // Check if user is an admin or a marketing team lead (roles 1 or 2)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1" && roleString != "2") // Admin or Team Lead
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            
            if (candidateIds == null || !candidateIds.Any())
            {
                return BadRequest();
            }

            try
            {
                // Get candidates to delete
                var candidates = await _context.Candidates
                    .Where(c => candidateIds.Contains(c.CandidateId))
                    .ToListAsync();
                
                // Get registrations associated with these candidates
                var registrations = await _context.Registrations
                    .Where(r => candidateIds.Contains(r.CandidateId.Value))
                    .ToListAsync();
                
                // Remove registrations first (due to foreign key constraints)
                if (registrations.Any())
                {
                    _context.Registrations.RemoveRange(registrations);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted {Count} registrations for candidates", registrations.Count);
                }
                
                // Now remove the candidates
                if (candidates.Any())
                {
                    _context.Candidates.RemoveRange(candidates);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Marketing Team Lead deleted {Count} candidates", candidates.Count);
                    TempData["SuccessMessage"] = $"{candidates.Count} kandidaten zijn succesvol verwijderd.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting candidates");
                TempData["ErrorMessage"] = "Er is een fout opgetreden bij het verwijderen van kandidaten.";
            }

            return RedirectToAction(nameof(ManageCandidates));
        }
    }
}   