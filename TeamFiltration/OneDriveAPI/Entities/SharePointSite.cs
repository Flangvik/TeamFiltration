using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Information regarding a SharePoint site
    /// </summary>
    public class SharePointSite : OneDriveItemBase
    {
        /// <summary>
        /// Date and time at which the site was created
        /// </summary>
        [JsonProperty("createdDateTime")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Description of the site
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Unique identifier of the site
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Date and time at which the site was last modified
        /// </summary>
        [JsonProperty("lastModifiedDateTime")]
        public DateTime? LastModifiedDateTime { get; set; }

        /// <summary>
        /// Name of the site
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Full URL to where the site resides
        /// </summary>
        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }

        //[JsonProperty("siteCollection")]
        //public SiteCollection SiteCollection { get; set; }

        /// <summary>
        /// Title of the site
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
    }
}
