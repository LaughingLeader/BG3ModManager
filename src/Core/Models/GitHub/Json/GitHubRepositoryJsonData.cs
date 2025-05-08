using DynamicData.Binding;

using System.Runtime.Serialization;

namespace ModManager.Models.GitHub.Json;


/// <summary>
/// This is intended to be parsed from a file such as Repository.json, similar to the file structure for mods made for UnityModManager.
/// This file can be set up to configure downloads for specific versions of multiple mods, so it may be ideal for more complicated setups,
/// or authors that want to specifically control where releases should be downloaded from.
/// </summary>
[DataContract]
public class GitHubRepositoryJsonData : ReactiveObject
{
	[Reactive, DataMember] public ObservableCollectionExtended<GitHubReleaseJsonEntry> Releases { get; set; }

	public GitHubReleaseJsonEntry? GetLatest(string? uuid = "")
	{
		if (uuid.IsValid())
		{
			return Releases.Where(x => x.UUID == uuid).OrderBy(x => x.Version).FirstOrDefault();
		}
		return Releases.OrderBy(x => x.Version).FirstOrDefault();
	}

	public GitHubRepositoryJsonData()
	{
		Releases = [];
	}
}