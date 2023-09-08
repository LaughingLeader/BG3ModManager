// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>User login metadata.</summary>
    public sealed class Validate
    {
        /// <summary>The unique user ID.</summary>
        [JsonProperty("user_id")]
        public int UserID { get; set; }

        /// <summary>The user's API authentication key.</summary>
        [JsonProperty("Key")]
        public string Key { get; set; }

        /// <summary>The username.</summary>
        [JsonProperty("Name")]
        public string Name { get; set; }

        /// <summary>The user's email address.</summary>
        [JsonProperty("Email")]
        public string Email { get; set; }

        /// <summary>The URL of the user's avatar.</summary>
        [JsonProperty("profile_url")]
        public string ProfileUrl { get; set; }

        /// <summary>Whether the user has a premium Nexus account.</summary>
        [JsonProperty("is_premium")]
        public bool IsPremium { get; set; }

        /// <summary>Whether the user has a supporter Nexus account.</summary>
        [JsonProperty("is_supporter")]
        public bool IsSupporter { get; set; }

    }
}