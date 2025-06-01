using System;
using System.Collections.Generic;

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
    
    public class TradeShowAllStatisticsDto
    {
        public int TradeShowId { get; set; }
        public string? TradeShowName { get; set; }
        public DateTime? EventDate { get; set; }
        public int TotalCandidateCount { get; set; }
        public ProgramStatisticsDto? MostPopularProgram { get; set; }
        public ProgramStatisticsDto? LeastPopularProgram { get; set; }
    }

    public class CandidateRegistrationHistoryDto
    {
        public int RegistrationId { get; set; }
        public int TradeShowId { get; set; }
        public string? TradeShowName { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public string? Notes { get; set; }
        public List<string>? Programs { get; set; }
    }

    public class ProgramBasicDto
    {
        public int ProgramId { get; set; }
        public string? ProgramName { get; set; }
    }
}