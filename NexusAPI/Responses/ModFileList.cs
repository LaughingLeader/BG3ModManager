// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>A list of files for a mod.</summary>
    public sealed class ModFileList
    {
        /// <summary>The matched file details.</summary>
        [JsonProperty("Files")]
        public ModFile[] Files { get; set; }

        /// <summary>The update relationships between files (i.e. a record of the uploader marking each file as a newer version of a previous one, if they did).</summary>
        [JsonProperty("file_updates")]
        public ModFileUpdate[] FileUpdates { get; set; }
    }
}