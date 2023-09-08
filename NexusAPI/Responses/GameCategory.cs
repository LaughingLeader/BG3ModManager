// ReSharper disable InconsistentNaming

using Newtonsoft.Json;

namespace NexusAPI.Responses
{
    /// <summary>A mod category available for a game's mods.</summary>
    public sealed class GameCategory
    {
        /// <summary>The category ID.</summary>
        [JsonProperty("category_id")]
        public int ID { get; set; }

        /// <summary>The category name.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>The parent category, if any.</summary>
        [JsonProperty("parent_category")]
        public int? ParentCategory { get; set; }
    }
}