using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveErrorDetail : OneDriveItemBase
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("innererror")]
        public OneDriveErrorDetail InnerError { get; set; }
    }
}
