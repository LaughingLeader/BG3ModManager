// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>Simplified data about a user.</summary>
    public sealed class User
    {
        /// <summary>The unique user ID.</summary>
        [JsonProperty("member_id")]
        public int MemberID { get; set; }

        /// <summary>The member group ID.</summary>
        [JsonProperty("member_group_id")]
        public int MemberGroupID { get; set; }

        /// <summary>The username.</summary>
        [JsonProperty("Name")]
        public string Name { get; set; }
    }
}