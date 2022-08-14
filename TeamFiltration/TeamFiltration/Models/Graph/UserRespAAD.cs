using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamFiltration.Models.Graph
{


    public class UserRespAAD
    {

       
        [JsonProperty("odata.metadata")]
        public string odatametadata { get; set; }
        [JsonProperty("odata.nextLink")]
        public string odatanextLink { get; set; }
        public List<UserObjectAAD> value { get; set; }
    }

    public class UserObjectAAD
    {
        public string odatatype { get; set; }
        public string objectType { get; set; }
        public string objectId { get; set; }
        public object deletionTimestamp { get; set; }
        public bool accountEnabled { get; set; }
        public object ageGroup { get; set; }
        public Assignedlicens[] assignedLicenses { get; set; }
        public Assignedplan[] assignedPlans { get; set; }
        public object city { get; set; }
        public string companyName { get; set; }
        public object consentProvidedForMinor { get; set; }
        public string country { get; set; }
        public DateTime createdDateTime { get; set; }
        public string creationType { get; set; }
        public string department { get; set; }
        public bool? dirSyncEnabled { get; set; }
        public string displayName { get; set; }
        public object employeeId { get; set; }
        public string facsimileTelephoneNumber { get; set; }
        public string givenName { get; set; }
        public string immutableId { get; set; }
        public object isCompromised { get; set; }
        public string jobTitle { get; set; }
        public string lastDirSyncTime { get; set; }
        public object legalAgeGroupClassification { get; set; }
        public string mail { get; set; }
        public string mailNickname { get; set; }
        public object mobile { get; set; }
        public string onPremisesDistinguishedName { get; set; }
        public string onPremisesSecurityIdentifier { get; set; }
        public string[] otherMails { get; set; }
        public string passwordPolicies { get; set; }
        public Passwordprofile passwordProfile { get; set; }
        public string physicalDeliveryOfficeName { get; set; }
        public object postalCode { get; set; }
        public object preferredLanguage { get; set; }
        public Provisionedplan[] provisionedPlans { get; set; }
        public Provisioningerror[] provisioningErrors { get; set; }
        public string[] proxyAddresses { get; set; }
        public string refreshTokensValidFromDateTime { get; set; }
        public bool? showInAddressList { get; set; }
        public object[] signInNames { get; set; }
        public string sipProxyAddress { get; set; }
        public string state { get; set; }
        public object streetAddress { get; set; }
        public string surname { get; set; }
        public string telephoneNumber { get; set; }
        public string thumbnailPhotoodatamediaEditLink { get; set; }
        public string usageLocation { get; set; }
        public object[] userIdentities { get; set; }
        public string userPrincipalName { get; set; }
        public string userState { get; set; }
        public string userStateChangedOn { get; set; }
        public string userType { get; set; }
        public string extension_aa5a8156c3fa4cf1b44f13e43036af30_pcc_EmployeeStatus { get; set; }
        public string extension_aa5a8156c3fa4cf1b44f13e43036af30_pcc_EmployeeJDEAccount { get; set; }
        public string extension_aa5a8156c3fa4cf1b44f13e43036af30_pcc_EmployeeCompanyNumber { get; set; }
        public string extension_aa5a8156c3fa4cf1b44f13e43036af30_pcc_EmployeeClockId { get; set; }
        public string extension_aa5a8156c3fa4cf1b44f13e43036af30_pcc_EmployeeABNumber { get; set; }
        public string thumbnailPhotoodatamediaContentType { get; set; }
    }

    public class Passwordprofile
    {
        public object password { get; set; }
        public bool forceChangePasswordNextLogin { get; set; }
        public bool enforceChangePasswordPolicy { get; set; }
    }

    public class Assignedlicens
    {
        public string[] disabledPlans { get; set; }
        public string skuId { get; set; }
    }

    public class Assignedplan
    {
        public string assignedTimestamp { get; set; }
        public string capabilityStatus { get; set; }
        public string service { get; set; }
        public string servicePlanId { get; set; }
    }

    public class Provisionedplan
    {
        public string capabilityStatus { get; set; }
        public string provisioningStatus { get; set; }
        public string service { get; set; }
    }

    public class Provisioningerror
    {
        public string errorDetail { get; set; }
        public bool resolved { get; set; }
        public string service { get; set; }
        public string timestamp { get; set; }
    }

  
    
}
