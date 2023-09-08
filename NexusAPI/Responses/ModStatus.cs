// ReSharper disable InconsistentNaming

using System.Runtime.Serialization;

namespace NexusAPI.Responses
{
    /// <summary>A mod publication status.</summary>
    public enum ModStatus
    {
        /// <summary>Not applicable.</summary>
        [EnumMember(Value = "none")]
        None,

        /// <summary>The mod is not yet published.</summary>
        [EnumMember(Value = "not_published")]
        NotPublished,

        /// <summary>The mod is published and visible.</summary>
        [EnumMember(Value = "published")]
        Published,

        /// <summary>The mod should be published once the game goes live.</summary>
        [EnumMember(Value = "publish_with_game")]
        PublishWithGame,

        /// <summary>The mod has been hidden by the author.</summary>
        [EnumMember(Value = "hidden")]
        Hidden,

        /// <summary>The mod is hidden while it undergoes moderator review.</summary>
        [EnumMember(Value = "under_moderation")]
        UnderModeration,

        /// <summary>The mod has been recoverably deleted.</summary>
        [EnumMember(Value = "waste_binned")]
        Wastebinned,

        /// <summary>The mod has been permanently removed.</summary>
        [EnumMember(Value = "removed")]
        Removed
    }
}