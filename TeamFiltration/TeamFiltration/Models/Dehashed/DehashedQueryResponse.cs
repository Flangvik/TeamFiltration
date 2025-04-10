using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Dehashed
{


    public class DehashedQueryResponse
    {
        public int balance { get; set; }
        public List<Entry> entries { get; set; }
        public string took { get; set; }
        public int total { get; set; }
    }

    public class Entry
    {
        public string id { get; set; }
        public string[] email { get; set; }
        public string[] username { get; set; }
        public string[] name { get; set; }
        public string[] address { get; set; }
        public string[] phone { get; set; }
        public string[] company { get; set; }
        public string[] url { get; set; }
        public string[] social { get; set; }
        public string database_name { get; set; }
        public string[] password { get; set; }
        public string[] hashed_password { get; set; }
        public string[] ip_address { get; set; }
    }


}
