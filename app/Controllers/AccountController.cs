using Microsoft.AspNetCore.Mvc;
using app.SID_in_beurzen;
using app.Models;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace app.Controllers
{
    public class AccountController : Controller
    {
        private readonly SidInBeurzenContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SidInBeurzenContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Debug logging
            _logger.LogDebug("Received Email: {Email}, Password Length: {PasswordLength}", 
                model.EmailAddress ?? "null", 
                model.Password?.Length ?? 0);

            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Count > 0)
                    {
                        _logger.LogDebug("Validation error for {Key}: {Error}", 
                            key, 
                            state.Errors.FirstOrDefault()?.ErrorMessage);
                    }
                }
                return View(model);
            }

            _logger.LogDebug("Login attempt for email: {Email}", model.EmailAddress);

            // Hash the password for comparison
            string hashedPassword = HashPassword(model.Password);

            var user = await _context.Accounts
                .FirstOrDefaultAsync(a => a.EmailAddress == model.EmailAddress
                                    && a.PasswordHash == hashedPassword
                                    && a.IsActive == true);

            if (user != null)
            {
                // Make sure we're storing the role as a string
                var roleId = user.RoleTypeId.HasValue ? user.RoleTypeId.Value.ToString() : "0";
                
                HttpContext.Session.SetString("GebruikerEmail", user.EmailAddress);
                HttpContext.Session.SetString("GebruikerRol", roleId);
                
                _logger.LogInformation("User {Email} logged in successfully with role {RoleId}", 
                    model.EmailAddress, roleId);
                
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Foute accountgegevens.");
            _logger.LogWarning("Failed login attempt for email: {Email}", model.EmailAddress);
            return View(model);
        }

        [Route("Account/Logout")]
        public IActionResult Logout()
        {
            var email = HttpContext.Session.GetString("GebruikerEmail");
            HttpContext.Session.Clear();
            _logger.LogInformation("User {Email} logged out", email ?? "unknown");
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/SetPassword
        [HttpGet]
        [Route("Account/SetPassword")]
        public async Task<IActionResult> SetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token is vereist.");
            }

            var parsedToken = Guid.TryParse(token, out var guidToken) ? guidToken : Guid.Empty;
            
            var user = await _context.Accounts
                .FirstOrDefaultAsync(a => a.RegistrationToken == parsedToken);
            
            if (user == null)
            {
                _logger.LogWarning("Invalid registration token: {Token}", token);
                return NotFound("Ongeldige registratielink.");
            }

            // If user has already set a password
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("Registration already completed for token: {Token}", token);
                return RedirectToAction("Login", new { message = "Registratie al voltooid. Log in met je wachtwoord." });
            }

            var model = new SetPasswordViewModel { Token = token };
            return View(model);
        }

        // POST: /Account/SetPassword
        [HttpPost]
        [Route("Account/SetPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            _logger.LogInformation("SetPassword POST method started with token: {TokenLength}", 
                model.Token?.Length ?? 0);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState not valid for SetPassword");
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

            if (string.IsNullOrEmpty(model.Token))
            {
                _logger.LogWarning("Token is empty in SetPassword");
                return BadRequest("Token is vereist.");
            }

            var parsedToken = Guid.TryParse(model.Token, out var guidToken) ? guidToken : Guid.Empty;
            _logger.LogInformation("Parsed token: {Token}", guidToken);
            
            var user = await _context.Accounts
                .FirstOrDefaultAsync(a => a.RegistrationToken == parsedToken);
            
            if (user == null)
            {
                _logger.LogWarning("No user found with token: {Token}", model.Token);
                ModelState.AddModelError(string.Empty, "Ongeldige registratielink.");
                return View(model);
            }

            _logger.LogInformation("Found user: {Email} with token", user.EmailAddress);

            // Hash the password
            string hashedPassword = HashPassword(model.Password);

            // Update the user account
            user.PasswordHash = hashedPassword;
            user.RegistrationToken = null; // Clear the token as it's been used

            await _context.SaveChangesAsync();
            _logger.LogInformation("User {Email} completed registration", user.EmailAddress);

            TempData["SuccessMessage"] = "Registratie voltooid. Je kunt nu inloggen.";
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        [Route("Account/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}