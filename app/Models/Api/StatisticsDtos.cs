using System;

namespace app.Models.Api
{
    public class ProgramStatisticsDto
    {
        public int ProgramId { get; set; }
        public string? ProgramName { get; set; }
        public int RegistrationCount { get; set; }
    }

    public class TradeShowStatisticsDto
    {
        public int TradeShowId { get; set; }
        public string? TradeShowName { get; set; }
        public int CandidateCount { get; set; }
        public DateTime? EventDate { get; set; }
    }
}