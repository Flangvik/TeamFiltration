using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KoenZomers.OneDrive.Api.Entities;
using KoenZomers.OneDrive.Api.Helpers;
using Newtonsoft.Json;
using KoenZomers.OneDrive.Api.Enums;
using System.Net.Http;

namespace KoenZomers.OneDrive.Api
{
    /// <summary>
    /// API for OneDrive for Business on Office 365
    /// Create your own Client ID / Client Secret at https://azure.com
    /// </summary>
    public class OneDriveForBusinessO365Api : OneDriveApi
    {
        #region Constants

        /// <summary>
        /// The url to provide as the redirect URL after successful authentication
        /// </summary>
        public override string AuthenticationRedirectUrl { get; set; } = "https://login.live.com/oauth20_desktop.srf";

        /// <summary>
        /// String formatted Uri that needs to be called to authenticate
        /// </summary>
        protected override string AuthenticateUri => "https://login.microsoftonline.com/common/oauth2/authorize?response_type=code&client_id={0}&redirect_uri={1}";

        /// <summary>
        /// String formatted Uri that can be called to sign out from the OneDrive API
        /// </summary>
        public override string SignoutUri => "https://login.microsoftonline.com/logout.srf";

        /// <summary>
        /// The url where an access token can be obtained
        /// </summary>
        protected override string AccessTokenUri => "https://login.microsoftonline.com/common/oauth2/token";

        /// <summary>
        /// The url which can be called to discover available services for an user in Office 365
        /// </summary>
        protected const string ServiceDiscoveryUri = "https://api.office.com/discovery/v2.0/me/services/";

        /// <summary>
        /// Defines the maximum allowed file size that can be used for basic uploads
        /// </summary>
        public new static long MaximumBasicFileUploadSizeInBytes = 5 * 1024;

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates a new instance of the OneDrive for Business API
        /// </summary>
        /// <param name="clientId">OneDrive Client ID to use to connect</param>
        /// <param name="clientSecret">OneDrive Client Secret to use to connect</param>
        public OneDriveForBusinessO365Api(string clientId, string clientSecret) : base(clientId, clientSecret)
        {
        }

        #endregion

        #region Public Methods - Authentication

        /// <summary>
        /// Returns the Uri that needs to be called to authenticate to the OneDrive for Business API
        /// </summary>
        /// <returns>Uri that needs to be called in a browser to authenticate to the OneDrive for Business API</returns>
        public override Uri GetAuthenticationUri()
        {
            var uri = string.Format(AuthenticateUri, ClientId, AuthenticationRedirectUrl);
            return new Uri(uri);
        }

        /// <summary>
        /// Gets an access token from the provided authorization token
        /// </summary>
        /// <param name="authorizationToken">Authorization token</param>
        /// <param name="resourceIdentifier">Resource to request an access token for</param>
        /// <returns>Access token for OneDrive</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected async Task<OneDriveAccessToken> GetAccessTokenFromAuthorizationToken(string authorizationToken, string resourceIdentifier)
        {
            var queryBuilder = new QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("redirect_uri", AuthenticationRedirectUrl);
            queryBuilder.Add("client_secret", ClientSecret);
            queryBuilder.Add("code", authorizationToken);
            queryBuilder.Add("resource", resourceIdentifier);
            queryBuilder.Add("grant_type", "authorization_code");
            return await PostToTokenEndPoint(queryBuilder);
        }

        /// <summary>
        /// Gets an access token from the provided refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <param name="resourceIdentifier">Resource to request an access token for</param>
        /// <returns>Access token for OneDrive</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected async Task<OneDriveAccessToken> GetAccessTokenFromRefreshToken(string refreshToken, string resourceIdentifier)
        {
            var queryBuilder = new QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("redirect_uri", AuthenticationRedirectUrl);
            queryBuilder.Add("client_secret", ClientSecret);
            queryBuilder.Add("refresh_token", refreshToken);
            queryBuilder.Add("resource", resourceIdentifier);
            queryBuilder.Add("grant_type", "refresh_token");
            return await PostToTokenEndPoint(queryBuilder);
        }

        /// <summary>
        /// Gets an access token from the provided refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Access token for OneDrive or NULL if unable to retrieve an access token</returns>
        protected override async Task<OneDriveAccessToken> GetAccessTokenFromRefreshToken(string refreshToken)
        {
            var discoveryAccessToken = await GetAccessTokenFromRefreshToken(refreshToken, "https://api.office.com/discovery/");
            var oneDriveForBusinessService = await DiscoverOneDriveForBusinessService(discoveryAccessToken.AccessToken);

            OneDriveApiBaseUrl = string.Concat(oneDriveForBusinessService.ServiceEndPointUri, "/");

            var oneDriveForBusinessAccessToken = await GetAccessTokenFromRefreshToken(refreshToken, oneDriveForBusinessService.ServiceResourceId);
            return oneDriveForBusinessAccessToken;
        }

        /// <summary>
        /// Gets an access token from the provided authorization token
        /// </summary>
        /// <param name="authorizationToken">Authorization token</param>
        /// <returns>Access token for OneDrive or NULL if unable to retrieve an access token</returns>
        protected override async Task<OneDriveAccessToken> GetAccessTokenFromAuthorizationToken(string authorizationToken)
        {
            var discoveryAccessToken = await GetAccessTokenFromAuthorizationToken(authorizationToken, "https://api.office.com/discovery/");
            var oneDriveForBusinessService = await DiscoverOneDriveForBusinessService(discoveryAccessToken.AccessToken);

            OneDriveApiBaseUrl = string.Concat(oneDriveForBusinessService.ServiceEndPointUri, "/");

            var oneDriveForBusinessAccessToken = await GetAccessTokenFromRefreshToken(discoveryAccessToken.RefreshToken, oneDriveForBusinessService.ServiceResourceId);
            return oneDriveForBusinessAccessToken;
        }

        /// <summary>
        /// Use the Office 365 Discovery Service to return all available services for the provided Access Token
        /// </summary>
        /// <param name="accessToken">Access Token to query available Office 365 Services for</param>
        /// <returns>Set with discovered services available for the provided Access Token</returns>
        private async Task<ServiceDiscoverySet> DiscoverOffice365Services(string accessToken)
        {
            // Create an HTTPClient instance to communicate with the REST API of OneDrive
            var client = CreateHttpClient();

            // Provide the access token through a bearer authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

            // Send the request to the OneDrive API
            var response = await client.GetAsync(ServiceDiscoveryUri);

            // Verify if the response was successful
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }

            // Retrieve the results from the Office 365 Discovery Service
            var result = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(result))
            {
                return null;
            }

            // Get the first Office 365 Service
            try
            {
                var discoveryResult = JsonConvert.DeserializeObject<ServiceDiscoverySet>(result);

                if (discoveryResult.Services.Count == 0)
                {
                    return null;
                }

                // Service discovery successful
                return discoveryResult;
            }
            catch (JsonReaderException e)
            {
                throw new Exceptions.InvalidResponseException(result, e);
            }
        }

        /// <summary>
        /// Use the Office 365 Discovery Service to try to locate the OneDrive for Business endpoint for the current user
        /// </summary>
        /// <param name="accessToken">An Access Token to use to query the Office 365 Discovery Service</param>
        /// <returns>Discovered service details</returns>
        private async Task<ServiceDiscoveryItem> DiscoverOneDriveForBusinessService(string accessToken)
        {
            var discoveredServices = await DiscoverOffice365Services(accessToken);

            var oneDriveForBusinessService = discoveredServices.Services.FirstOrDefault(service => service.Capability == "MyFiles" && service.ServiceApiVersion == "v2.0");
            return oneDriveForBusinessService;
        }

        #endregion

        #region Public Methods - Validate

        /// <summary>
        /// Validates if the provided filename is valid to be used on OneDrive
        /// </summary>
        /// <param name="filename">Filename to validate</param>
        /// <returns>True if filename is valid to be used, false if it isn't</returns>
        public override bool ValidFilename(string filename)
        {
            char[] restrictedCharacters = { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '#', '%' };
            return filename.IndexOfAny(restrictedCharacters) == -1;
        }

        #endregion

        #region Public Methods - OneDrive for Business Only

        /// <summary>
        /// Returns all the items that have been shared by others through OneDrive for Business with the current user
        /// </summary>
        /// <returns>Collection with items that have been shared by others with the current user</returns>
        public override async Task<OneDriveItemCollection> GetSharedWithMe()
        {
            var oneDriveItems = await GetData<OneDriveItemCollection>("drive/view.sharedWithMe");
            return oneDriveItems;
        }

        /// <summary>
        /// Shares a OneDrive item
        /// </summary>
        /// <param name="itemPath">The path to the OneDrive item to share</param>
        /// <param name="linkType">Type of sharing to request</param>
        /// <returns>OneDrivePermission entity representing the share or NULL if the operation fails</returns>
        public override async Task<OneDrivePermission> ShareItem(string itemPath, OneDriveLinkType linkType)
        {
            return await ShareItemInternal(string.Concat("drive/root:/", itemPath, ":/createLink"), linkType);
        }

        /// <summary>
        /// Shares a OneDrive item
        /// </summary>
        /// <param name="item">The OneDrive item to share</param>
        /// <param name="linkType">Type of sharing to request</param>
        /// <returns>OneDrivePermission entity representing the share or NULL if the operation fails</returns>
        public override async Task<OneDrivePermission> ShareItem(OneDriveItem item, OneDriveLinkType linkType)
        {
            return await ShareItemInternal(string.Concat("drive/items/", item.Id, "/createLink"), linkType);
        }

        /// <summary>
        /// Shares a OneDrive item by creating an anonymous link to the item
        /// </summary>
        /// <param name="itemPath">The path to the OneDrive item to share</param>
        /// <param name="linkType">Type of sharing to request</param>
        /// <param name="scope">Scope defining who has access to the shared item</param>
        /// <returns>OneDrivePermission entity representing the share or NULL if the operation fails</returns>
        public async Task<OneDrivePermission> ShareItem(string itemPath, OneDriveLinkType linkType, OneDriveSharingScope scope)
        {
            return await ShareItemInternal(string.Concat("drive/root:/", itemPath, ":/createLink"), linkType, scope);
        }

        /// <summary>
        /// Shares a OneDrive item by creating an anonymous link to the item
        /// </summary>
        /// <param name="item">The OneDrive item to share</param>
        /// <param name="linkType">Type of sharing to request</param>
        /// <param name="scope">Scope defining who has access to the shared item</param>
        /// <returns>OneDrivePermission entity representing the share or NULL if the operation fails</returns>
        public async Task<OneDrivePermission> ShareItem(OneDriveItem item, OneDriveLinkType linkType, OneDriveSharingScope scope)
        {
            return await ShareItemInternal(string.Concat("drive/items/", item.Id, "/createLink"), linkType, scope);
        }

        /// <summary>
        /// Initiates a resumable upload session to OneDrive. It doesn't perform the actual upload yet.
        /// </summary>
        /// <param name="fileName">Filename to store the uploaded content under</param>
        /// <param name="oneDriveItem">OneDriveItem container in which the file should be uploaded</param>
        /// <returns>OneDriveUploadSession instance containing the details where to upload the content to</returns>
        protected override async Task<OneDriveUploadSession> CreateResumableUploadSession(string fileName, OneDriveItem oneDriveItem)
        {
            // Construct the complete URL to call
            var completeUrl = string.Concat(OneDriveApiBaseUrl, "drive/items/", oneDriveItem.Id, ":/", fileName, ":/createUploadSession");

            // Construct the OneDriveUploadSessionItemContainer entity with the upload details
            // Add the conflictbehavior header to always overwrite the file if it already exists on OneDrive
            var uploadItemContainer = new OneDriveUploadSessionItemContainer
            {
                Item = new OneDriveUploadSessionItem
                {
                    FilenameConflictBehavior = NameConflictBehavior.Replace
                }
            };

            // Call the OneDrive webservice
            var result = await SendMessageReturnOneDriveItem<OneDriveUploadSession>(uploadItemContainer, HttpMethod.Post, completeUrl, HttpStatusCode.OK);
            return result;
        }

        /// <summary>
        /// Constructs the complete Url to be called based on the part of the url provided that contains the command
        /// </summary>
        /// <param name="commandUrl">Part of the URL to call that contains the command to execute for the API that is being called</param>
        /// <returns>Full URL to call the API</returns>
        protected override string ConstructCompleteUrl(string commandUrl)
        {
            if (commandUrl.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                return commandUrl;
            }
            return string.Concat(commandUrl.StartsWith("drives/", StringComparison.InvariantCultureIgnoreCase) ? OneDriveApiBaseUrl.EndsWith("me/") ? OneDriveApiBaseUrl.Remove(OneDriveApiBaseUrl.LastIndexOf("me/", StringComparison.OrdinalIgnoreCase)) : OneDriveApiBaseUrl : OneDriveApiBaseUrl, commandUrl);
        }

        #endregion
    }
}
