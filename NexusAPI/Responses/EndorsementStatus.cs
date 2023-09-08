// ReSharper disable InconsistentNaming
namespace NexusAPI.Responses
{
    /// <summary>A status representing whether the user has endorsed a mod.</summary>
    public enum EndorsementStatus
    {
        /// <summary>Not applicable.</summary>
        None,

        /// <summary>The user is eligible to endorse the mod, but hasn't done so yet.</summary>
        Undecided,

        /// <summary>The user endorsed and subsequently un-endorsed the mod.</summary>
        Abstained,

        /// <summary>The user has endorsed the mod.</summary>
        Endorsed
    }
}