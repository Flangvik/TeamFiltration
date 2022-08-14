using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Entity representing a reference to a SharePoint list in OneDrive for Business
    /// </summary>
    public class OneDriveForBusinessSharePointId
    {
        /// <summary>
        /// Unique numeric identifier of the item in the SharePoint list
        /// </summary>
        [JsonProperty("listItemId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ListItemId { get; set; }

        /// <summary>
        /// Unique GUID identifier of the item in the SharePoint list
        /// </summary>
        [JsonProperty("listItemUniqueId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ListItemUniqueId { get; set; }

        /// <summary>
        /// Unique GUID identifier of the site collection the SharePoint list resides in
        /// </summary>
        [JsonProperty("siteId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SiteId { get; set; }

        /// <summary>
        /// Unique GUID identifier of the site the SharePoint list resides in
        /// </summary>
        [JsonProperty("webId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string WebId { get; set; }

        /// <summary>
        /// Unique GUID identifier of the SharePoint list
        /// </summary>
        [JsonProperty("listId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ListId { get; set; }
    }
}
