using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.TeamFiltration
{
    public class PulledTokens
    {
        public int Id { get; set; }
        public string ResourceUri { get; set; }
        public string Username { get; set; }
        public string ResourceClientId { get; set; }
        public string ResponseData { get; set; }
        public DateTime DateTime { get; set; }
    }
}
