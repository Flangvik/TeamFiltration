using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.TeamFiltration
{
    public class Config
    {
        /*
            Pushover App and User key, for altering on valid creds or blocked accounts
        */
        public string PushoverAppKey { get; set; }
        public string PushoverUserKey { get; set; }

        /*
            Collection of FireProx endpoints targeting different Microsoft Auth endpoints, when spraying
      

        public List<string> MsolFireProxEndpointsUs { get; set; }
        public List<string> MsolFireProxEndpoints { get; set; }
        public List<string> TeamsEnumFireProxEndpoints { get; set; }
        public List<string> AadSSoFireProxEndpoints { get; set; }
          */

        /*
            Dehashed API for the enumeration phase
        */

        public string DehashedApiKey { get; set; }
        public string DehashedEmail { get; set; }

        /*
           Sacrifical o365 account to be used for enumerating users using the teams method
        */

        public string SacrificialO365Username { get; set; }
        public string SacrificialO365Passwords { get; set; }

        public string proxyEndpoint { get; set; }
        public string AWSAccessKey { get; set; }
        public string AWSSecretKey { get; set; }

    }
}
