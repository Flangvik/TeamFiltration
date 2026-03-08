using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeamFiltration.Models.OneDrive
{
    // ── Token ────────────────────────────────────────────────────────────────

    /// <summary>
    /// OAuth 2.0 access token returned by the Microsoft identity platform token endpoint.
    /// Endpoint: POST https://login.microsoftonline.com/common/oauth2/v2.0/token
    /// Grant types used by TeamFiltration: "refresh_token" and direct injection via AuthenticateUsingAccessToken.
    /// Graph API reference: https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow
    /// </summary>
    public class OneDriveAccessToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>Authentication token (same as access_token in practice).</summary>
        public string AuthenticationToken { get; set; }

        [JsonProperty("scope")]
        public string Scopes { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// Seconds until the access token expires.
        /// JSON field name: "expires_in".
        /// </summary>
        [JsonProperty("expires_in")]
        public int AccessTokenExpirationDuration { get; set; }
    }

    // ── Drive items ──────────────────────────────────────────────────────────

    /// <summary>
    /// Represents a file or folder in a OneDrive / SharePoint document library.
    /// Graph API reference: https://learn.microsoft.com/en-us/graph/api/resources/driveitem
    /// JSON shape (abridged):
    /// {
    ///   "id": "...",
    ///   "name": "...",
    ///   "size": 12345,
    ///   "lastModifiedDateTime": "2024-01-01T00:00:00Z",
    ///   "parentReference": { "id": "...", "driveId": "...", "path": "..." },
    ///   "file": {},          // present only if item is a file
    ///   "folder": {},        // present only if item is a folder
    ///   "remoteItem": { ... } // present only if item lives on a different drive
    /// }
    /// </summary>
    public class OneDriveItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("lastModifiedDateTime")]
        public DateTimeOffset LastModifiedDateTime { get; set; }

        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }

        /// <summary>Non-null when item is a file (use as null-check only).</summary>
        [JsonProperty("file")]
        public OneDriveFileFacet File { get; set; }

        /// <summary>Non-null when item is a folder (use as null-check only).</summary>
        [JsonProperty("folder")]
        public OneDriveFolderFacet Folder { get; set; }

        [JsonProperty("parentReference")]
        public OneDriveItemReference ParentReference { get; set; }

        /// <summary>Non-null when item is a remote reference to an item on another drive.</summary>
        [JsonProperty("remoteItem")]
        public OneDriveRemoteItem RemoteItem { get; set; }
    }

    /// <summary>
    /// Marker facet — present on an item when it is a file.
    /// JSON key: "file"
    /// </summary>
    public class OneDriveFileFacet { }

    /// <summary>
    /// Marker facet — present on an item when it is a folder.
    /// JSON key: "folder"
    /// </summary>
    public class OneDriveFolderFacet
    {
        [JsonProperty("childCount")]
        public int ChildCount { get; set; }
    }

    /// <summary>
    /// Parent reference included in every DriveItem.
    /// Used to navigate up the hierarchy and detect cross-drive items.
    /// </summary>
    public class OneDriveItemReference
    {
        /// <summary>Unique ID of the parent folder item.</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Drive ID of the drive that owns this parent folder.
        /// Non-empty value means the item lives on a drive other than the user's default drive.
        /// </summary>
        [JsonProperty("driveId")]
        public string DriveId { get; set; }

        /// <summary>
        /// Human-readable drive-relative path, e.g. "/drive/root:/Documents".
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }
    }

    /// <summary>
    /// Represents an item that resides on another user's or site's drive but is linked from the current user's drive.
    /// Graph API reference: https://learn.microsoft.com/en-us/graph/api/resources/remoteitem
    /// </summary>
    public class OneDriveRemoteItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>The parent reference on the *remote* drive.</summary>
        [JsonProperty("parentReference")]
        public OneDriveItemReference ParentReference { get; set; }
    }

    /// <summary>
    /// A page of DriveItem children returned by the Graph API.
    /// Graph API reference: https://learn.microsoft.com/en-us/graph/api/driveitem-list-children
    /// JSON shape:
    /// {
    ///   "value": [ { DriveItem }, ... ],
    ///   "@odata.nextLink": "https://graph.microsoft.com/v1.0/me/drive/items/{id}/children?$skiptoken=..."
    /// }
    /// </summary>
    public class OneDriveItemCollection
    {
        [JsonProperty("value")]
        public List<OneDriveItem> Collection { get; set; } = new List<OneDriveItem>();

        /// <summary>
        /// URL for the next page of results. Null/empty when this is the last page.
        /// Follow this URL (with Authorization header) to retrieve more items.
        /// </summary>
        [JsonProperty("@odata.nextLink")]
        public string NextLink { get; set; }
    }

    // ── Drive ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Top-level container representing a OneDrive or SharePoint document library drive.
    /// Graph API reference: https://learn.microsoft.com/en-us/graph/api/resources/drive
    /// Endpoint: GET https://graph.microsoft.com/v1.0/me/drive
    /// JSON shape (abridged):
    /// {
    ///   "id": "b!...",
    ///   "driveType": "personal" | "business" | "documentLibrary",
    ///   "quota": { "total": ..., "used": ..., "remaining": ... }
    /// }
    /// </summary>
    public class OneDriveDrive
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>"personal", "business", or "documentLibrary".</summary>
        [JsonProperty("driveType")]
        public string DriveType { get; set; }

        [JsonProperty("quota")]
        public OneDriveDriveQuota Quota { get; set; }
    }

    public class OneDriveDriveQuota
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("used")]
        public long Used { get; set; }

        [JsonProperty("remaining")]
        public long Remaining { get; set; }
    }

    // ── SharedWithMe ──────────────────────────────────────────────────────────

    /// <summary>
    /// Collection of items shared with the current user.
    /// Graph API reference: https://learn.microsoft.com/en-us/graph/api/drive-sharedwithme
    /// Endpoint: GET https://graph.microsoft.com/v1.0/me/drive/sharedWithMe
    /// JSON shape: same as OneDriveItemCollection — array under "value" key.
    /// </summary>
    public class OneDriveSharedWithMeItemCollection
    {
        [JsonProperty("value")]
        public List<OneDriveItem> Collection { get; set; } = new List<OneDriveItem>();
    }

    // ── SharePoint ────────────────────────────────────────────────────────────

    /// <summary>
    /// Represents the root SharePoint site of the tenant.
    /// Graph API reference: https://learn.microsoft.com/en-us/graph/api/resources/site
    /// Endpoint: GET https://graph.microsoft.com/v1.0/sites/root
    /// JSON shape (abridged):
    /// {
    ///   "id": "tenant.sharepoint.com,{siteId},{webId}",
    ///   "displayName": "...",
    ///   "webUrl": "https://tenant.sharepoint.com"
    /// }
    /// </summary>
    public class SharePointSite
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }
    }
}
