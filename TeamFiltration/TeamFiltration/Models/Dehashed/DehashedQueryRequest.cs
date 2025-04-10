using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamFiltration.Models.Dehashed
{

    public class DehashedQueryRequest
    {
        public string query { get; set; }
        public int page { get; set; }
        public int size { get; set; }
        public bool regex { get; set; }
        public bool wildcard { get; set; }
        public bool de_dupe { get; set; }
    }

}
