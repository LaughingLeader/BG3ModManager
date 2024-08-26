using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

using Humanizer;
using Humanizer.Localisation;

namespace ModManager.Controls;

[TemplatePart("PART_Days", typeof(NumericUpDown))]
[TemplatePart("PART_Hours", typeof(NumericUpDown))]
[TemplatePart("PART_Minutes", typeof(NumericUpDown))]
public class TimeSpanUpDown : TemplatedControl
{
	public static readonly StyledProperty<TimeSpan> ValueProperty = AvaloniaProperty.Register<TimeSpanUpDown, TimeSpan>(nameof(Value));

	public static readonly StyledProperty<int> DaysProperty = AvaloniaProperty.Register<TimeSpanUpDown, int>(nameof(Days));
	public static readonly StyledProperty<int> HoursProperty = AvaloniaProperty.Register<TimeSpanUpDown, int>(nameof(Hours));
	public static readonly StyledProperty<int> MinutesProperty = AvaloniaProperty.Register<TimeSpanUpDown, int>(nameof(Minutes));
	public static readonly StyledProperty<int> SecondsProperty = AvaloniaProperty.Register<TimeSpanUpDown, int>(nameof(Seconds));
	public static readonly StyledProperty<int> MillisecondsProperty = AvaloniaProperty.Register<TimeSpanUpDown, int>(nameof(Milliseconds));
	public static readonly StyledProperty<int> MicrosecondsProperty = AvaloniaProperty.Register<TimeSpanUpDown, int>(nameof(Microseconds));

	public static readonly StyledProperty<string> DaysFormatStringProperty = AvaloniaProperty.Register<TimeSpanUpDown, string>(nameof(DaysFormatString), "0 days");
	public static readonly StyledProperty<string> HoursFormatStringProperty = AvaloniaProperty.Register<TimeSpanUpDown, string>(nameof(HoursFormatString), "0 hours");
	public static readonly StyledProperty<string> MinutesFormatStringProperty = AvaloniaProperty.Register<TimeSpanUpDown, string>(nameof(MinutesFormatString), "0 minutes");

	public TimeSpan Value
	{
		get => GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}
	public int Days
	{
		get => GetValue(DaysProperty);
		set => SetValue(DaysProperty, value);
	}

	public int Hours
	{
		get => GetValue(HoursProperty);
		set => SetValue(HoursProperty, value);
	}

	public int Minutes
	{
		get => GetValue(MinutesProperty);
		set => SetValue(MinutesProperty, value);
	}

	public int Seconds
	{
		get => GetValue(SecondsProperty);
		set => SetValue(SecondsProperty, value);
	}

	public int Milliseconds
	{
		get => GetValue(MillisecondsProperty);
		set => SetValue(MillisecondsProperty, value);
	}

	public int Microseconds
	{
		get => GetValue(MicrosecondsProperty);
		set => SetValue(MicrosecondsProperty, value);
	}

	public string DaysFormatString
	{
		get => GetValue(DaysFormatStringProperty);
		set => SetValue(DaysFormatStringProperty, value);
	}

	public string HoursFormatString
	{
		get => GetValue(HoursFormatStringProperty);
		set => SetValue(HoursFormatStringProperty, value);
	}

	public string MinutesFormatString
	{
		get => GetValue(MinutesFormatStringProperty);
		set => SetValue(MinutesFormatStringProperty, value);
	}

	static TimeSpanUpDown()
	{
		ValueProperty.Changed.Subscribe(OnValueChanged);
		MinutesProperty.Changed.Subscribe(OnUpDownValueChanged);
		HoursProperty.Changed.Subscribe(OnUpDownValueChanged);
		DaysProperty.Changed.Subscribe(OnUpDownValueChanged);
		SecondsProperty.Changed.Subscribe(OnUpDownValueChanged);
		MillisecondsProperty.Changed.Subscribe(OnUpDownValueChanged);
		MicrosecondsProperty.Changed.Subscribe(OnUpDownValueChanged);
	}

	private static void OnValueChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Sender is TimeSpanUpDown upDown)
		{
			var oldValue = (TimeSpan?)e.OldValue;
			var newValue = (TimeSpan?)e.NewValue;
			upDown.OnValueChanged(oldValue ?? TimeSpan.Zero, newValue ?? TimeSpan.Zero);
		}
	}

	private static void OnUpDownValueChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Sender is TimeSpanUpDown upDown && e.OldValue is int oldValue && e.NewValue is int newValue)
		{
			upDown.OnOnUpDownValueChanged(oldValue, newValue);
		}
	}

	private bool _updatingValues = false;

	protected virtual void UpdateFormatStrings()
	{
		var daysFormat = "0 day";
		if(Days != 1) daysFormat += "s";
		DaysFormatString = daysFormat;

		var hoursFormat = "0 hour";
		if (Hours != 1) hoursFormat += "s";
		HoursFormatString = hoursFormat;

		var minutesFormat = "0 minute";
		if (Minutes != 1) minutesFormat += "s";
		MinutesFormatString = minutesFormat;
	}

	protected virtual void OnValueChanged(TimeSpan oldValue, TimeSpan newValue)
	{
		if (_updatingValues) return;

		_updatingValues = true;

		try
		{
			Days = newValue.Days;
			Hours = newValue.Hours;
			Minutes = newValue.Minutes;
			Seconds = newValue.Seconds;
			Milliseconds = newValue.Milliseconds;
			Microseconds = newValue.Microseconds;

			UpdateFormatStrings();
		}
		finally
		{
			_updatingValues = false;
		}
	}

	protected virtual void OnOnUpDownValueChanged(int oldValue, int newValue)
	{
		if (_updatingValues) return;

		_updatingValues = true;

		try
		{
			Value = new TimeSpan(Days, Hours, Minutes, Seconds, Milliseconds, Microseconds);

			UpdateFormatStrings();
		}
		finally
		{
			_updatingValues = false;
		}
	}

	private NumericUpDown? _daysUpDown;
	private NumericUpDown? _hoursUpDown;
	private NumericUpDown? _minutesUpDown;

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		_daysUpDown = e.NameScope.Find<NumericUpDown>("PART_Days");
		_hoursUpDown = e.NameScope.Find<NumericUpDown>("PART_Hours");
		_minutesUpDown = e.NameScope.Find<NumericUpDown>("PART_Minutes");

		if(_hoursUpDown != null && _daysUpDown != null)
		{
			_hoursUpDown.GetObservable(NumericUpDown.ValueProperty).Subscribe(x =>
			{
				if(x >= 24)
				{
					_hoursUpDown.Value = 0;
					_daysUpDown.Value += 1;
				}
			});
		}

		if(_hoursUpDown != null && _minutesUpDown != null)
		{
			_minutesUpDown.GetObservable(NumericUpDown.ValueProperty).Subscribe(x =>
			{
				if(x >= 60)
				{
					_minutesUpDown.Value = 0;
					_hoursUpDown.Value += 1;
				}
			});
		}
	}
}
