using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDriveLocationFacet
    {
        [JsonProperty("altitude")]
        public double Altitude { get; set; }
        
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
    }
}
