using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NexusAPI.Responses
{
    /// <summary>Simplified data about a mod endorsement.</summary>
    public sealed class Endorsement
    {
        /// <summary>The mod endorsement status.</summary>
        [JsonProperty("endorse_status")]
        public EndorsementStatus EndorseStatus { get; set; }

        /// <summary>When the user endorsed the mod (if they did).</summary>
        [JsonProperty("timestamp")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>The current mod version when the user endorsed the mod.</summary>
        [JsonProperty("Version")]
        public string Version { get; set; }
    }
}