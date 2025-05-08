using ModManager.Extensions;

using System.Globalization;
using System.Runtime.Serialization;

namespace ModManager.Models.Extender;

[DataContract]
public class ScriptExtenderUpdateData
{
	[DataMember] public int ManifestMinorVersion { get; set; }
	[DataMember] public int ManifestVersion { get; set; }
	[DataMember] public string? NoMatchingVersionNotice { get; set; }
	[DataMember] public List<ScriptExtenderUpdateResource>? Resources { get; set; }
}

[DataContract]
public class ScriptExtenderUpdateResource
{
	[DataMember] public string? Name { get; set; }
	[DataMember] public List<ScriptExtenderUpdateVersion>? Versions { get; set; }
}

[DataContract]
public class ScriptExtenderUpdateVersion : ReactiveObject
{
	[DataMember, Reactive] public long BuildDate { get; set; }
	[DataMember, Reactive] public string? Digest { get; set; }
	[DataMember, Reactive] public string? MinGameVersion { get; set; }
	[DataMember, Reactive] public string? Notice { get; set; }
	[DataMember, Reactive] public string? URL { get; set; }
	[DataMember, Reactive] public string? Version { get; set; }
	[DataMember, Reactive] public string? Signature { get; set; }

	[ObservableAsProperty] public string? DisplayName { get; }
	[ObservableAsProperty] public string? BuildDateDisplayString { get; }
	[ObservableAsProperty] public bool IsEmpty { get; }

	private static string TimestampToReadableString(long timestamp)
	{
		var date = DateTime.FromFileTime(timestamp);
		return date.ToString(DivinityApp.DateTimeExtenderBuildFormat, CultureInfo.InstalledUICulture);
	}

	private static string ToDisplayName(ValueTuple<string?, string?, string?> data)
	{
		if (!data.Item1.IsValid()) return "Latest";
		var result = data.Item1;
		if (data.Item2.IsValid())
		{
			result += $" ({data.Item2})";
		}
		if (data.Item3.IsValid())
		{
			result += $" - {data.Item3}";
		}
		return result;
	}

	public ScriptExtenderUpdateVersion()
	{
		this.WhenAnyValue(x => x.Version).Select(x => !x.IsValid())
			.ToUIProperty(this, x => x.IsEmpty);

		this.WhenAnyValue(x => x.BuildDate).Select(TimestampToReadableString)
			.ToUIProperty(this, x => x.BuildDateDisplayString);

		this.WhenAnyValue(x => x.Version, x => x.MinGameVersion, x => x.BuildDateDisplayString).Select(ToDisplayName)
			.ToUIProperty(this, x => x.DisplayName);
	}
}
