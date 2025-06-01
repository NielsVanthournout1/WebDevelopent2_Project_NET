using Microsoft.AspNetCore.Mvc;
using app.SID_in_beurzen;
using app.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using app.Services; // Add this namespace if DatabaseFixService is defined here

namespace app.Controllers
{
    public class AdminController : Controller
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IConfiguration _configuration;
        private readonly DatabaseFixService _dbFixService; // Ensure the type is correctly referenced
        private readonly DirectDatabaseAccessService _directDbService;

        public AdminController(SidInBeurzenContext context, ILogger<AdminController> logger, 
            IConfiguration configuration, DatabaseFixService dbFixService,
            DirectDatabaseAccessService directDbService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _dbFixService = dbFixService;
            _directDbService = directDbService;
        }

        // GET: /Admin/UserManagement
        public async Task<IActionResult> UserManagement()
        {
            // Check if user is admin (assuming role id 1 is admin)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var viewModel = new UserListViewModel
            {
                Users = await _context.Accounts
                    .Include(a => a.RoleType)
                    .Select(a => new UserViewModel
                    {
                        AccountId = a.AccountId,
                        FirstName = a.FirstName,
                        LastName = a.LastName,
                        EmailAddress = a.EmailAddress,
                        RoleTypeId = a.RoleTypeId,
                        RoleName = a.RoleType.RoleName,
                        IsActive = a.IsActive ?? false,
                        IsRegistrationComplete = a.PasswordHash != null
                    })
                    .ToListAsync(),

                AvailableRoles = await _context.RoleTypes.ToListAsync()
            };

            return View(viewModel);
        }

        // GET: /Admin/CreateUser
        public async Task<IActionResult> CreateUser()
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var viewModel = new CreateUserViewModel
            {
                AvailableRoles = await _context.RoleTypes.ToListAsync()
            };

            return View(viewModel);
        }

        // POST: /Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            _logger.LogInformation("CreateUser POST method started");
            
            try
            {
                // Check if user is admin
                var roleString = HttpContext.Session.GetString("GebruikerRol");
                _logger.LogInformation("Current user role: {Role}", roleString ?? "null");
                
                if (roleString != "1") // Administrator role
                {
                    _logger.LogWarning("Access denied: User is not admin");
                    return RedirectToAction("AccessDenied", "Account");
                }

                _logger.LogInformation("Admin validated, processing form data");
                _logger.LogInformation("Form data - FirstName: {FirstName}, LastName: {LastName}, Email: {Email}, RoleTypeId: {RoleTypeId}", 
                    model.FirstName, model.LastName, model.EmailAddress, model.RoleTypeId);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            _logger.LogWarning("Validation error for {Property}: {Error}", 
                                state.Key, error.ErrorMessage);
                        }
                    }
                    model.AvailableRoles = await _context.RoleTypes.ToListAsync();
                    return View(model);
                }

                _logger.LogInformation("Form validation passed");

                // Check if email already exists
                var existingUser = await _context.Accounts.FirstOrDefaultAsync(a =>
                    a.EmailAddress == model.EmailAddress);

                if (existingUser != null)
                {
                    _logger.LogWarning("Email {Email} already exists", model.EmailAddress);
                    ModelState.AddModelError("EmailAddress", "Email adres is al in gebruik.");
                    model.AvailableRoles = await _context.RoleTypes.ToListAsync();
                    return View(model);
                }

                // Generate a unique registration token
                var registrationToken = Guid.NewGuid();
                _logger.LogInformation("Generated registration token: {Token}", registrationToken);

                // Create new user account
                var newUser = new Account
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    RoleTypeId = model.RoleTypeId,
                    IsActive = true,
                    RegistrationToken = registrationToken,
                    PasswordHash = null // Will be set when user completes registration
                };

                _logger.LogInformation("Created user object in memory");

                try
                {
                    _logger.LogInformation("Adding user to context");
                    _context.Accounts.Add(newUser);
                    
                    _logger.LogInformation("Saving changes to database");
                    var rowsAffected = await _context.SaveChangesAsync();
                    _logger.LogInformation("Database save completed. Rows affected: {RowsAffected}", rowsAffected);

                    _logger.LogInformation("Admin created new user account for {Email} with ID {AccountId}", 
                        model.EmailAddress, newUser.AccountId);

                    // Generate registration link
                    var registrationLink = Url.Action("SetPassword", "Account",
                        new { token = registrationToken.ToString() }, Request.Scheme);
                    
                    _logger.LogInformation("Generated registration link: {Link}", registrationLink);
                    
                    // More visible console log
                    _logger.LogCritical("REGISTRATION LINK FOR USER {Email}: {Link}", 
                        model.EmailAddress, registrationLink);

                    // Store link in ViewBag for display purposes
                    ViewBag.RegistrationLink = registrationLink;
                    ViewBag.SuccessMessage = $"Gebruiker {model.FirstName} {model.LastName} is succesvol aangemaakt.";
                    _logger.LogInformation("Setting ViewBag values and returning UserCreated view");

                    return View("UserCreated");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving new user account for {Email}", model.EmailAddress);
                    ModelState.AddModelError(string.Empty, "Er is een fout opgetreden bij het aanmaken van de gebruiker: " + ex.Message);
                    model.AvailableRoles = await _context.RoleTypes.ToListAsync();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateUser");
                ModelState.AddModelError(string.Empty, "Er is een onverwachte fout opgetreden: " + ex.Message);
                model.AvailableRoles = await _context.RoleTypes.ToListAsync();
                return View(model);
            }
        }

        // GET: /Admin/UserCreated - Should only be accessed from CreateUser action
        public IActionResult UserCreated()
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // If the view is accessed directly without going through the creation process
            if (ViewBag.RegistrationLink == null || ViewBag.SuccessMessage == null)
            {
                return RedirectToAction(nameof(UserManagement));
            }

            return View();
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                // Check if user is admin
                var roleString = HttpContext.Session.GetString("GebruikerRol");
                if (roleString != "1") // Administrator role
                {
                    _logger.LogWarning("Non-admin attempted to delete user with ID: {UserId}", id);
                    return RedirectToAction("AccessDenied", "Account");
                }

                _logger.LogInformation("Attempting to delete user with ID: {UserId}", id);

                var user = await _context.Accounts.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Delete failed: User with ID {UserId} not found", id);
                    TempData["ErrorMessage"] = "Gebruiker niet gevonden.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Don't allow deleting your own account
                if (user.EmailAddress == HttpContext.Session.GetString("GebruikerEmail"))
                {
                    _logger.LogWarning("User attempted to delete their own account: {Email}", user.EmailAddress);
                    TempData["ErrorMessage"] = "Je kunt je eigen account niet verwijderen.";
                    return RedirectToAction(nameof(UserManagement));
                }

                _context.Accounts.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin successfully deleted user: {Email}", user.EmailAddress);
                TempData["SuccessMessage"] = $"Gebruiker {user.FirstName} {user.LastName} is succesvol verwijderd.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                TempData["ErrorMessage"] = "Er is een fout opgetreden bij het verwijderen van de gebruiker.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // POST: /Admin/UpdateUserRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(int id, int roleId)
        {
            try
            {
                // Check if user is admin
                var roleString = HttpContext.Session.GetString("GebruikerRol");
                if (roleString != "1") // Administrator role
                {
                    _logger.LogWarning("Non-admin attempted to update user role. User ID: {UserId}, New Role: {RoleId}", id, roleId);
                    return RedirectToAction("AccessDenied", "Account");
                }

                _logger.LogInformation("Attempting to update role for user ID: {UserId} to role ID: {RoleId}", id, roleId);

                // Check if role exists
                var role = await _context.RoleTypes.FindAsync(roleId);
                if (role == null)
                {
                    _logger.LogWarning("Role update failed: Role ID {RoleId} not found", roleId);
                    TempData["ErrorMessage"] = "De geselecteerde rol bestaat niet.";
                    return RedirectToAction(nameof(UserManagement));
                }

                var user = await _context.Accounts.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Role update failed: User with ID {UserId} not found", id);
                    TempData["ErrorMessage"] = "Gebruiker niet gevonden.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // If it's the same role, don't do anything
                if (user.RoleTypeId == roleId)
                {
                    _logger.LogInformation("No role change needed - user already has role ID: {RoleId}", roleId);
                    return RedirectToAction(nameof(UserManagement));
                }

                // Don't allow changing your own role from admin
                if (user.EmailAddress == HttpContext.Session.GetString("GebruikerEmail") && roleId != 1)
                {
                    _logger.LogWarning("Admin attempted to change their own role from admin: {Email}", user.EmailAddress);
                    TempData["ErrorMessage"] = "Je kunt je eigen admin rol niet wijzigen.";
                    return RedirectToAction(nameof(UserManagement));
                }

                // Update user role
                user.RoleTypeId = roleId;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully updated role for user: {Email} to {RoleName}", user.EmailAddress, role.RoleName);
                TempData["SuccessMessage"] = $"Rol van {user.FirstName} {user.LastName} is succesvol gewijzigd naar {role.RoleName}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user ID: {UserId} to role ID: {RoleId}", id, roleId);
                TempData["ErrorMessage"] = "Er is een fout opgetreden bij het bijwerken van de gebruikersrol.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // POST: /Admin/ResendInvitation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendInvitation(int id)
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var user = await _context.Accounts.FindAsync(id);
            if (user != null && user.PasswordHash == null)
            {
                // Generate a new token if the old one is used or lost
                if (user.RegistrationToken == null)
                {
                    user.RegistrationToken = Guid.NewGuid();
                    await _context.SaveChangesAsync();
                }

                // Generate registration link
                var registrationLink = Url.Action("SetPassword", "Account",
                    new { token = user.RegistrationToken.ToString() }, Request.Scheme);

                // Instead of sending email, log the link (it will appear in the console)
                _logger.LogWarning("REGISTRATION LINK: {Link}", registrationLink);

                ViewBag.RegistrationLink = registrationLink;
                ViewBag.SuccessMessage = $"Uitnodiging voor {user.FirstName} {user.LastName} is opnieuw gegenereerd. Controleer de console voor de registratielink.";

                return View("UserCreated");
            }

            TempData["ErrorMessage"] = "Gebruiker heeft de registratie al voltooid of bestaat niet.";
            return RedirectToAction(nameof(UserManagement));
        }

        // GET: /Admin/CheckUsers
        public async Task<IActionResult> CheckUsers()
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            try
            {
                var accounts = await _context.Accounts.Include(a => a.RoleType).ToListAsync();
                var result = new ContentResult
                {
                    ContentType = "text/plain",
                    Content = $"Total accounts: {accounts.Count}\n\n" +
                              string.Join("\n", accounts.Select(a => 
                                  $"ID: {a.AccountId}, Name: {a.FirstName} {a.LastName}, " +
                                  $"Email: {a.EmailAddress}, Role: {a.RoleType?.RoleName ?? "None"}, " +
                                  $"Active: {a.IsActive}, HasPassword: {!string.IsNullOrEmpty(a.PasswordHash)}, " +
                                  $"Token: {a.RegistrationToken}"))
                };
                return result;
            }
            catch (Exception ex)
            {
                return new ContentResult
                {
                    ContentType = "text/plain",
                    Content = $"Error accessing database: {ex.Message}\n\n{ex.StackTrace}"
                };
            }
        }

        // Add this method to send emails
        private async Task SendRegistrationEmail(string email, string firstName, string lastName, string registrationLink)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var smtpClient = new SmtpClient(smtpSettings["Host"])
                {
                    Port = int.Parse(smtpSettings["Port"]),
                    Credentials = new NetworkCredential(smtpSettings["Username"], smtpSettings["Password"]),
                    EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings["SenderEmail"], smtpSettings["SenderName"]),
                    Subject = "Voltooi je registratie",
                    Body = $@"
                        <html>
                        <body>
                            <h2>Welkom bij onze applicatie!</h2>
                            <p>Beste {firstName} {lastName},</p>
                            <p>Je account is aangemaakt. Gebruik de onderstaande link om je registratie te voltooien en een wachtwoord in te stellen:</p>
                            <p><a href='{registrationLink}'>{registrationLink}</a></p>
                            <p>Deze link is alleen voor jou bedoeld. Deel deze niet met anderen.</p>
                            <p>Met vriendelijke groet,<br>Het Team</p>
                        </body>
                        </html>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Registration email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send registration email to {Email}", email);
            }
        }

        // GET: /Admin/CandidateManagement
        public async Task<IActionResult> CandidateManagement()
        {
            // Check if user is admin (assuming role id 1 is admin)
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var viewModel = new CandidateListViewModel
            {
                Candidates = await _context.Candidates
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
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // GET: /Admin/CreateCandidate
        public IActionResult CreateCandidate()
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            _logger.LogInformation("User attempting to access CreateCandidate with role: {Role}", roleString);
            
            if (roleString != "1") // Administrator role
            {
                _logger.LogWarning("Access denied to CreateCandidate. User role: {Role}", roleString);
                return RedirectToAction("AccessDenied", "Account");
            }

            return View(new CreateCandidateViewModel());
        }

        // POST: /Admin/CreateCandidate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCandidate(CreateCandidateViewModel model)
        {
            _logger.LogInformation("CreateCandidate POST method started");

            try
            {
                // Check if user is admin
                var roleString = HttpContext.Session.GetString("GebruikerRol");
                if (roleString != "1") // Administrator role
                {
                    _logger.LogWarning("Access denied: User is not admin");
                    return RedirectToAction("AccessDenied", "Account");
                }

                _logger.LogInformation("Form data received - GivenName: {GivenName}, Surname: {Surname}, " +
                    "BirthDate: {BirthDate}, Institution: {Institution}, FieldOfStudy: {FieldOfStudy}",
                    model.GivenName, model.Surname, model.BirthDate, model.Institution, model.FieldOfStudy);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            _logger.LogWarning("Validation error for {Property}: {Error}",
                                state.Key, error.ErrorMessage);
                        }
                    }
                    return View(model);
                }

                // Generate a unique QR code token
                var qrCodeToken = GenerateUniqueToken();
                _logger.LogInformation("Generated QR code token: {Token}", qrCodeToken);

                // First try with EF Core approach
                try
                {
                    _logger.LogInformation("Attempting EF Core approach first");

                    // Create new candidate with explicit column name match
                    var newCandidate = new Candidate
                    {
                        GivenName = model.GivenName,
                        Surname = model.Surname,
                        BirthDate = model.BirthDate,
                        Institution = model.Institution,
                        FieldOfStudy = model.FieldOfStudy,
                        QrcodeLink = qrCodeToken  // This property maps to QRCodeLink in the DB
                    };

                    // Add candidate to current context
                    _context.ChangeTracker.Clear(); // Clear any tracked entities
                    _context.Candidates.Add(newCandidate);

                    // Save changes 
                    var rowsAffected = await _context.SaveChangesAsync();
                    _logger.LogInformation("EF Core save completed. Rows affected: {RowsAffected}", rowsAffected);

                    if (newCandidate.CandidateId > 0)
                    {
                        // Success - generate QR code URL
                        var qrCodeUrl = Url.Action("RegisterInterest", "CandidateRegistration",
                            new { candidateToken = qrCodeToken }, Request.Scheme);

                        // Store data in ViewBag for the success view
                        ViewBag.QrCodeUrl = qrCodeUrl;
                        ViewBag.Candidate = newCandidate;
                        ViewBag.SuccessMessage = $"Studiekiezer {model.GivenName} {model.Surname} is succesvol aangemaakt.";

                        return View("CandidateCreated");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error with EF Core approach, will try direct SQL next");
                }

                // Try direct database insertion as fallback
                _logger.LogInformation("Attempting direct SQL insertion");
                var directResult = await _directDbService.InsertCandidateDirectly(
                    model.GivenName, model.Surname, model.BirthDate,
                    model.Institution, model.FieldOfStudy, qrCodeToken);

                if (directResult.success)
                {
                    _logger.LogInformation("Direct SQL insertion succeeded with ID {ID}", directResult.candidateId);

                    // Create a Candidate object for the view with the ID from direct SQL
                    var directCandidate = new Candidate
                    {
                        CandidateId = directResult.candidateId,
                        GivenName = model.GivenName,
                        Surname = model.Surname,
                        BirthDate = model.BirthDate,
                        Institution = model.Institution,
                        FieldOfStudy = model.FieldOfStudy,
                        QrcodeLink = qrCodeToken
                    };

                    // Generate QR code URL
                    var qrCodeUrl = Url.Action("RegisterInterest", "CandidateRegistration",
                        new { candidateToken = qrCodeToken }, Request.Scheme);

                    // Store data in ViewBag
                    ViewBag.QrCodeUrl = qrCodeUrl;
                    ViewBag.Candidate = directCandidate;
                    ViewBag.SuccessMessage = $"Studiekiezer {model.GivenName} {model.Surname} is succesvol aangemaakt.";

                    return View("CandidateCreated");
                }

                // Both methods failed
                ModelState.AddModelError(string.Empty, "Kon de studiekiezer niet aanmaken in de database. Probeer het opnieuw.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in CreateCandidate");
                ModelState.AddModelError(string.Empty, $"Er is een onverwachte fout opgetreden: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Admin/EditCandidate/{id}
        public async Task<IActionResult> EditCandidate(int id)
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            _logger.LogInformation("User attempting to access EditCandidate with role: {Role}", roleString);
            
            if (roleString != "1") // Administrator role
            {
                _logger.LogWarning("Access denied to EditCandidate. User role: {Role}", roleString);
                return RedirectToAction("AccessDenied", "Account");
            }

            var candidate = await _context.Candidates.FindAsync(id);
            if (candidate == null)
            {
                _logger.LogWarning("Candidate with ID {Id} not found", id);
                return NotFound();
            }

            var viewModel = new EditCandidateViewModel
            {
                CandidateId = candidate.CandidateId,
                GivenName = candidate.GivenName,
                Surname = candidate.Surname,
                BirthDate = candidate.BirthDate,
                Institution = candidate.Institution,
                FieldOfStudy = candidate.FieldOfStudy,
                QrcodeLink = candidate.QrcodeLink
            };

            return View(viewModel);
        }

        // POST: /Admin/EditCandidate/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCandidate(int id, EditCandidateViewModel model)
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
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
                    _logger.LogInformation("Updated candidate with ID {CandidateId}", id);
                    TempData["SuccessMessage"] = $"Studiekiezer {model.GivenName} {model.Surname} is succesvol bijgewerkt.";
                    return RedirectToAction(nameof(CandidateManagement));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating candidate {CandidateId}", id);
                    ModelState.AddModelError(string.Empty, "Er is een fout opgetreden bij het bijwerken van de studiekiezer.");
                }
            }

            return View(model);
        }

        // POST: /Admin/DeleteCandidate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCandidate(int id)
        {
            try
            {
                // Check if user is admin
                var roleString = HttpContext.Session.GetString("GebruikerRol");
                if (roleString != "1") // Administrator role
                {
                    _logger.LogWarning("Non-admin attempted to delete candidate with ID: {CandidateId}", id);
                    return RedirectToAction("AccessDenied", "Account");
                }

                _logger.LogInformation("Attempting to delete candidate with ID: {CandidateId}", id);

                var candidate = await _context.Candidates.FindAsync(id);
                if (candidate == null)
                {
                    _logger.LogWarning("Delete failed: Candidate with ID {CandidateId} not found", id);
                    TempData["ErrorMessage"] = "Studiekiezer niet gevonden.";
                    return RedirectToAction(nameof(CandidateManagement));
                }

                // Check if candidate has registrations
                var hasRegistrations = await _context.Registrations
                    .AnyAsync(r => r.CandidateId == id);
                    
                if (hasRegistrations)
                {
                    _logger.LogWarning("Cannot delete candidate with ID {CandidateId} because they have registrations", id);
                    TempData["ErrorMessage"] = "Deze studiekiezer heeft registraties en kan niet worden verwijderd.";
                    return RedirectToAction(nameof(CandidateManagement));
                }

                _context.Candidates.Remove(candidate);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin successfully deleted candidate: {Name}", $"{candidate.GivenName} {candidate.Surname}");
                TempData["SuccessMessage"] = $"Studiekiezer {candidate.GivenName} {candidate.Surname} is succesvol verwijderd.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting candidate with ID: {CandidateId}", id);
                TempData["ErrorMessage"] = "Er is een fout opgetreden bij het verwijderen van de studiekiezer.";
            }

            return RedirectToAction(nameof(CandidateManagement));
        }

        private string GenerateUniqueToken()
        {
            // Generate a random token for QR codes
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var tokenChars = new char[10]; // 10 character token

            for (int i = 0; i < tokenChars.Length; i++)
            {
                tokenChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(tokenChars);
        }

        // Add this to your AdminController.cs
        // This method will test database access specifically for candidates
        [HttpGet]
        public async Task<IActionResult> TestCandidateDb()
        {
            // Check if user is admin
            var roleString = HttpContext.Session.GetString("GebruikerRol");
            if (roleString != "1") // Administrator role
            {
                return Content("Unauthorized: Admin access required");
            }

            try
            {
                var output = new System.Text.StringBuilder();
                
                // Step 1: Test basic database connection
                var canConnect = await _context.Database.CanConnectAsync();
                output.AppendLine($"Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}");
                
                // Step 2: Verify that DbContext is properly configured
                var dbContextType = _context.GetType().FullName;
                output.AppendLine($"DbContext type: {dbContextType}");
                
                // Step 3: Try to read candidates (this doesn't modify the database)
                var candidateCount = await _context.Candidates.CountAsync();
                output.AppendLine($"Current candidate count: {candidateCount}");

                // Step 4: Test inserting a candidate directly
                var testCandidate = new Candidate
                {
                    GivenName = "Test_" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    Surname = "Candidate",
                    BirthDate = DateTime.Now.AddYears(-20),
                    Institution = "Test University",
                    FieldOfStudy = "Testing",
                    QrcodeLink = "test_" + Guid.NewGuid().ToString("N").Substring(0, 8)
                };
                
                output.AppendLine($"Testing insertion of candidate: {testCandidate.GivenName} {testCandidate.Surname}");
                
                // Reset DbContext state to make sure we're starting fresh
                _context.ChangeTracker.Clear();
                    
                // Add and save
                _context.Candidates.Add(testCandidate);
                var rowsAffected = await _context.SaveChangesAsync();
                    
                output.AppendLine($"Insert test result: Rows affected = {rowsAffected}, New ID = {testCandidate.CandidateId}");
                
                // Step 5: Verify the candidate was added
                var newCount = await _context.Candidates.CountAsync();
                output.AppendLine($"New candidate count: {newCount} (should be +1 from before)");
                
                // Step 6: Find the candidate we just created to be sure
                var foundCandidate = await _context.Candidates.FindAsync(testCandidate.CandidateId);
                output.AppendLine(foundCandidate != null 
                    ? $"Successfully retrieved test candidate with ID {testCandidate.CandidateId}" 
                    : "Failed to retrieve the test candidate!");

                // Step 7: Configure connection string info (without showing passwords)
                var connectionString = _context.Database.GetConnectionString();
                if (connectionString != null)
                {
                    // Show connection string info without revealing password
                    var connParts = connectionString.Split(';')
                        .Where(p => !p.StartsWith("password", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    output.AppendLine($"Connection string (partial): {string.Join(";", connParts)}");
                }
                else
                {
                    output.AppendLine("Connection string is null!");
                }
                
                return Content(output.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                var error = new System.Text.StringBuilder();
                error.AppendLine($"TEST FAILED: {ex.Message}");
                error.AppendLine($"Exception type: {ex.GetType().FullName}");
                error.AppendLine("Stack trace:");
                error.AppendLine(ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    error.AppendLine($"Inner exception: {ex.InnerException.Message}");
                    error.AppendLine($"Inner exception type: {ex.InnerException.GetType().FullName}");
                }
                
                return Content(error.ToString(), "text/plain");
            }
        }
    }
}