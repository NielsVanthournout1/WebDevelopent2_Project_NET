using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using app.SID_in_beurzen;

namespace app.Models
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Voornaam is verplicht")]
        [Display(Name = "Voornaam")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Achternaam is verplicht")]
        [Display(Name = "Achternaam")]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "E-mailadres is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mailadres")]
        [Display(Name = "E-mailadres")]
        public string? EmailAddress { get; set; }

        [Required(ErrorMessage = "Rol is verplicht")]
        [Display(Name = "Rol")]
        public int? RoleTypeId { get; set; }

        public List<RoleType> AvailableRoles { get; set; } = new List<RoleType>();
    }
}