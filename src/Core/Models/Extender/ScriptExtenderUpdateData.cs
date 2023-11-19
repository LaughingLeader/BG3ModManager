﻿using DivinityModManager.Util;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Extender
{
	[DataContract]
	public class ScriptExtenderUpdateData
	{
		[DataMember] public int ManifestMinorVersion { get; set; }
		[DataMember] public int ManifestVersion { get; set; }
		[DataMember] public string NoMatchingVersionNotice { get; set; }
		[DataMember] public List<ScriptExtenderUpdateResource> Resources { get; set; }
	}

	[DataContract]
	public class ScriptExtenderUpdateResource
	{
		[DataMember] public string Name { get; set; }
		[DataMember] public List<ScriptExtenderUpdateVersion> Versions { get; set; }
	}

	[DataContract]
	public class ScriptExtenderUpdateVersion : ReactiveObject
	{
		[DataMember][Reactive] public long BuildDate { get; set; }
		[DataMember][Reactive] public string Digest { get; set; }
		[DataMember][Reactive] public string MinGameVersion { get; set; }
		[DataMember][Reactive] public string Notice { get; set; }
		[DataMember][Reactive] public string URL { get; set; }
		[DataMember][Reactive] public string Version { get; set; }
		[DataMember][Reactive] public string Signature { get; set; }

		[ObservableAsProperty] public string DisplayName { get; }
		[ObservableAsProperty] public string BuildDateDisplayString { get; }
		[ObservableAsProperty] public bool IsEmpty { get; }

		private string TimestampToReadableString(long timestamp)
		{
			var date = DateTime.FromFileTime(timestamp);
			return date.ToString(DivinityApp.DateTimeExtenderBuildFormat, CultureInfo.InstalledUICulture);
		}

		private string ToDisplayName(ValueTuple<string, string, string> data)
		{
			if (String.IsNullOrEmpty(data.Item1)) return "Latest";
			var result = data.Item1;
			if(!String.IsNullOrEmpty(data.Item2))
			{
				result += $" ({data.Item2})";
			}
			if(!String.IsNullOrEmpty(data.Item3))
			{
				result += $" - {data.Item3}";
			}
			return result;
		}

		public ScriptExtenderUpdateVersion()
		{
			this.WhenAnyValue(x => x.Version).Select(x => String.IsNullOrEmpty(x)).ToUIProperty(this, x => x.IsEmpty);
			this.WhenAnyValue(x => x.BuildDate).Select(TimestampToReadableString).ToUIProperty(this, x => x.BuildDateDisplayString);
			this.WhenAnyValue(x => x.Version, x => x.MinGameVersion, x => x.BuildDateDisplayString).Select(ToDisplayName).ToUIProperty(this, x => x.DisplayName);
		}
	}
}
