using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.TeamFiltration
{
    public class SpraySummary
    {
        public DateTime StartTime { get; set; }
        public DateTime StopTime { get; set; }
        public string Password { get; set; }
        public int SuccesCount { get; set; }
        public int TotalCount { get; set; }
    }
}
