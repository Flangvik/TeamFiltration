using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamFiltration.Models.Teams
{
    public class GetPresenceResp
    {
        public string mri { get; set; }
        public Presence presence { get; set; }
        public bool etagMatch { get; set; }
        public string etag { get; set; }
        public int status { get; set; }
    }

    public class Presence
    {
        public string sourceNetwork { get; set; }
        public Calendardata calendarData { get; set; }
        public object[] capabilities { get; set; }
        public string availability { get; set; }
        public string activity { get; set; }
        public string deviceType { get; set; }
    }

    public class Calendardata
    {
        public Outofofficenote outOfOfficeNote { get; set; }
        public bool isOutOfOffice { get; set; }
    }

    public class Outofofficenote
    {
        public string message { get; set; }
        public DateTime publishTime { get; set; }
        public DateTime expiry { get; set; }
    }
}
