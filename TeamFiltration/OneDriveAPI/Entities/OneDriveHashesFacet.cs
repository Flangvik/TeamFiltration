using Newtonsoft.Json;

namespace KoenZomers.OneDrive.Api.Entities
{
    /// <summary>
    /// Hashes of the file
    /// </summary>
    public class OneDriveHashesFacet
    {
        /// <summary>
        /// SHA1 hash of the file
        /// </summary>
        [JsonProperty("sha1Hash")]
        public string Sha1 { get; set; }

        /// <summary>
        /// CRC32 hash of the file
        /// </summary>
        [JsonProperty("crc32Hash")]
        public string Crc32 { get; set; }
    }
}
