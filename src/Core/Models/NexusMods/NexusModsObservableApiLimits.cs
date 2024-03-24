using NexusModsNET;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ModManager.Models.NexusMods;

public class NexusModsObservableApiLimits : ReactiveObject, INexusApiLimits
{
	[Reactive] public int HourlyLimit { get; set; }
	[Reactive] public int HourlyRemaining { get; set; }
	[Reactive] public DateTime HourlyReset { get; set; }
	[Reactive] public int DailyLimit { get; set; }
	[Reactive] public int DailyRemaining { get; set; }
	[Reactive] public DateTime DailyReset { get; set; }

	public void Reset()
	{
		HourlyLimit = 100;
		HourlyRemaining = 100;
		DailyLimit = 2500;
		DailyRemaining = 2500;
		HourlyReset = DateTime.MaxValue;
		DailyReset = DateTime.MaxValue;
	}

	public NexusModsObservableApiLimits()
	{
		Reset();
	}
}
