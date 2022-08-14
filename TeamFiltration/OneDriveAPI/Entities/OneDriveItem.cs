using System;
using KoenZomers.OneDrive.Api.Enums;
using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// The Item resource type represents metadata for an item in OneDrive. All top-level filesystem objects in OneDrive are Item resources. If an item is a Folder or File facet, the Item resource will contain a value for either the folder or file property, respectively.
    /// </summary>
    public class OneDriveItem : OneDriveItemBase
    {
        /// <summary>
        /// The unique identifier of the item within the Drive. Read-only.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// The name of the item (filename and extension). Writable.
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// eTag for the entire item (metadata + content). Read-only.
        /// </summary>
        [JsonProperty("etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ETag { get; set; }

        /// <summary>
        /// An eTag for the content of the item. This eTag is not changed if only the metadata is changed. Read-only.
        /// </summary>
        [JsonProperty("ctag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CTag { get; set; }

        /// <summary>
        /// Identity of the user, device, and application which created the item. Read-only.
        /// </summary>
        [JsonProperty("createdBy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public OneDriveIdentitySet CreatedBy { get; set; }

        /// <summary>
        /// Date and time of item creation. Read-only.
        /// </summary>
        [JsonProperty("createdDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>
        /// Identity of the user, device, and application which last modified the item. Read-only.
        /// </summary>
        [JsonProperty("lastModifiedBy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public OneDriveIdentitySet LastModifiedBy { get; set; }

        /// <summary>
        /// Date and time the item was last modified. Read-only.
        /// </summary>
        [JsonProperty("lastModifiedDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset LastModifiedDateTime { get; set; }

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
        /// File metadata, if the item is a file. Read-only.
        /// </summary>
        [JsonProperty("file", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveFileFacet File { get; set; }

        /// <summary>
        /// Image metadata, if the item is an image. Read-only.
        /// </summary>
        [JsonProperty("image", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveImageFacet Image { get; set; }

        /// <summary>
        ///  Photo metadata, if the item is a photo. Read-only. 
        /// </summary>
        [JsonProperty("photo", NullValueHandling = NullValueHandling.Ignore)]
        public OneDrivePhotoFacet Photo { get; set; }

        /// <summary>
        /// Audio metadata, if the item is an audio file. Read-only.
        /// </summary>
        [JsonProperty("audio", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveAudioFacet Audio { get; set; }

        /// <summary>
        /// Video metadata, if the item is a video. Read-only.
        /// </summary>
        [JsonProperty("video", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveVideoFacet Video { get; set; }

        /// <summary>
        /// Location metadata, if the item has location data. Read-only.
        /// </summary>
        [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveLocationFacet Location { get; set; }

        /// <summary>
        /// Information about the deleted state of the item. Read-only.
        /// </summary>
        [JsonProperty("deleted", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveTombstoneFacet Deleted { get; set; }

        [JsonProperty("specialFolder", NullValueHandling = NullValueHandling.Ignore)]
        public OneDriveSpecialFolderFacet SpecialFolder { get; set; }

        /// <summary>
        /// The conflict resolution behavior for actions that create a new item. An item will never be returned with this annotation. Write-only.
        /// </summary>
        [JsonProperty("@name.conflictBehavior", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public NameConflictBehavior? NameConflictBehahiorAnnotation { get; set; }

        /// <summary>
        /// A Url that can be used to download this file's content. Authentication is not required with this URL. Read-only.
        /// </summary>
        [JsonProperty("@content.downloadUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DownloadUrlAnnotation { get; set; }

        /// <summary>
        /// When issuing a PUT request, this instance annotation can be used to instruct the service to download the contents of the URL, and store it as the file. Write-only.
        /// </summary>
        [JsonProperty("@content.sourceUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SourceUrlAnnotation { get; set; }

        [JsonProperty("children@odata.nextLink", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ChildrenNextLinkAnnotation { get; set; }

        /// <summary>
        /// Collection containing ThumbnailSet objects associated with the item. For more info, see getting thumbnails.
        /// </summary>
        [JsonProperty("thumbnails", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public OneDriveThumbnailSet[] Thumbnails { get; set; }

        /// <summary>
        /// Collection containing Item objects for the immediate children of Item. Only items representing folders have children.
        /// </summary>
        [JsonProperty("children", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public OneDriveItem[] Children { get; set; }

        /// <summary>
        /// If containing information, it regards a OneDriveItem stored on another OneDrive but linked by the current OneDrive
        /// </summary>
        [JsonProperty("remoteItem", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public OneDriveRemoteItem RemoteItem{ get; set; }

        /// <summary>
        /// Information about the owner of a shared item
        /// </summary>
        [JsonProperty("shared")]
        public OneDriveSharedItem Shared { get; set; }
    }
}
