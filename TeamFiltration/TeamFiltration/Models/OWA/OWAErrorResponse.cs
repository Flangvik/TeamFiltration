using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.OWA
{


    public class OWAErrorResponse
    {
        public Error error { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }

   
}
