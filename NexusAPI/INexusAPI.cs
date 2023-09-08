// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Threading.Tasks;
using NexusAPI.Responses;
using RestEase;

namespace NexusAPI
{
    [Header("Cache-Control", "no-cache")]
    public interface INexusAPI
    {
        [Header("apikey")]
        string ApiKey { get; set; }
        
        [Header("User-Agent")]
        string UserAgent { get; set; }

        #region Mods
        [Get("v1/games/{domainName}/mods/updated.json")]
        Task<ModUpdate[]> Updated([Path]string domainName, [Query] string period);

        [Get("v1/games/{domainName}/mods/{modId}/changelogs.json")]
        Task<IDictionary<string, string[]>> ChangeLogs([Path] string domainName, [Path] int modId);
        
        [Get("v1/games/{domainName}/mods/latest_added.json")]
        Task<Mod[]> LatestAdded([Path]string domainName);

        [Get("v1/games/{domainName}/mods/latest_updated.json")]
        Task<Mod[]> LatestUpdated([Path]string domainName);

        [Get("v1/games/{domainName}/mods/trending.json")]
        Task<Mod[]> Trending([Path]string domainName);

        [Get("v1/games/{domainName}/mods/{modId}.json")]
        Task<Mod> GetMod([Path]string domainName, [Path]int modId);

        [Get("/v1/games/{domainName}/mods/md5_search/{md5_hash}.json")]
        Task<ModHashResult[]> Md5Search([Path]string domainName, [Path]string md5_hash);

        [Post("v1/games/{domainName}/mods/{modId}/endorse.json")]
        Task Endorse([Path] string domainName, [Path] int modId, [Body] string version);

        [Post("v1/games/{domainName}/mods/{modId}/abstain.json")]
        Task Abstain([Path] string domainName, [Path] int modId, [Body] string version);
        #endregion
        
        #region Mod Files
        [Get("v1/games/{domainName}/mods/{modId}/files/{fileId}.json")]
        Task<ModFile> GetModFle([Path]string domainName, [Path]int modId, [Path]int fileId);

        [Get("v1/games/{domainName}/mods/{modId}/files.json")]
        Task<ModFileList> GetModFiles([Path]string domainName, [Path]int modId);
        
        [Get("v1/games/{domainName}/mods/{modId}/files/{fileId}/download_link.json")]
        Task<ModFileDownloadLink[]> GetDownloadLinks([Path]string domainName, [Path]int modId, [Path]int fileId);
        #endregion
        
        #region Games
        [Get("v1/games.json")]
        Task<Game[]> Games([Query(QuerySerializationMethod.Serialized)]bool include_unapproved = false);

        [Get("v1/games/{domainName}.json")]
        Task<Game> GetGame([Path]string domainName);
        #endregion
        
        #region User
        [Get("v1/users/validate.json")]
        Task<Validate> ValidateAsync();

        [Get("v1/user/tracked_mods.json")]
        Task<UserTrackedMod[]> TrackedMods();

        [Post("v1/user/tracked_mods.json")]
        Task TrackMod([Body] int mod_id);

        [Delete("v1/user/tracked_mods.json")]
        Task UntrackMod([Body] int mod_id);

        [Get("v1/user/endorsements.json")]
        Task<UserEndorsement[]> Endorsements();
        #endregion

        #region Colour Schemes
        [Get("/v1/colourschemes.json")]
        Task<ColorScheme[]> ColourSchemes();
        #endregion
    }
}