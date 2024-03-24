using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows.Input;

namespace ModManager.ViewModels;

public class DownloadActivityBarViewModel : BaseWindowViewModel
{
	[Reactive] private double ProgressValue { get; set; }
	[Reactive] private string ProgressText { get; set; }
	[Reactive] public bool IsActive { get; private set; }
	[Reactive] public Action CancelAction { get; set; }

	[ObservableAsProperty] public double CurrentValue { get; }
	[ObservableAsProperty] public string CurrentText { get; }
	[ObservableAsProperty] public bool IsAnimating { get; }

	public ICommand CancelCommand { get; private set; }

	public void UpdateProgress(double value, string text = "")
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			ProgressValue = value;
			if (!String.IsNullOrEmpty(text))
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
		else if (!String.IsNullOrEmpty(ProgressText))
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
		this.WhenAnyValue(x => x.ProgressValue).Select(Clamp).ToUIProperty(this, x => x.CurrentValue, 0d);
		this.WhenAnyValue(x => x.ProgressText).ToUIProperty(this, x => x.CurrentText, "");
		this.WhenAnyValue(x => x.CurrentValue, x => x < 100).ToUIProperty(this, x => x.IsAnimating, true);

		this.WhenAnyValue(x => x.CurrentText, x => x.CurrentValue).Select(x => !String.IsNullOrEmpty(x.Item1) || x.Item2 > 0).BindTo(this, x => x.IsActive);

		//this.WhenAnyValue(x => x.IsActive).Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.IsVisible, Visibility.Collapsed);

		CancelCommand = ReactiveCommand.Create(Cancel);
	}
}
