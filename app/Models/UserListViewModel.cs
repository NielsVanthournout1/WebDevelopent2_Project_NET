using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using app.SID_in_beurzen;

namespace app.Models
{
    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; } = new();
        public List<RoleType> AvailableRoles { get; set; } = new();
    }

    public class UserViewModel
    {
        public int AccountId { get; set; }
        
        [Display(Name = "Voornaam")]
        public string? FirstName { get; set; }
        
        [Display(Name = "Achternaam")]
        public string? LastName { get; set; }
        
        [Display(Name = "Email")]
        public string? EmailAddress { get; set; }
        
        [Display(Name = "Rol")]
        public int? RoleTypeId { get; set; }
        
        [Display(Name = "Rol Naam")]
        public string? RoleName { get; set; }
        
        [Display(Name = "Actief")]
        public bool IsActive { get; set; }
        
        [Display(Name = "Registratie Status")]
        public bool IsRegistrationComplete { get; set; }
    }
}