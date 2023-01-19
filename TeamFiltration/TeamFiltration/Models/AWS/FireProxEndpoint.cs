using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.AWS
{
    public class FireProxEndpoint
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string FireProxURL { get; set; }
        public string RestApiId { get; set; }
        public bool Active { get; set; }
        public bool Deleted { get; set; }
        public string URL { get; set; }
        public string Region { get; set; }
    }
}
