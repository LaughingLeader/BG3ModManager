using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Linq;

namespace ModManager.Models.Steam;

public class SteamWorkshopModData : ReactiveObject
{
	[Reactive] public long ModId { get; set; }
	[Reactive] public DateTime CreatedDate { get; set; }
	[Reactive] public DateTime UpdatedDate { get; set; }

	public List<string> Tags { get; set; }

	/// <summary>
	/// True if ModId is set.
	/// </summary>
	[Reactive] public bool IsEnabled { get; private set; }

	public void Update(SteamWorkshopModData otherData)
	{
		ModId = otherData.ModId;
		CreatedDate = otherData.CreatedDate;
		UpdatedDate = otherData.UpdatedDate;
		Tags = otherData.Tags;

		this.WhenAnyValue(x => x.ModId)
		.Select(x => x >= DivinityApp.WORKSHOP_MOD_ID_START)
		.ObserveOn(RxApp.MainThreadScheduler)
		.BindTo(this, x => x.IsEnabled);
	}
}
