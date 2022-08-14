using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{



    public class GroupsResp
    {
        [JsonProperty("@odata.context")]
        public string odatacontext { get; set; }
        [JsonProperty("@odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<GroupObject> Value { get; set; }
    }

    public class GroupObject
    {
        public List<MembersObject> userMembers { get; set; }
        public string id { get; set; }
        public object deletedDateTime { get; set; }
        public object classification { get; set; }
        public DateTime createdDateTime { get; set; }
        public string[] creationOptions { get; set; }
        public string description { get; set; }
        public string displayName { get; set; }
        public object expirationDateTime { get; set; }
        public string[] groupTypes { get; set; }
        public object isAssignableToRole { get; set; }
        public string mail { get; set; }
        public bool mailEnabled { get; set; }
        public string mailNickname { get; set; }
        public string membershipRule { get; set; }
        public string membershipRuleProcessingState { get; set; }
        public object onPremisesDomainName { get; set; }
        public object onPremisesLastSyncDateTime { get; set; }
        public object onPremisesNetBiosName { get; set; }
        public object onPremisesSamAccountName { get; set; }
        public object onPremisesSecurityIdentifier { get; set; }
        public object onPremisesSyncEnabled { get; set; }
        public object preferredDataLocation { get; set; }
        public object preferredLanguage { get; set; }
        public string[] proxyAddresses { get; set; }
        public DateTime renewedDateTime { get; set; }
        public string[] resourceBehaviorOptions { get; set; }
        public string[] resourceProvisioningOptions { get; set; }
        public bool securityEnabled { get; set; }
        public string securityIdentifier { get; set; }
        public object theme { get; set; }
        public string visibility { get; set; }
        public object[] onPremisesProvisioningErrors { get; set; }
    }


}
