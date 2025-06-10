using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.ViewModels.Main;
public class FooterViewModel : ReactiveObject
{
	[Reactive] public bool IsNexusModsSectionExpanded { get; set; }
	[ObservableAsProperty] public string? NexusModsLimitsText { get; }
	[ObservableAsProperty] public Uri? NexusModsProfileBitmapUri { get; }
	[ObservableAsProperty] public bool HasNexusModsProfileAvatar { get; }

	public FooterViewModel(INexusModsService nexusModsService)
	{
		IsNexusModsSectionExpanded = true;

		nexusModsService.WhenLimitsChange.Throttle(TimeSpan.FromMilliseconds(50)).Select(x => x?.ToString() ?? string.Empty).ToUIProperty(this, x => x.NexusModsLimitsText, nexusModsService.ApiLimits.ToString());
		var whenNexusModsAvatar = nexusModsService.WhenAnyValue(x => x.ProfileAvatarUrl);
		whenNexusModsAvatar.Select(Validators.IsValid).ToUIProperty(this, x => x.HasNexusModsProfileAvatar);
		whenNexusModsAvatar.ToUIProperty(this, x => x.NexusModsProfileBitmapUri);
	}
}
