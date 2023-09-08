using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>A downloadable mod file, with its MD5 hash.</summary>
    public sealed class ModFileWithHash : ModFile
    {
        /// <summary>The MD5 file hash.</summary>
        [JsonProperty("Md5")]
        public string Md5 { get; set; }
    }
}