using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Runtime.Serialization;

namespace DivinityModManager.Models.GitHub.Json;

[DataContract]
public class GitHubReleaseJsonEntry : ReactiveObject
{
	[Reactive, DataMember] public string UUID { get; set; }
	[Reactive, DataMember] public string Version { get; set; }
	[Reactive, DataMember] public string DownloadUrl { get; set; }
}