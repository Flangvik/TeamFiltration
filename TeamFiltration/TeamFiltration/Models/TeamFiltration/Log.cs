using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.TeamFiltration
{
    public class Log
    {
        public Log(string module, string message, string prefix = "")
        {

            Module = module;
            Message = message;
            Prefix = prefix;
            Timestamp = DateTime.Now;
        }

        public int Id { get; set; }
        public string Module { get; set; }
        public string Message { get; set; }
        public string Prefix { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
