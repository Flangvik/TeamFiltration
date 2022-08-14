using System;
using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    public class OneDrivePhotoFacet
    {
        [JsonProperty("takenDateTime")]
        public DateTimeOffset TakenDateTime { get; set; }

        [JsonProperty("cameraMake")]
        public string CameraMake { get; set; }
        
        [JsonProperty("cameraModel")]
        public string CameraModel { get; set; }

        [JsonProperty("fNumber")]
        public double FStop { get; set; }

        [JsonProperty("exposureDenominator")]
        public double ExposureDenominator { get; set; }

        [JsonProperty("exposureNumerator")]
        public double ExposureNumerator { get; set; }

        [JsonProperty("focalLength")]
        public double FocalLength { get; set; }

        [JsonProperty("iso")]
        public int ISO { get; set; }
    }
}
