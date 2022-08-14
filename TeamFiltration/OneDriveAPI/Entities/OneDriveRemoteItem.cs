using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// The Item resource type represents metadata for an item in OneDrive that links to a OneDriveItem stored on another OneDrive
    /// </summary>
    public class OneDriveRemoteItem : OneDriveItemBase
    {
        /// <summary>
        /// The unique identifier of the item within the linked OneDrive
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Size of the item in bytes. Read-only.
        /// </summary>
        [JsonProperty("size", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long Size { get; set; }

        /// <summary>
        /// URL that displays the resource in the browser. Read-only.
        /// </summary>
        [JsonProperty("webUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string WebUrl { get; set; }

        /// <summary>
        /// Parent information, if the item has a parent. Writeable
        /// </summary>
        [JsonProperty("parentReference", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveItemReference ParentReference { get; set; }

        /// <summary>
        /// Folder metadata, if the item is a folder. Read-only.
        /// </summary>
        [JsonProperty("folder", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveFolderFacet Folder { get; set; }

        /// <summary>
        /// File metadata, if the item is a file
        /// </summary>
        [JsonProperty("file", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveFileFacet File { get; set; }

        /// <summary>
        /// Date and time at which the item has been created
        /// </summary>
        [JsonProperty("createdDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public string CreatedDateTime { get; set; }

        /// <summary>
        /// Date and time at which the item was last modified
        /// </summary>
        [JsonProperty("lastModifiedDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public string LastModifiedDateTime { get; set; }

        /// <summary>
        /// User that last modified the contents of this item
        /// </summary>
        [JsonProperty("lastModifiedBy", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveUserProfile LastModifiedBy { get; set; }

        /// <summary>
        /// User that created the contents of this item
        /// </summary>
        [JsonProperty("createdBy", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveUserProfile CreatedBy { get; set; }

        /// <summary>
        /// SharePoint specific identifiers for this item
        /// </summary>
        [JsonProperty("sharepointIds", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveForBusinessSharePointId SharePointIds { get; set; }

        /// <summary>
        /// Name of the item
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Url to use with the WebDav protocol to access this item
        /// </summary>
        [JsonProperty("webDavUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string WebDavUrl { get; set; }

        /// <summary>
        /// Information about the owner of the shared item
        /// </summary>
        [JsonProperty("shared")]
        public OneDriveSharedItem Shared { get; set; }
    }
}
