using System;
using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class SetPasswordViewModel
    {
        [Required(ErrorMessage = "Token is verplicht")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Wachtwoord is verplicht")]
        [StringLength(100, ErrorMessage = "Het {0} moet ten minste {2} en maximaal {1} tekens bevatten.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Bevestig wachtwoord")]
        [Compare("Password", ErrorMessage = "Het wachtwoord en het bevestigingswachtwoord komen niet overeen.")]
        public string ConfirmPassword { get; set; }
    }
}