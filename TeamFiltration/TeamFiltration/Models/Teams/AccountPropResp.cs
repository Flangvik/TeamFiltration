using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Teams
{


    public class AccountPropResp
    {
        public string userPinnedApps { get; set; }
        public string firstLoginInformation { get; set; }
        public string licenseType { get; set; }
        public string contactsTabLastVisitTime { get; set; }
        public string personalFileSite { get; set; }
        public string cortanaSettings { get; set; }
        public string readReceiptsEnabled { get; set; }
        public string locale { get; set; }
        public string isSkypeTeamsUserSetInSettingsStore { get; set; }
        public string suggestedContacts { get; set; }
        public string userDetails { get; set; }
        public string cid { get; set; }
        public string cidHex { get; set; }
        public bool dogfoodUser { get; set; }
        public string primaryMemberName { get; set; }
        public string skypeName { get; set; }
    }


    public class userDetails
    {
        public string name { get; set; }
        public string upn { get; set; }
    }


}
