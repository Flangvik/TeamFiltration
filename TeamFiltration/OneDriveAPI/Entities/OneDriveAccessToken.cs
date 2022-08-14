using Newtonsoft.Json;
using System;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Contains information regarding an access token to OneDrive
    /// </summary>
    public class OneDriveAccessToken
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int AccessTokenExpirationDuration { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("scope")]
        public string Scopes { get; set; }

        [JsonProperty("authentication_token")]
        public string AuthenticationToken { get; set; }

      

        
    }
}
