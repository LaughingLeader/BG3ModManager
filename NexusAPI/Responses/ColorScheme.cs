// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>A Nexus color scheme.</summary>
    public sealed class ColorScheme
    {
        /// <summary>The unique color scheme ID.</summary>
        [JsonProperty("id")]
        public int ID { get; set; }

        /// <summary>The color scheme name.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>The primary color in the scheme, like '#92ab20'.</summary>
        [JsonProperty("primary_colour")]
        public string PrimaryColor { get; set; }

        /// <summary>The secondary color in the scheme, like '#a4c21e'.</summary>
        [JsonProperty("secondary_colour")]
        public string SecondaryColor { get; set; }

        /// <summary>The darker color in the scheme, like '#545e24'.</summary>
        [JsonProperty("darker_colour")]
        public string DarkerColor { get; set; }
    }
}