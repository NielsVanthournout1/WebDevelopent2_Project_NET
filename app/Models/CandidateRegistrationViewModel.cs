using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using app.SID_in_beurzen;

namespace app.Models
{
    public class CandidateRegistrationViewModel
    {
        public int CandidateId { get; set; }

        [Display(Name = "Voornaam")]
        public string? GivenName { get; set; }

        [Display(Name = "Achternaam")]
        public string? Surname { get; set; }

        [Display(Name = "Geboortedatum")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Huidige onderwijsinstelling")]
        public string? Institution { get; set; }

        [Display(Name = "Huidig studiegebied")]
        public string? FieldOfStudy { get; set; }

        [Display(Name = "Beurs")]
        [Required(ErrorMessage = "Selecteer een beurs")]
        public int SelectedTradeShowId { get; set; }

        [Display(Name = "Opleidingen")]
        [Required(ErrorMessage = "Selecteer tenminste één opleiding")]
        public List<int>? SelectedProgramIds { get; set; }

        [Display(Name = "Opmerkingen")]
        public string? Notes { get; set; }

        // For the view to display available options
        public IEnumerable<app.SID_in_beurzen.Program>? AvailablePrograms { get; set; }
        public IEnumerable<app.SID_in_beurzen.TradeShow>? TradeShows { get; set; }
    }
}