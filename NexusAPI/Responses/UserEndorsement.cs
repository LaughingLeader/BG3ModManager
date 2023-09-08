// ReSharper disable InconsistentNaming

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NexusAPI.Responses
{
    /// <summary>A mod endorsement by the user.</summary>
    public sealed class UserEndorsement
    {
        /// <summary>The unique mod ID.</summary>
        [JsonProperty("mod_id")]
        public int ModID { get; set; }

        /// <summary>The mod's game key.</summary>
        [JsonProperty("domain_name")]
        public string DomainName { get; set; }

        /// <summary>When the user endorsed the mod.</summary>
        [JsonProperty("Date")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        public DateTimeOffset Date { get; set; }

        /// <summary>The current mod version when the user endorsed the mod.</summary>
        [JsonProperty("Version")]
        public string Version { get; set; }

        /// <summary>The mod endorsement status.</summary>
        [JsonProperty("Status")]
        public EndorsementStatus Status { get; set; }
    }
}