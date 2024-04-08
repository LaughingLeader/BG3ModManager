using Avalonia.Input;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;

namespace ModManager.Models;

public interface IHotkey
{
	Key Key { get; set; }
	ModifierKeys Modifiers { get; set; }
	bool Enabled { get; set; }
	string? DisplayName { get; set; }
}

[DataContract]
public class Hotkey : ReactiveObject, IHotkey
{
	private readonly Key _defaultKey = Key.None;
	private readonly ModifierKeys _defaultModifiers = ModifierKeys.None;

	public Key DefaultKey => _defaultKey;
	public ModifierKeys DefaultModifiers => _defaultModifiers;

	public string Id { get; }


	[DataMember, JsonConverter(typeof(StringEnumConverter))]
	[Reactive] public Key Key { get; set; }

	[DataMember, JsonConverter(typeof(StringEnumConverter))]
	[Reactive] public ModifierKeys Modifiers { get; set; }

	public IReactiveCommand Command { get; }

	[Reactive] public string? DisplayName { get; set; }

	[Reactive] public bool Enabled { get; set; }
	[Reactive] public bool CanEdit { get; set; }
	[Reactive] public bool IsSelected { get; set; }

	[ObservableAsProperty] public string? KeyBindingText { get; }
	[ObservableAsProperty] public string? ModifiedText { get; }
	[ObservableAsProperty] public string? ToolTip { get; }
	[ObservableAsProperty] public bool IsDefault { get; }

	public void ResetToDefault()
	{
		Key = _defaultKey;
		Modifiers = _defaultModifiers;
	}

	public void Clear()
	{
		Key = Key.None;
		Modifiers = ModifierKeys.None;
	}

	private static string KeyToDisplayString(Key key, ModifierKeys modifiers)
	{
		var str = new StringBuilder();

		if (modifiers.HasFlag(ModifierKeys.Control))
			str.Append("Ctrl + ");
		if (modifiers.HasFlag(ModifierKeys.Shift))
			str.Append("Shift + ");
		if (modifiers.HasFlag(ModifierKeys.Alt))
			str.Append("Alt + ");
		if (key.HasFlag(Key.LWin) || key.HasFlag(Key.RWin))
			str.Append("Win + ");

		str.Append(key.GetKeyName());

		return str.ToString();
	}

	public Hotkey(string id, string displayName, Key key, IReactiveCommand command, ModifierKeys modifiers = ModifierKeys.None)
	{
		_defaultKey = key;
		_defaultModifiers = modifiers;
		Id = id;
		Key = key;
		DisplayName = displayName;
		Modifiers = modifiers;
		Command = command;

		Enabled = true;
		CanEdit = true;

		var keysChanged = this.WhenAnyValue(x => x.Key, x => x.Modifiers);

		keysChanged.Select(x => x.Item1 == _defaultKey && x.Item2 == _defaultModifiers).ToUIProperty(this, x => x.IsDefault, true);
		keysChanged.Select(x => KeyToDisplayString(x.Item1, x.Item2)).ToUIProperty(this, x => x.KeyBindingText);

		var isDefaultObservable = this.WhenAnyValue(x => x.IsDefault);

		isDefaultObservable.Select(b => !b ? "*" : "").ToUIProperty(this, x => x.ModifiedText, "");

		this.WhenAnyValue(x => x.DisplayName, x => x.IsDefault)
			.Select(x => x.Item2 ? $"{x.Item1} (Modified)" : x.Item1)
			.ToUIProperty(this, x => x.ToolTip);
	}

	public override string ToString() => KeyToDisplayString(Key, Modifiers);
}
