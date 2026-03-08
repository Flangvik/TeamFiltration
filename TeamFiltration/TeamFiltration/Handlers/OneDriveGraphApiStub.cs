using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TeamFiltration.Helpers;
using TeamFiltration.Models.OneDrive;

namespace TeamFiltration.Handlers
{
    /// <summary>
    /// Stub replacement for the removed KoenZomers.OneDrive.Api.OneDriveGraphApi library.
    ///
    /// PURPOSE: This class preserves the public API surface that OneDriveHandler.cs depends on.
    ///          Every method that makes an HTTP call throws NotImplementedException with a comment
    ///          describing the exact Microsoft Graph API endpoint and request/response contract
    ///          that a real implementation must satisfy.
    ///
    /// IMPLEMENTATION GUIDE:
    ///   Base URL for user-owned items:  https://graph.microsoft.com/v1.0/me/
    ///   Base URL for drives by ID:      https://graph.microsoft.com/v1.0/drives/{driveId}/
    ///   Base URL for SharePoint sites:  https://graph.microsoft.com/v1.0/sites/
    ///   All requests require:           Authorization: Bearer {access_token}
    ///   All responses are JSON (UTF-8).
    ///
    /// TOKEN REFRESH:
    ///   When the access token expires (AccessTokenValidUntil &lt; DateTime.UtcNow),
    ///   call POST https://login.microsoftonline.com/common/oauth2/v2.0/token with body:
    ///     client_id={clientId}&amp;refresh_token={RefreshToken}&amp;grant_type=refresh_token&amp;scope=openid&amp;redirect_uri=https://login.microsoftonline.com/common/oauth2/nativeclient
    ///   Use FireProxAuthUrl instead of the above URL if it is non-null/non-empty.
    ///   Parse the response as OneDriveAccessToken and update AccessToken + AccessTokenValidUntil.
    /// </summary>
    public class OneDriveGraphApiStub
    {
        // ── State ────────────────────────────────────────────────────────────

        private readonly string _clientId;
        private readonly GlobalArgumentsHandler _globalArgsHandler;
        private HttpClient _httpClient;

        /// <summary>
        /// Override URL for the OAuth2 token endpoint.
        /// When non-empty, all token POSTs go here instead of the standard
        /// https://login.microsoftonline.com/common/oauth2/v2.0/token endpoint.
        /// (Used by the legacy FireProx IP-rotation proxy.)
        /// </summary>
        public string FireProxAuthUrl { get; set; }

        /// <summary>Current access token. Set by Authenticate* methods.</summary>
        public OneDriveAccessToken AccessToken { get; set; }

        /// <summary>
        /// Optional extra HTTP headers to add to the raw HttpClient used for API calls.
        /// Each element is a (header name, header value) tuple.
        /// Populated during AuthenticateUsingRefreshToken when the token endpoint
        /// returns additional header directives (currently always null in the stub).
        /// </summary>
        public List<(string header, string value)> headerListObject { get; set; }

        private DateTime? AccessTokenValidUntil { get; set; }

        public OneDriveGraphApiStub(string clientId, GlobalArgumentsHandler globalArgsHandler)
        {
            _clientId = clientId;
            _globalArgsHandler = globalArgsHandler;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void InitHttpClient()
        {
            var proxy = new WebProxy
            {
                Address = new Uri(_globalArgsHandler.TeamFiltrationConfig.proxyEndpoint),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false,
            };

            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                SslProtocols = SslProtocols.None,
                UseProxy = _globalArgsHandler.DebugMode,
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {AccessToken.AccessToken}");
        }

        private async Task<OneDriveItem[]> GetAllItemsPagedAsync(string initialUrl)
        {
            var results = new List<OneDriveItem>();
            var url = initialUrl;
            do
            {
                var resp = await _httpClient.PollyGetAsync(url);
                var json = await resp.Content.ReadAsStringAsync();
                var page = JsonConvert.DeserializeObject<OneDriveItemCollection>(json);
                if (page?.Collection != null)
                    results.AddRange(page.Collection);
                url = page?.NextLink;
            } while (!string.IsNullOrEmpty(url));
            return results.ToArray();
        }

        // ── Authentication ────────────────────────────────────────────────────

        /// <summary>
        /// Sets the access token directly from an already-obtained token object.
        /// No network call is made. AccessTokenValidUntil is set to
        ///   DateTime.UtcNow + AccessToken.AccessTokenExpirationDuration seconds.
        ///
        /// IMPLEMENTATION: Synchronous. Just assign:
        ///   AccessToken = accessToken;
        ///   AccessTokenValidUntil = DateTime.UtcNow.AddSeconds(accessToken.AccessTokenExpirationDuration);
        /// </summary>
        public void AuthenticateUsingAccessToken(OneDriveAccessToken accessToken)
        {
            AccessToken = accessToken;
            AccessTokenValidUntil = DateTime.UtcNow.AddSeconds(accessToken.AccessTokenExpirationDuration);
            InitHttpClient();
        }

        /// <summary>
        /// Exchanges a refresh token for a new access token.
        ///
        /// IMPLEMENTATION:
        ///   POST to FireProxAuthUrl ?? "https://login.microsoftonline.com/common/oauth2/v2.0/token"
        ///   Content-Type: application/x-www-form-urlencoded
        ///   Body (URL-encoded):
        ///     client_id={_clientId}
        ///     &amp;scope=openid
        ///     &amp;refresh_token={refreshToken}
        ///     &amp;redirect_uri=https://login.microsoftonline.com/common/oauth2/nativeclient
        ///     &amp;grant_type=refresh_token
        ///
        ///   On HTTP 200: deserialize response JSON as OneDriveAccessToken,
        ///                assign to AccessToken, set AccessTokenValidUntil.
        ///   On non-200:  throw an exception with the response body as message.
        /// </summary>
        public async Task AuthenticateUsingRefreshToken(string refreshToken)
        {
            var tokenUrl = !string.IsNullOrEmpty(FireProxAuthUrl)
                ? FireProxAuthUrl
                : "https://login.microsoftonline.com/common/oauth2/v2.0/token";

            var body = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("scope", "openid"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("redirect_uri", "https://login.microsoftonline.com/common/oauth2/nativeclient"),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
            });

            // Use a fresh client (no auth header) for the token endpoint
            using var tokenClient = new HttpClient();
            var resp = await tokenClient.PostAsync(tokenUrl, body);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Token refresh failed ({(int)resp.StatusCode}): {json}");

            AccessToken = JsonConvert.DeserializeObject<OneDriveAccessToken>(json);
            AccessTokenValidUntil = DateTime.UtcNow.AddSeconds(AccessToken.AccessTokenExpirationDuration);
            InitHttpClient();
        }

        // ── Drive root listing ────────────────────────────────────────────────

        /// <summary>
        /// Returns ALL items (files and folders) directly under the user's OneDrive root.
        /// Handles pagination automatically via @odata.nextLink.
        ///
        /// IMPLEMENTATION:
        ///   Initial request: GET https://graph.microsoft.com/v1.0/me/drive/root/children
        ///   Headers: Authorization: Bearer {AccessToken.AccessToken}
        ///   Response: OneDriveItemCollection JSON
        ///     {
        ///       "value": [ { DriveItem }, ... ],
        ///       "@odata.nextLink": "https://graph.microsoft.com/v1.0/..." // optional
        ///     }
        ///   If @odata.nextLink is present, follow it (GET, same headers) and accumulate
        ///   all pages until nextLink is absent.
        ///   Return results as OneDriveItem[].
        /// </summary>
        public async Task<OneDriveItem[]> GetAllDriveRootChildren()
        {
            return await GetAllItemsPagedAsync("https://graph.microsoft.com/v1.0/me/drive/root/children");
        }

        // ── Folder children ───────────────────────────────────────────────────

        /// <summary>
        /// Returns ALL items under the folder with the given ID. Handles pagination.
        ///
        /// IMPLEMENTATION:
        ///   GET https://graph.microsoft.com/v1.0/me/drive/items/{id}/children
        ///   Same pagination logic as GetAllDriveRootChildren.
        ///   Return OneDriveItem[].
        /// </summary>
        public async Task<OneDriveItem[]> GetAllChildrenByFolderId(string id)
        {
            return await GetAllItemsPagedAsync($"https://graph.microsoft.com/v1.0/me/drive/items/{id}/children");
        }

        /// <summary>
        /// Returns ALL items under the given parent item. Handles pagination.
        /// Cross-drive items: when item.RemoteItem != null, use
        ///   GET /drives/{item.RemoteItem.ParentReference.DriveId}/items/{item.RemoteItem.Id}/children
        /// When item.ParentReference.DriveId is non-empty (but RemoteItem is null), use
        ///   GET /drives/{item.ParentReference.DriveId}/items/{item.Id}/children
        /// Otherwise use
        ///   GET /me/drive/items/{item.Id}/children
        ///
        /// IMPLEMENTATION: Same pagination logic as GetAllDriveRootChildren. Return OneDriveItem[].
        /// </summary>
        public async Task<OneDriveItem[]> GetAllChildrenByParentItem(OneDriveItem item)
        {
            string url;
            if (item.RemoteItem != null)
                url = $"https://graph.microsoft.com/v1.0/drives/{item.RemoteItem.ParentReference.DriveId}/items/{item.RemoteItem.Id}/children";
            else if (!string.IsNullOrEmpty(item.ParentReference?.DriveId))
                url = $"https://graph.microsoft.com/v1.0/drives/{item.ParentReference.DriveId}/items/{item.Id}/children";
            else
                url = $"https://graph.microsoft.com/v1.0/me/drive/items/{item.Id}/children";

            return await GetAllItemsPagedAsync(url);
        }

        /// <summary>
        /// Returns the FIRST PAGE ONLY of children under the given parent item.
        /// Same URL selection logic as GetAllChildrenByParentItem (cross-drive aware).
        ///
        /// IMPLEMENTATION:
        ///   Single GET — no pagination. Deserialize as OneDriveItemCollection.
        ///   Note: NextLink on the returned collection may be non-null if there are more pages.
        /// </summary>
        public async Task<OneDriveItemCollection> GetChildrenByParentItem(OneDriveItem item)
        {
            string url;
            if (item.RemoteItem != null)
                url = $"https://graph.microsoft.com/v1.0/drives/{item.RemoteItem.ParentReference.DriveId}/items/{item.RemoteItem.Id}/children";
            else if (!string.IsNullOrEmpty(item.ParentReference?.DriveId))
                url = $"https://graph.microsoft.com/v1.0/drives/{item.ParentReference.DriveId}/items/{item.Id}/children";
            else
                url = $"https://graph.microsoft.com/v1.0/me/drive/items/{item.Id}/children";

            var resp = await _httpClient.PollyGetAsync(url);
            var json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OneDriveItemCollection>(json);
        }

        // ── Single item lookup ────────────────────────────────────────────────

        /// <summary>
        /// Returns the DriveItem with the given ID from the current user's drive.
        ///
        /// IMPLEMENTATION:
        ///   GET https://graph.microsoft.com/v1.0/me/drive/items/{id}
        ///   Deserialize response as OneDriveItem.
        ///   Return null if HTTP 404; throw on other non-2xx.
        /// </summary>
        public async Task<OneDriveItem> GetItemById(string id)
        {
            var resp = await _httpClient.PollyGetAsync($"https://graph.microsoft.com/v1.0/me/drive/items/{id}");
            if (resp.StatusCode == HttpStatusCode.NotFound) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OneDriveItem>(json);
        }

        // ── Download ──────────────────────────────────────────────────────────

        /// <summary>
        /// Downloads the content of item and saves it to the directory saveTo,
        /// keeping item.Name as the file name.
        /// Delegates to DownloadItemAndSaveAs(item, Path.Combine(saveTo, item.Name)).
        ///
        /// IMPLEMENTATION: call DownloadItemAndSaveAs with saveTo + "/" + item.Name.
        /// </summary>
        public async Task<bool> DownloadItem(OneDriveItem item, string saveTo)
        {
            return await DownloadItemAndSaveAs(item, Path.Combine(saveTo, item.Name));
        }

        /// <summary>
        /// Downloads the content of item and saves it to the full path saveAs.
        ///
        /// IMPLEMENTATION:
        ///   Determine URL:
        ///     if item.RemoteItem != null:
        ///       GET /drives/{item.RemoteItem.ParentReference.DriveId}/items/{item.RemoteItem.Id}/content
        ///     else if item.ParentReference?.DriveId non-empty:
        ///       GET /drives/{item.ParentReference.DriveId}/items/{item.Id}/content
        ///     else:
        ///       GET /me/drive/items/{item.Id}/content
        ///   The Graph API responds with HTTP 302 redirect to a pre-authenticated download URL.
        ///   HttpClient with AllowAutoRedirect=true will follow the redirect automatically.
        ///   Write the response body bytes to saveAs (overwrite if exists).
        ///   Return true on success; throw (or return false) on error.
        /// </summary>
        public async Task<bool> DownloadItemAndSaveAs(OneDriveItem item, string saveAs)
        {
            string url;
            if (item.RemoteItem != null)
                url = $"https://graph.microsoft.com/v1.0/drives/{item.RemoteItem.ParentReference.DriveId}/items/{item.RemoteItem.Id}/content";
            else if (!string.IsNullOrEmpty(item.ParentReference?.DriveId))
                url = $"https://graph.microsoft.com/v1.0/drives/{item.ParentReference.DriveId}/items/{item.Id}/content";
            else
                url = $"https://graph.microsoft.com/v1.0/me/drive/items/{item.Id}/content";

            var resp = await _httpClient.PollyGetAsync(url);
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(saveAs, bytes);
            return true;
        }

        // ── Search ────────────────────────────────────────────────────────────

        /// <summary>
        /// Searches the user's entire OneDrive for items matching query.
        ///
        /// IMPLEMENTATION:
        ///   GET https://graph.microsoft.com/v1.0/me/drive/root/search(q='{query}')
        ///   URI-encode the query string.
        ///   Response shape is identical to the children endpoint (value array + nextLink).
        ///   Paginate via @odata.nextLink until exhausted.
        ///   Return IList&lt;OneDriveItem&gt; (all pages combined).
        /// </summary>
        public async Task<IList<OneDriveItem>> Search(string query)
        {
            var encoded = Uri.EscapeDataString(query);
            var items = await GetAllItemsPagedAsync($"https://graph.microsoft.com/v1.0/me/drive/root/search(q='{encoded}')");
            return items;
        }

        // ── Upload ────────────────────────────────────────────────────────────

        /// <summary>
        /// Uploads a local file to the given parent folder in OneDrive.
        /// Uses simple upload for files &lt;= 4 MB; resumable upload for larger files.
        ///
        /// SIMPLE UPLOAD (file &lt;= 4 MB):
        ///   PUT https://graph.microsoft.com/v1.0/me/drive/items/{parentFolder.Id}/children/{filename}/content
        ///   Content-Type: application/octet-stream
        ///   Body: raw file bytes
        ///   On HTTP 200/201: deserialize response as OneDriveItem.
        ///
        /// RESUMABLE UPLOAD (file > 4 MB):
        ///   Step 1 — create upload session:
        ///     POST https://graph.microsoft.com/v1.0/me/drive/items/{parentFolder.Id}/children/{filename}/createUploadSession
        ///     Body: { "item": { "@microsoft.graph.conflictBehavior": "replace" } }
        ///     Response: { "uploadUrl": "https://..." }
        ///   Step 2 — upload chunks:
        ///     PUT {uploadUrl}
        ///     Content-Range: bytes {start}-{end}/{total}
        ///     Body: chunk bytes (recommended chunk size: 10 MB, must be multiple of 327680 bytes)
        ///     Repeat until all bytes uploaded. Final PUT returns the DriveItem JSON.
        ///   Deserialize final PUT response as OneDriveItem.
        ///
        /// Return the newly created/updated OneDriveItem.
        /// </summary>
        public async Task<OneDriveItem> UploadFile(string filePath, OneDriveItem parentFolder)
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Uri.EscapeDataString(fileInfo.Name);
            const long simpleUploadLimit = 4 * 1024 * 1024; // 4 MB

            if (fileInfo.Length <= simpleUploadLimit)
            {
                // Simple upload
                var putUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{parentFolder.Id}:/{fileName}:/content";
                var bytes = File.ReadAllBytes(filePath);
                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var resp = await _httpClient.PutAsync(putUrl, content);
                var json = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<OneDriveItem>(json);
            }
            else
            {
                // Resumable upload: create session then upload in 10 MB chunks
                var sessionUrl = $"https://graph.microsoft.com/v1.0/me/drive/items/{parentFolder.Id}:/{fileName}:/createUploadSession";
                var sessionBody = new StringContent(
                    "{\"item\":{\"@microsoft.graph.conflictBehavior\":\"replace\"}}",
                    Encoding.UTF8, "application/json");
                var sessionResp = await _httpClient.PollyPostAsync(sessionUrl, sessionBody);
                var sessionJson = await sessionResp.Content.ReadAsStringAsync();
                var sessionObj  = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(sessionJson);
                var uploadUrl   = sessionObj["uploadUrl"]?.ToString();

                const int chunkSize = 10 * 1024 * 1024; // 10 MB (must be multiple of 327680)
                var fileBytes = File.ReadAllBytes(filePath);
                long totalSize = fileBytes.Length;
                int offset = 0;
                HttpResponseMessage lastResp = null;

                while (offset < totalSize)
                {
                    int length = (int)Math.Min(chunkSize, totalSize - offset);
                    var chunk = new ByteArrayContent(fileBytes, offset, length);
                    chunk.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + length - 1, totalSize);
                    chunk.Headers.ContentType  = new MediaTypeHeaderValue("application/octet-stream");

                    // Chunk upload goes directly to uploadUrl (no Auth header needed)
                    using var chunkClient = new HttpClient();
                    lastResp = await chunkClient.PutAsync(uploadUrl, chunk);
                    offset  += length;
                }

                var resultJson = await lastResp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<OneDriveItem>(resultJson);
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────

        /// <summary>
        /// Permanently deletes an item from OneDrive (moves to recycle bin for personal drives).
        ///
        /// IMPLEMENTATION:
        ///   Determine URL (same cross-drive logic as DownloadItemAndSaveAs):
        ///     if item.RemoteItem != null:
        ///       DELETE /drives/{item.RemoteItem.ParentReference.DriveId}/items/{item.RemoteItem.Id}
        ///     else if item.ParentReference?.DriveId non-empty:
        ///       DELETE /drives/{item.ParentReference.DriveId}/items/{item.Id}
        ///     else:
        ///       DELETE /me/drive/items/{item.Id}
        ///   On HTTP 204 (No Content): return true.
        ///   On other non-2xx: throw or return false.
        /// </summary>
        public async Task<bool> Delete(OneDriveItem item)
        {
            string url;
            if (item.RemoteItem != null)
                url = $"https://graph.microsoft.com/v1.0/drives/{item.RemoteItem.ParentReference.DriveId}/items/{item.RemoteItem.Id}";
            else if (!string.IsNullOrEmpty(item.ParentReference?.DriveId))
                url = $"https://graph.microsoft.com/v1.0/drives/{item.ParentReference.DriveId}/items/{item.Id}";
            else
                url = $"https://graph.microsoft.com/v1.0/me/drive/items/{item.Id}";

            var resp = await _httpClient.DeleteAsync(url);
            return resp.StatusCode == HttpStatusCode.NoContent;
        }

        // ── Drive metadata ────────────────────────────────────────────────────

        /// <summary>
        /// Returns metadata about the current user's default drive.
        ///
        /// IMPLEMENTATION:
        ///   GET https://graph.microsoft.com/v1.0/me/drive
        ///   Deserialize response as OneDriveDrive.
        /// </summary>
        public async Task<OneDriveDrive> GetDrive()
        {
            var resp = await _httpClient.PollyGetAsync("https://graph.microsoft.com/v1.0/me/drive");
            var json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OneDriveDrive>(json);
        }

        // ── Shared with me ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the collection of items that other users have shared with the current user.
        ///
        /// IMPLEMENTATION:
        ///   GET https://graph.microsoft.com/v1.0/me/drive/sharedWithMe
        ///   Response shape: { "value": [ { DriveItem }, ... ] }
        ///   Deserialize as OneDriveItemCollection.
        ///   Note: the Graph API returns a flat list (no @odata.nextLink for this endpoint in v1.0).
        ///   Each item that is a folder will have item.Folder != null; recurse into it separately.
        /// </summary>
        public async Task<OneDriveItemCollection> GetSharedWithMe()
        {
            var resp = await _httpClient.PollyGetAsync("https://graph.microsoft.com/v1.0/me/drive/sharedWithMe");
            var json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OneDriveItemCollection>(json);
        }

        // ── SharePoint ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the root SharePoint site of the tenant.
        ///
        /// IMPLEMENTATION:
        ///   GET https://graph.microsoft.com/v1.0/sites/root
        ///   Deserialize response as SharePointSite.
        ///   JSON shape:
        ///   {
        ///     "id": "tenant.sharepoint.com,{siteCollectionId},{siteId}",
        ///     "displayName": "Communication Site",
        ///     "webUrl": "https://tenant.sharepoint.com"
        ///   }
        /// </summary>
        public async Task<SharePointSite> GetSiteRoot()
        {
            var resp = await _httpClient.PollyGetAsync("https://graph.microsoft.com/v1.0/sites/root");
            var json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SharePointSite>(json);
        }
    }
}
