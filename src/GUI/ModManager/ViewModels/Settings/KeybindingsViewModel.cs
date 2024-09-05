using DynamicData;

using ModManager.Models;

using System.Collections.ObjectModel;

namespace ModManager.ViewModels.Settings;
public class KeybindingsViewModel : ReactiveObject
{
	private readonly ReadOnlyObservableCollection<Hotkey> _hotkeys;
	public ReadOnlyObservableCollection<Hotkey> Hotkeys => _hotkeys;

	[Reactive] public Hotkey? TargetHotkey { get; private set; }

	[ObservableAsProperty] public bool IsBindingKey { get; }

	public ReactiveCommand<Hotkey, Unit> ClearKeyCommand { get; }
	public ReactiveCommand<Hotkey, Unit> ResetKeyCommand { get; }
	public ReactiveCommand<Hotkey, Unit> StartBindingKeyCommand { get; }
	public ReactiveCommand<KeyEventArgs, Unit> OnBindingKeyPressedCommand { get; }
	public RxCommandUnit CancelBindingKeyCommand { get; }

	private void Cancel()
	{
		TargetHotkey = null;
	}

	private void OnBindingKeyPressed(KeyEventArgs e)
	{
		if(e.Key == Key.Escape)
		{
			Cancel();
			return;
		}
		if(TargetHotkey != null)
		{
			TargetHotkey.Key = e.Key;
			TargetHotkey.Modifiers = e.KeyModifiers;
		}
	}

	public KeybindingsViewModel()
	{
		this.WhenAnyValue(x => x.TargetHotkey).Select(x => x != null).ToUIPropertyImmediate(this, x => x.IsBindingKey);

		ClearKeyCommand = ReactiveCommand.Create<Hotkey>(hotkey => hotkey.Clear());
		ResetKeyCommand = ReactiveCommand.Create<Hotkey>(hotkey => hotkey.ResetToDefault());
		StartBindingKeyCommand = ReactiveCommand.Create<Hotkey>(hotkey => TargetHotkey = hotkey);
		OnBindingKeyPressedCommand = ReactiveCommand.Create<KeyEventArgs>(OnBindingKeyPressed);
		CancelBindingKeyCommand = ReactiveCommand.Create(Cancel);

		var keybindingsService = AppServices.Get<AppKeysService>()!;
		keybindingsService.Hotkeys.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out _hotkeys).Subscribe();
	}
}

public class KeybindingsDesignViewModel : KeybindingsViewModel
{
	public KeybindingsDesignViewModel() : base()
	{
		var keybindingsService = AppServices.Get<AppKeysService>()!;

		var testCommand = ReactiveCommand.Create(() => { });

		keybindingsService.Hotkeys.AddOrUpdate(new Hotkey("Test", "Test", Key.A, testCommand, KeyModifiers.Control));
		keybindingsService.Hotkeys.AddOrUpdate(new Hotkey("Test2", "Close", Key.F4, testCommand, KeyModifiers.Control | KeyModifiers.Alt));
	}
}