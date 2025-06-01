using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using app.SID_in_beurzen;

namespace app.Models
{
    public class CandidateListViewModel
    {
        public List<CandidateViewModel> Candidates { get; set; } = new();
    }

    public class CandidateViewModel
    {
        public int CandidateId { get; set; }
        
        [Display(Name = "Voornaam")]
        public string? GivenName { get; set; }
        
        [Display(Name = "Achternaam")]
        public string? Surname { get; set; }
        
        [Display(Name = "Geboortedatum")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }
        
        [Display(Name = "Instelling")]
        public string? Institution { get; set; }
        
        [Display(Name = "Studiegebied")]
        public string? FieldOfStudy { get; set; }
        
        [Display(Name = "QR Code Link")]
        public string? QrcodeLink { get; set; }
    }

    public class CreateCandidateViewModel
    {
        [Required(ErrorMessage = "Voornaam is verplicht")]
        [Display(Name = "Voornaam")]
        public string GivenName { get; set; }

        [Required(ErrorMessage = "Achternaam is verplicht")]
        [Display(Name = "Achternaam")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Geboortedatum is verplicht")]
        [Display(Name = "Geboortedatum")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Required(ErrorMessage = "Instelling is verplicht")]
        [Display(Name = "Huidige onderwijsinstelling")]
        public string Institution { get; set; }

        [Required(ErrorMessage = "Studiegebied is verplicht")]
        [Display(Name = "Huidig studiegebied")]
        public string FieldOfStudy { get; set; }
    }

    public class EditCandidateViewModel : CreateCandidateViewModel
    {
        public int CandidateId { get; set; }
        public string? QrcodeLink { get; set; }
    }
}