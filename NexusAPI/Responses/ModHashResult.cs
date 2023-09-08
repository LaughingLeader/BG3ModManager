using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>The result of a mod MD5 hash search.</summary>
    public sealed class ModHashResult
    {
        /// <summary>The matched mod.</summary>
        [JsonProperty("Mod")]
        public Mod Mod { get; set; }

        /// <summary>The matched file details.</summary>
        [JsonProperty("file_details")]
        public ModFileWithHash FileDetails { get; set; }
    }
}