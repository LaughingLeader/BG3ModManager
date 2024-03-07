﻿using DivinityModManager.Json;

using DynamicData;

using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Linq;
using System.Runtime.Serialization;

namespace DivinityModManager.Models
{
	[DataContract]
	public class DivinityModScriptExtenderConfig : ReactiveObject
	{
		[DataMember][Reactive] public int RequiredVersion { get; set; }
		[DataMember][Reactive] public string ModTable { get; set; }

		[JsonConverter(typeof(JsonArrayToSourceListConverter<string>))]
		[DataMember] public SourceList<string> FeatureFlags { get; set; }

		[ObservableAsProperty] public int TotalFeatureFlags { get; }
		[ObservableAsProperty] public bool HasAnySettings { get; }

		public bool Lua => FeatureFlags.Items.Contains("Lua");

		public DivinityModScriptExtenderConfig()
		{
			RequiredVersion = -1;
			FeatureFlags = new();
			FeatureFlags.CountChanged.ToPropertyEx(this, x => x.TotalFeatureFlags);
			this.WhenAnyValue(x => x.RequiredVersion, x => x.TotalFeatureFlags, x => x.ModTable)
			.Select(x => x.Item1 > -1 || x.Item2 > 0 || !String.IsNullOrEmpty(x.Item3)).ToPropertyEx(this, x => x.HasAnySettings);
		}
	}
}
