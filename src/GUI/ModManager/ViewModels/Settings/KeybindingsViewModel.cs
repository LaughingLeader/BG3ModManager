using ModManager.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.ViewModels.Settings;
public class KeybindingsViewModel : ReactiveObject, IRoutableViewModel
{
	public string UrlPathSegment => "keybindings";
	public IScreen HostScreen { get; }


	public ReactiveCommand<Hotkey, Unit> ClearKeyCommand { get; }
	public ReactiveCommand<Hotkey, Unit> ResetKeyCommand { get; }

	public KeybindingsViewModel(IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;

		ClearKeyCommand = ReactiveCommand.Create<Hotkey>(hotkey => hotkey.Clear());
		ResetKeyCommand = ReactiveCommand.Create<Hotkey>(hotkey => hotkey.ResetToDefault());
	}
}
