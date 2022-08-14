using System;
using System.Threading.Tasks;
using KoenZomers.OneDrive.Api.Entities;
using KoenZomers.OneDrive.Api.Helpers;

namespace KoenZomers.OneDrive.Api
{
    /// <summary>
    /// API for the Consumer OneDrive
    /// Create your own Client ID / Client Secret at https://account.live.com/developers/applications/index
    /// </summary>
    public class OneDriveConsumerApi : OneDriveApi
    {
        #region Constants

        /// <summary>
        /// The url to provide as the redirect URL after successful authentication
        /// </summary>
        public override string AuthenticationRedirectUrl { get; set; } = "https://login.live.com/oauth20_desktop.srf";

        /// <summary>
        /// String formatted Uri that needs to be called to authenticate
        /// </summary>
        protected override string AuthenticateUri => "https://login.live.com/oauth20_authorize.srf?client_id={0}&scope={1}&response_type=code&redirect_uri={2}";

        /// <summary>
        /// String formatted Uri that can be called to sign out from the OneDrive API
        /// </summary>
        public override string SignoutUri => "https://login.live.com/oauth20_logout.srf";

        /// <summary>
        /// The url where an access token can be obtained
        /// </summary>
        protected override string AccessTokenUri => "https://login.live.com/oauth20_token.srf";

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiates a new instance of the Consumer OneDrive API
        /// </summary>
        /// <param name="clientId">OneDrive Client ID to use to connect</param>
        /// <param name="clientSecret">OneDrive Client Secret to use to connect</param>
        public OneDriveConsumerApi(string clientId, string clientSecret) : base(clientId, clientSecret)
        {
            OneDriveApiBaseUrl = "https://api.onedrive.com/v1.0/";
        }

        #endregion

        #region Public Methods - Authentication

        /// <summary>
        /// Instantiates a new instance of the OneDriveApi
        /// </summary>
        /// <param name="clientId">OneDrive Client ID to use to connect</param>
        /// <param name="clientSecret">OneDrive Client Secret to use to connect</param>
        /// <param name="refreshToken">Refreshtoken to use to get an access token</param>
        [Obsolete("Use AuthenticateUsingRefreshToken instead")]
        public static async Task<OneDriveApi> GetOneDriveApiFromRefreshToken(string clientId, string clientSecret, string refreshToken)
        {
            var oneDriveApi = new OneDriveConsumerApi(clientId, clientSecret);
            oneDriveApi.AccessToken = await oneDriveApi.GetAccessTokenFromRefreshToken(refreshToken);

            return oneDriveApi;
        }

        /// <summary>
        /// Gets an access token from the provided refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Access token for OneDrive</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected override async Task<OneDriveAccessToken> GetAccessTokenFromRefreshToken(string refreshToken)
        {
            var queryBuilder = new QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("redirect_uri", AuthenticationRedirectUrl);
            queryBuilder.Add("client_secret", ClientSecret);
            queryBuilder.Add("refresh_token", refreshToken);
            queryBuilder.Add("grant_type", "refresh_token");
            queryBuilder.Add("scope", "openid");
            return await PostToTokenEndPoint(queryBuilder);
        }

        /// <summary>
        /// Returns the Uri that needs to be called to authenticate to the OneDrive API
        /// </summary>
        /// <param name="scopes">String with one or more scopes separated with a space to which you want to request access. See https://msdn.microsoft.com/en-us/library/office/dn631845.aspx for the scopes that you can use.</param>
        /// <returns>Uri that needs to be called in a browser to authenticate to the OneDrive API</returns>
        public Uri GetAuthenticationUri(string scopes)
        {
            var uri = string.Format(AuthenticateUri, ClientId, scopes, AuthenticationRedirectUrl);
            return new Uri(uri);
        }

        /// <summary>
        /// Returns the Uri that needs to be called to authenticate to the OneDrive API using the default scope of "wl.signin wl.offline_access onedrive.readwrite"
        /// </summary>
        /// <returns>Uri that needs to be called in a browser to authenticate to the OneDrive API</returns>
        public override Uri GetAuthenticationUri()
        {
            return GetAuthenticationUri("wl.signin wl.offline_access onedrive.readwrite");
        }

        /// <summary>
        /// Gets an access token from the provided authorization token
        /// </summary>
        /// <param name="authorizationToken">Authorization token</param>
        /// <returns>Access token for OneDrive</returns>
        /// <exception cref="Exceptions.TokenRetrievalFailedException">Thrown when unable to retrieve a valid access token</exception>
        protected override async Task<OneDriveAccessToken> GetAccessTokenFromAuthorizationToken(string authorizationToken)
        {
            var queryBuilder = new QueryStringBuilder();
            queryBuilder.Add("client_id", ClientId);
            queryBuilder.Add("redirect_uri", AuthenticationRedirectUrl);
            queryBuilder.Add("client_secret", ClientSecret);
            queryBuilder.Add("code", authorizationToken);
            queryBuilder.Add("grant_type", "authorization_code");

            return await PostToTokenEndPoint(queryBuilder);
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
            char[] restrictedCharacters = { '\\', '/', ':', '*', '?', '<', '>', '|' };
            return filename.IndexOfAny(restrictedCharacters) == -1;
        }

        #endregion
    }
}
