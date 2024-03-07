﻿using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Linq;

namespace DivinityModManager.Models
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DivinityModVersion : ReactiveObject
	{
		[Reactive] public int Major { get; set; }
		[Reactive] public int Minor { get; set; }
		[Reactive] public int Revision { get; set; }
		[Reactive] public int Build { get; set; }

		[ObservableAsProperty] public string Version { get; }
		[ObservableAsProperty] public int VersionInt { get; }

		public int ToInt()
		{
			return (Major << 28) + (Minor << 24) + (Revision << 16) + (Build);
		}

		public override string ToString()
		{
			return Version;
		}

		public void ParseInt(int vInt)
		{
			Major = (vInt >> 28);
			Minor = (vInt >> 24) & 0x0F;
			Revision = (vInt >> 16) & 0xFF;
			Build = (vInt & 0xFFFF);
		}

		public static string StringFromIndividual(int major, int minor, int revision, int build)
		{
			return $"{major}.{minor}.{revision}.{build}";
		}

		public static int IntFromIndividual(int major, int minor, int revision, int build)
		{
			return (major << 28) + (minor << 24) + (revision << 16) + (build);
		}

		public static DivinityModVersion FromInt(int vInt)
		{
			return new DivinityModVersion(vInt);
		}

		public DivinityModVersion()
		{
			var whenAnyNum = this.WhenAnyValue(x => x.Major, x => x.Minor, x => x.Revision, x => x.Build);
			whenAnyNum.Select(v => StringFromIndividual(v.Item1, v.Item2, v.Item3, v.Item4)).ToPropertyEx(this, x => x.Version);
			whenAnyNum.Select(v => IntFromIndividual(v.Item1, v.Item2, v.Item3, v.Item4)).ToPropertyEx(this, x => x.VersionInt);
		}

		public DivinityModVersion(int vInt) : this()
		{
			ParseInt(vInt);
		}

		public static bool operator >(DivinityModVersion a, DivinityModVersion b)
		{
			return a.VersionInt > b.VersionInt;
		}

		public static bool operator <(DivinityModVersion a, DivinityModVersion b)
		{
			return a.VersionInt < b.VersionInt;
		}

		public static bool operator >=(DivinityModVersion a, DivinityModVersion b)
		{
			return a.VersionInt >= b.VersionInt;
		}

		public static bool operator <=(DivinityModVersion a, DivinityModVersion b)
		{
			return a.VersionInt <= b.VersionInt;
		}

		public DivinityModVersion(int headerMajor, int headerMinor, int headerRevision, int headerBuild) : this()
		{
			Major = headerMajor;
			Minor = headerMinor;
			Revision = headerRevision;
			Build = headerBuild;
		}
	}
}