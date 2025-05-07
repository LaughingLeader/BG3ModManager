namespace ModManager.ViewModels;

public class DownloadActivityBarViewModel : ReactiveObject, IClosableViewModel
{
	#region IClosableViewModel
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	[Reactive] private double ProgressValue { get; set; }
	[Reactive] private string? ProgressText { get; set; }
	[Reactive] public bool IsActive { get; private set; }
	[Reactive] public Action? CancelAction { get; set; }

	[ObservableAsProperty] public double CurrentValue { get; }
	[ObservableAsProperty] public string? CurrentText { get; }
	[ObservableAsProperty] public bool IsAnimating { get; }

	public void UpdateProgress(double value, string text = "")
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			ProgressValue = value;
			if (!string.IsNullOrEmpty(text))
			{
				ProgressText = text;
			}
		});
	}

	public void Cancel()
	{
		if (CancelAction != null)
		{
			CancelAction.Invoke();
		}
		else if (!string.IsNullOrEmpty(ProgressText))
		{
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				ProgressValue = 0d;
				ProgressText = "";
				IsActive = false;
			});
		}
	}

	private double Clamp(double value)
	{
		return Math.Min(100, Math.Max(0, value));
	}

	public DownloadActivityBarViewModel()
	{
		CloseCommand = this.CreateCloseCommand(invokeAction: Cancel);

		this.WhenAnyValue(x => x.ProgressValue).Select(Clamp).ToUIProperty(this, x => x.CurrentValue, 0d);
		this.WhenAnyValue(x => x.ProgressText).ToUIProperty(this, x => x.CurrentText, "");
		this.WhenAnyValue(x => x.CurrentValue, x => x < 100).ToUIProperty(this, x => x.IsAnimating, true);

		this.WhenAnyValue(x => x.CurrentText, x => x.CurrentValue).Select(x => x.Item1.IsValid() || x.Item2 > 0).BindTo(this, x => x.IsActive);
	}
}
