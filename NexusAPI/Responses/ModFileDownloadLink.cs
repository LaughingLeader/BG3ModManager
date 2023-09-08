// ReSharper disable InconsistentNaming

using System;
using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>A mod file download link.</summary>
    public sealed class ModFileDownloadLink
    {
        /// <summary>The full name of the CDN serving the file.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>The short name of the CDN serving the file.</summary>
        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        /// <summary>The download URL.</summary>
        public Uri Uri { get; set; }
    }
}