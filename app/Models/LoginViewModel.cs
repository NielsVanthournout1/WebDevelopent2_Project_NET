using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class LoginViewModel // Removed 'partial' keyword
    {
        [Required(ErrorMessage = "E-mailadres is verplicht")]
        [EmailAddress(ErrorMessage = "Voer een geldig e-mailadres in")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        public string Password { get; set; } = string.Empty;
    }
}