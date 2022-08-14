using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.TeamFiltration
{


    public class CookieObject
    {
        public string domain { get; set; }
        public float expirationDate { get; set; }
        public bool hostOnly { get; set; }
        public bool httpOnly { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public string sameSite { get; set; }
        public bool secure { get; set; }
        public bool session { get; set; }
        public object storeId { get; set; }
        public string value { get; set; }
    }

 


}
