using System.Diagnostics.CodeAnalysis;

namespace ModManager.Models.App;
public class AppUpdateResult(bool isAvailable, Version? version, DateTimeOffset date, string? downloadUrl)
{
	public bool IsAvailable => isAvailable;

	[MemberNotNullWhen(true, nameof(IsAvailable))]
	public Version? Version => version;

	[MemberNotNullWhen(true, nameof(IsAvailable))]
	public string? DownloadUrl => downloadUrl;

	public DateTimeOffset Date => date;
}