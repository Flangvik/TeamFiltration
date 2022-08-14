using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{

    public class GetMembersAAD
    {
        [JsonProperty("odata.metadata")]
        public string odatametadata { get; set; }
        [JsonProperty("odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<MemberAAD> value { get; set; }
    }

    public class MemberAAD
    {
        public string odatatype { get; set; }
        public string objectType { get; set; }
        public string objectId { get; set; }
        public bool? accountEnabled { get; set; }
        public string city { get; set; }
        public string companyName { get; set; }
      
        public string department { get; set; }
        public string displayName { get; set; }
        public string facsimileTelephoneNumber { get; set; }
        public string givenName { get; set; }
        public string immutableId { get; set; }
        public string jobTitle { get; set; }
  
    
        public string mail { get; set; }
        public string mailNickname { get; set; }
        public string mobile { get; set; }
        public string onPremisesDistinguishedName { get; set; }
        public string onPremisesSecurityIdentifier { get; set; }

        public string passwordPolicies { get; set; }
        public object passwordProfile { get; set; }
        public string physicalDeliveryOfficeName { get; set; }
        public string postalCode { get; set; }
        public string[] proxyAddresses { get; set; }
    
        public string sipProxyAddress { get; set; }
        public string state { get; set; }
        public string streetAddress { get; set; }
        public string surname { get; set; }
        public string telephoneNumber { get; set; }
        public string thumbnailPhotoodatamediaEditLink { get; set; }
        public string thumbnailPhotoodatamediaContentType { get; set; }
        public string usageLocation { get; set; }
        public string userPrincipalName { get; set; }
        public string userType { get; set; }

    }

  

  
}
