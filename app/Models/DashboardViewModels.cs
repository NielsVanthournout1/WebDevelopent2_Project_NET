using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace app.Models
{
    public class ProgramDetailsViewModel
    {
        public int ProgramId { get; set; }
        public string? ProgramName { get; set; }
        public List<ProgramCandidateViewModel> Candidates { get; set; } = new();
    }

    public class ProgramCandidateViewModel
    {
        public int RegistrationId { get; set; }
        public int CandidateId { get; set; }
        public string? CandidateName { get; set; }
        public string? Institution { get; set; }
        public string? FieldOfStudy { get; set; }
        public string? TradeShowName { get; set; }
        public DateTime? TradeShowDate { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ProgramStatisticsViewModel
    {
        public int ProgramId { get; set; }
        public string? ProgramName { get; set; }
        public int TotalRegistrations { get; set; }
        public List<TradeShowStatViewModel> TradeShowBreakdown { get; set; } = new();
    }

    public class TradeShowStatViewModel
    {
        public int TradeShowId { get; set; }
        public string? TradeShowName { get; set; }
        public int Count { get; set; }
    }

    public class CandidateEditViewModel
    {
        public int CandidateId { get; set; }
        
        [Display(Name = "Voornaam")]
        [Required(ErrorMessage = "Voornaam is verplicht")]
        public string? GivenName { get; set; }
        
        [Display(Name = "Achternaam")]
        [Required(ErrorMessage = "Achternaam is verplicht")]
        public string? Surname { get; set; }
        
        [Display(Name = "Geboortedatum")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }
        
        [Display(Name = "Onderwijsinstelling")]
        public string? Institution { get; set; }
        
        [Display(Name = "Studiegebied")]
        public string? FieldOfStudy { get; set; }
    }
}