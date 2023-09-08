// ReSharper disable InconsistentNaming

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NexusAPI.Responses
{
    /// <summary>A downloadable mod file.</summary>
    public class ModFile
    {
        /// <summary>The unique file ID.</summary>
        [JsonProperty("file_id")]
        public int FileID { get; set; }

        /// <summary>The download name.</summary>
        [JsonProperty("Name")]
        public string Name { get; set; }

        /// <summary>The file version number.</summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>The mod version at the time the file was uploaded.</summary>
        [JsonProperty("mod_version")]
        public string ModVersion { get; set; }

        /// <summary>The download filename.</summary>
        [JsonProperty("file_name")]
        public string FileName { get; set; }

        /// <summary>The file category.</summary>
        [JsonProperty("category_id")]
        public FileCategory Category { get; set; }

        /// <summary>Whether the file is marked as the primary download.</summary>
        [JsonProperty("is_primary")]
        public bool IsPrimary { get; set; }

        /// <summary>The file size in kilobytes.</summary>
        [JsonProperty("Size")]
        public int Size { get; set; }

        /// <summary>When the file was uploaded.</summary>
        [JsonProperty("uploaded_timestamp")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset UploadedTimestamp { get; set; }

        /// <summary>The URL to the external virus scan results.</summary>
        [JsonProperty("external_virus_scan_url")]
        public Uri ExternalVirusScanUri { get; set; }

        /// <summary>The HTML change logs, if any.</summary>
        [JsonProperty("changelog_html")]
        public string ChangeLogHtml { get; set; }
    }
}