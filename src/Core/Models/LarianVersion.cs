using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Linq;
using System.Runtime.Serialization;

namespace ModManager.Models;

static class VersionHelpers
{
	public static ValueTuple<ulong, ulong, ulong, ulong> FromStringToInt64(string value)
	{
		ulong major = 0;
		ulong minor = 0;
		ulong revision = 0;
		ulong build = 0;

		var values = value.Split('.');
		if (values.Length > 0)
		{
			ulong.TryParse(values[0], out major);
			if (values.Length > 1) ulong.TryParse(values[1], out minor);
			if (values.Length > 2) ulong.TryParse(values[2], out revision);
			if (values.Length > 3) ulong.TryParse(values[3], out build);
		}
		return (major, minor, revision, build);
	}

	public static ValueTuple<int, int, int, int> FromStringToInt(string value)
	{
		int major = 0;
		int minor = 0;
		int revision = 0;
		int build = 0;

		var values = value.Split('.');
		if (values.Length > 0)
		{
			int.TryParse(values[0], out major);
			if (values.Length > 1) int.TryParse(values[1], out minor);
			if (values.Length > 2) int.TryParse(values[2], out revision);
			if (values.Length > 3) int.TryParse(values[3], out build);
		}
		return (major, minor, revision, build);
	}

	public static ValueTuple<ulong, ulong, ulong, ulong> FromInt64(ulong value)
	{
		return (
			value >> 55,
			(value >> 47) & 0xFF,
			(value >> 31) & 0xFFFF,
			value & 0x7FFFFFFFUL
		);
	}

	public static ValueTuple<int, int, int, int> FromInt(int value)
	{
		return (
			value >> 28,
			(value >> 24) & 0x0F,
			(value >> 16) & 0xFF,
			value & 0xFFFF
		);
	}

	public static ulong ToInt64(ValueTuple<ulong, ulong, ulong, ulong> value)
	{
		return (value.Item1 << 55) + (value.Item2 << 47) + (value.Item3 << 31) + value.Item4;
	}

	public static int ToInt(ValueTuple<int, int, int, int> value)
	{
		return (value.Item1 << 28) + (value.Item2 << 24) + (value.Item3 << 16) + value.Item4;
	}
}

[DataContract]
public class LarianVersion : ReactiveObject
{
	// 1.0.0.0 in the int version
	public static readonly ulong IntVersion1 = 268435456;
	// 1.0.0.0 in the int64 version
	public static readonly ulong Int64Version1 = 36028797018963968;

#if !DOS2
#else
#endif
#if !DOS2
	[Reactive] public ulong Major { get; set; }
	[Reactive] public ulong Minor { get; set; }
	[Reactive] public ulong Revision { get; set; }
	[Reactive] public ulong Build { get; set; }

	private ulong versionInt = 0;

	[DataMember]
	public ulong VersionInt
	{
		get { return versionInt; }
		set => ParseInt(value);
	}
#else
	[Reactive] public int Major { get; set; }
	[Reactive] public int Minor { get; set; }
	[Reactive] public int Revision { get; set; }
	[Reactive] public int Build { get; set; }

	private int versionInt = 0;

	[DataMember]
	public int VersionInt
	{
		get { return versionInt; }
		set => ParseInt(value);
	}
#endif

	[DataMember, Reactive] public string Version { get; set; }

	private void UpdateVersion() => Version = $"{Major}.{Minor}.{Revision}.{Build}";

	public ulong ToInt()
	{
#if !DOS2
		return VersionHelpers.ToInt64((Major, Minor, Revision, Build));
#else
		return VersionHelpers.ToInt((Major, Minor, Revision, Build));
#endif
	}

	public override string ToString() => String.Format("{0}.{1}.{2}.{3}", Major, Minor, Revision, Build);

#if !DOS2
	public void ParseInt(ulong nextVersionInt)
	{
		nextVersionInt = Math.Max(ulong.MinValue, Math.Min(nextVersionInt, ulong.MaxValue));
		if (versionInt != nextVersionInt)
		{
			versionInt = nextVersionInt;
			if (versionInt != 0)
			{
				var parsed = VersionHelpers.FromInt64(versionInt);
				Major = parsed.Item1;
				Minor = parsed.Item2;
				Revision = parsed.Item3;
				Build = parsed.Item4;
			}
			else
			{
				Major = Minor = Revision = Build = 0;
			}
			this.RaisePropertyChanged(nameof(VersionInt));
		}
	}
#else
	public void ParseInt(int nextVersionInt)
	{
		nextVersionInt = Math.Max(int.MinValue, Math.Min(nextVersionInt, int.MaxValue));
		if (versionInt != nextVersionInt)
		{
			versionInt = nextVersionInt;
			if (versionInt != 0)
			{
				var parsed = VersionHelpers.FromInt(versionInt);
				Major = parsed.Item1;
				Minor = parsed.Item2;
				Revision = parsed.Item3;
				Build = parsed.Item4;
			}
			else
			{
				Major = Minor = Revision = Build = 0;
			}
			this.RaisePropertyChanged(nameof(VersionInt));
		}
	}
#endif

	public void ParseString(string nextVersion)
	{
#if !DOS2
		var result = VersionHelpers.FromStringToInt64(nextVersion);
#else
		var result = VersionHelpers.FromStringToInt(nextVersion);
#endif
		Major = result.Item1;
		Minor = result.Item2;
		Revision = result.Item3;
		Build = result.Item4;
		versionInt = ToInt();
		var values = nextVersion.Split('.');
		this.RaisePropertyChanged(nameof(VersionInt));
	}

#if !DOS2
	public static LarianVersion FromInt(ulong vInt)
	{
		if (vInt == 1 || vInt == IntVersion1)
		{
			// 1.0.0.0
			vInt = Int64Version1;
		}
		return new LarianVersion(vInt);
	}
#else
	public static LarianVersion FromInt(int vInt) => new LarianVersion(vInt);
#endif

	public LarianVersion()
	{
		this.WhenAnyValue(x => x.VersionInt).Subscribe((x) =>
		{
			UpdateVersion();
		});
	}
	public LarianVersion(string versionStr) : this()
	{
		ParseString(versionStr);
	}

#if !DOS2
	public LarianVersion(ulong vInt) : this()
	{
		ParseInt(vInt);
	}

	public LarianVersion(ulong headerMajor, ulong headerMinor, ulong headerRevision, ulong headerBuild) : this()
	{
		Major = headerMajor;
		Minor = headerMinor;
		Revision = headerRevision;
		Build = headerBuild;
		versionInt = ToInt();
		UpdateVersion();
	}
#else
	public LarianVersion(int vInt) : this()
	{
		ParseInt(vInt);
	}

	public LarianVersion(int headerMajor, int headerMinor, int headerRevision, int headerBuild) : this()
	{
		Major = headerMajor;
		Minor = headerMinor;
		Revision = headerRevision;
		Build = headerBuild;
		versionInt = ToInt();
		UpdateVersion();
	}
#endif

	public static readonly LarianVersion Empty = new(0);

	#region Version-to-Version Operators

	public static bool operator >(LarianVersion a, LarianVersion b)
	{
		return a.VersionInt > b.VersionInt;
	}

	public static bool operator <(LarianVersion a, LarianVersion b)
	{
		return a.VersionInt < b.VersionInt;
	}

	public static bool operator >=(LarianVersion a, LarianVersion b)
	{
		return a.VersionInt >= b.VersionInt;
	}

	public static bool operator <=(LarianVersion a, LarianVersion b)
	{
		return a.VersionInt <= b.VersionInt;
	}

	public static bool operator >(LarianVersion a, string b)
	{
		return a.VersionInt > new LarianVersion(b).VersionInt;
	}

	public static bool operator <(LarianVersion a, string b)
	{
		return a.VersionInt < new LarianVersion(b).VersionInt;
	}

	public static bool operator >=(LarianVersion a, string b)
	{
		return a.VersionInt >= new LarianVersion(b).VersionInt;
	}

	public static bool operator <=(LarianVersion a, string b)
	{
		return a.VersionInt <= new LarianVersion(b).VersionInt;
	}

	#endregion

	#region Version-to-C# Version Operators

	public static bool operator >(LarianVersion a, System.Version b)
	{
		return a.VersionInt > new LarianVersion(b.ToString()).VersionInt;
	}

	public static bool operator <(LarianVersion a, System.Version b)
	{
		return a.VersionInt < new LarianVersion(b.ToString()).VersionInt;
	}

	public static bool operator >=(LarianVersion a, System.Version b)
	{
		return a.VersionInt >= new LarianVersion(b.ToString()).VersionInt;
	}

	public static bool operator <=(LarianVersion a, System.Version b)
	{
		return a.VersionInt <= new LarianVersion(b.ToString()).VersionInt;
	}

	#endregion
}
