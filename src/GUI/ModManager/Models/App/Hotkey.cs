using Avalonia.Input;

using DynamicData;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.Runtime.Serialization;
using System.Text;
using System.Windows.Input;

namespace ModManager.Models.App;

public interface IHotkey
{
	Key Key { get; set; }
	ModifierKeys Modifiers { get; set; }
	ICommand Command { get; }
	bool Enabled { get; set; }
	string DisplayName { get; set; }
}

[DataContract]
public class Hotkey : ReactiveObject, IHotkey
{
	public string ID { get; set; }

	[Reactive] public string DisplayName { get; set; }

	[ObservableAsProperty] public string ToolTip { get; }

	[Reactive] public string DisplayBindingText { get; private set; }

	[DataMember]
	[JsonConverter(typeof(StringEnumConverter))]
	[Reactive] public Key Key { get; set; }

	[DataMember]
	[JsonConverter(typeof(StringEnumConverter))]
	[Reactive] public ModifierKeys Modifiers { get; set; }

	public RxCommandUnit Command { get; set; }
	ICommand IHotkey.Command => this.Command;

	[Reactive] public ICommand ResetCommand { get; private set; }
	[Reactive] public ICommand ClearCommand { get; private set; }

	[Reactive] public bool Enabled { get; set; }
	[Reactive] public bool CanEdit { get; set; }

	[ObservableAsProperty] public bool IsDefault { get; }
	[Reactive] public bool IsSelected { get; set; }

	[ObservableAsProperty] public string ModifiedText { get; }

	private readonly Key _defaultKey = Key.None;
	private readonly ModifierKeys _defaultModifiers = ModifierKeys.None;

	public Key DefaultKey => _defaultKey;
	public ModifierKeys DefaultModifiers => _defaultModifiers;

	[Reactive] public bool CanExecuteCommand { get; private set; }

	private readonly List<IObservable<bool>> _canExecuteConditions = [];
	private CompositeDisposable _disposables = [];
	private IObservable<bool> _canExecute;

	private readonly List<Action> _actions;

	// Big thanks to https://sachabarbs.wordpress.com/2013/10/18/reactive-command-with-dynamic-predicates/

	private void SetupSubscription()
	{
		_disposables?.Dispose();
		_disposables =
		[
			_canExecute.Subscribe(b => CanExecuteCommand = b)
		];
	}

	public void AddCanExecuteCondition(IObservable<bool> canExecute)
	{
		_canExecuteConditions.Add(canExecute);
		_canExecute = _canExecute.CombineLatest(_canExecuteConditions.Last(), (b1, b2) => b1 && b2).DistinctUntilChanged();
		SetupSubscription();
	}

	public void AddAction(Action action, IObservable<bool> actionCanExecute = null)
	{
		_actions.Add(action);

		if (actionCanExecute != null)
		{
			AddCanExecuteCondition(actionCanExecute);
		}
	}

	private static void RunActionAsync(IScheduler scheduler, Func<Task> action)
	{
		scheduler.ScheduleAsync(async (sch, token) =>
		{
			await action.Invoke();
		});
	}

	public void AddAsyncAction(Func<Task> action, IObservable<bool>? actionCanExecute = null, IScheduler? scheduler = null)
	{
		scheduler ??= RxApp.MainThreadScheduler;
		_actions.Add(() => RunActionAsync(scheduler, action));

		if (actionCanExecute != null)
		{
			AddCanExecuteCondition(actionCanExecute);
		}
	}

	public void Invoke()
	{
		_actions.ForEach(a => a.Invoke());
	}

	public void ResetToDefault()
	{
		Key = _defaultKey;
		Modifiers = _defaultModifiers;
		UpdateDisplayBindingText();
	}

	public void Clear()
	{
		Key = Key.None;
		Modifiers = ModifierKeys.None;
		UpdateDisplayBindingText();
	}

	public void UpdateDisplayBindingText()
	{
		DisplayBindingText = ToString();
	}

	public Hotkey(Key key = Key.None, ModifierKeys modifiers = ModifierKeys.None)
	{
		DisplayName = "";
		Key = key;
		Modifiers = modifiers;
		_defaultKey = key;
		_defaultModifiers = modifiers;

		Enabled = true;
		CanEdit = true;

		_actions = [];

		DisplayBindingText = ToString();

		_canExecute = this.WhenAnyValue(x => x.Enabled);
		SetupSubscription();

		Command = ReactiveCommand.Create(Invoke, this.WhenAnyValue(x => x.CanExecuteCommand).ObserveOn(RxApp.MainThreadScheduler), RxApp.MainThreadScheduler);

		this.WhenAnyValue(x => x.Key, x => x.Modifiers).Select(x => x.Item1 == _defaultKey && x.Item2 == _defaultModifiers).ToUIProperty(this, x => x.IsDefault, true);

		var isDefaultObservable = this.WhenAnyValue(x => x.IsDefault);

		isDefaultObservable.Select(b => !b ? "*" : "").ToUIProperty(this, x => x.ModifiedText, "");

		this.WhenAnyValue(x => x.DisplayName, x => x.IsDefault).Select(x => x.Item2 ? $"{x.Item1} (Modified)" : x.Item1).ToUIProperty(this, x => x.ToolTip);

		var canReset = isDefaultObservable.Select(b => !b);
		var canClear = this.WhenAnyValue(x => x.Key, x => x.Modifiers, (k, m) => k != Key.None);

		ResetCommand = ReactiveCommand.Create(ResetToDefault, canReset);
		ClearCommand = ReactiveCommand.Create(Clear, canClear);
	}

	public override string ToString()
	{
		var str = new StringBuilder();

		if (Modifiers.HasFlag(ModifierKeys.Control))
			str.Append("Ctrl + ");
		if (Modifiers.HasFlag(ModifierKeys.Shift))
			str.Append("Shift + ");
		if (Modifiers.HasFlag(ModifierKeys.Alt))
			str.Append("Alt + ");
		if (Key.HasFlag(Key.LWin) || Key.HasFlag(Key.RWin))
			str.Append("Win + ");

		str.Append(Key.GetKeyName());

		return str.ToString();
	}
}
