// ReSharper disable InconsistentNaming

using System.Runtime.Serialization;

namespace NexusAPI.Responses
{
    /// <summary>A mod file category.</summary>
    public enum FileCategory
    {
        /// <summary>A main file.</summary>
        [EnumMember(Value = "MAIN")]
        Main = 1,

        /// <summary>An update file.</summary>
        [EnumMember(Value = "UPDATE")]
        Update = 2,

        /// <summary>An optional file.</summary>
        [EnumMember(Value = "OPTIONAL")]
        Optional = 3,

        [EnumMember(Value = "OLD_VERSION")]
        Old = 4,

        /// <summary>A miscellaneous file.</summary>
        [EnumMember(Value = "MISCELLANEOUS")]
        Miscellaneous = 5,

        /// <summary>A deleted file not shown in the UI.</summary>
        [EnumMember(Value = "DELETED")]
        Deleted = 6
    }
}