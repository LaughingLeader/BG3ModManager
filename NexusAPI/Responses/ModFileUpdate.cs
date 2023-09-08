// ReSharper disable InconsistentNaming

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NexusAPI.Responses
{
    /// <summary>A record indicating an update relationship between two files (e.g. 1.0.1 supersedes 1.0.0).</summary>
    public sealed class ModFileUpdate
    {
        /// <summary>The older file ID.</summary>
        [JsonProperty("old_file_id")]
        public int OldFileID { get; set; }

        /// <summary>The older filename.</summary>
        [JsonProperty("old_file_name")]
        public string OldFileName { get; set; }

        /// <summary>The newer file ID.</summary>
        [JsonProperty("new_file_id")]
        public int NewFileID { get; set; }

        /// <summary>The newer filename.</summary>
        [JsonProperty("new_file_name")]
        public string NewFileName { get; set; }

        /// <summary>When the newer file was uploaded.</summary>
        [JsonProperty("uploaded_timestamp")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset UploadTimestamp { get; set; }

    }
}