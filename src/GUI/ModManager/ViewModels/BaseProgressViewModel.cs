namespace ModManager.ViewModels;

public abstract class BaseProgressViewModel : ReactiveObject, IClosableViewModel
{
	#region IClosableViewModel
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	[Reactive] public bool CanRun { get; set; }
	[Reactive] public bool CanClose { get; set; }
	[Reactive] public bool IsProgressActive { get; set; }
	[Reactive] public string? ProgressTitle { get; set; }
	[Reactive] public string? ProgressWorkText { get; set; }
	[Reactive] public double ProgressValue { get; set; }

	/// <summary>
	/// True when the RunCommand is executing.
	/// </summary>
	[ObservableAsProperty] public bool IsRunning { get; }

	public ReactiveCommand<Unit, bool> RunCommand { get; }
	public RxCommandUnit CancelRunCommand { get; }

	internal async Task UpdateProgress(string title = "", string workText = "", double value = -1)
	{
		await Observable.Start(() =>
		{
			if (title.IsValid())
			{
				ProgressTitle = title;
			}
			if (workText.IsValid())
			{
				ProgressWorkText = workText;
			}
			if (value > -1)
			{
				ProgressValue = value;
			}
		}, RxApp.MainThreadScheduler);
	}

	public virtual async Task<bool> Run(CancellationToken token)
	{
		return true;
	}

	public virtual void Close()
	{
		CanClose = true;
		IsVisible = false;
		IsProgressActive = false;
	}

	public BaseProgressViewModel()
	{
		CanClose = true;

		CancelRunCommand = ReactiveCommand.Create(() => { }, this.WhenAnyValue(x => x.IsRunning));

		var canRun = this.WhenAnyValue(x => x.CanRun);
		RunCommand = ReactiveCommand.CreateFromObservable(() => Observable.StartAsync(cts => Run(cts)).TakeUntil(CancelRunCommand), canRun);
		RunCommand.IsExecuting.ToUIProperty(this, x => x.IsRunning);

		var canClose = this.WhenAnyValue(x => x.CanClose, x => x.IsRunning, (b1, b2) => b1 && !b2);
		CloseCommand = ReactiveCommand.Create(Close, canClose);
	}
}
