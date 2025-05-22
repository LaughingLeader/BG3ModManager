using Avalonia.Threading;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.ViewModels.Main;

public interface IProgressBarViewModel : IRoutableViewModel
{
	bool CanCancel { get; }
	string? Title { get; set; }
	string? WorkText { get; set; }
	double Value { get; set; }

	CancellationToken Token { get; }
	IRoutableViewModel? NextView { get; set; }
	bool IsVisible { get; }

	RxCommandUnit CancelCommand { get; }

	void Start(Func<CancellationToken, Task> asyncTask, bool canCancel = false, IRoutableViewModel? switchToViewOnFinish = null);
	Task CancelAsync();

	void IncreaseValue(double amount, string? workText = null);
}

public class ProgressBarViewModel : ReactiveObject, IProgressBarViewModel
{
	public string UrlPathSegment => "mainprogress";
	public IScreen HostScreen { get; }

	[Reactive] private CancellationTokenSource? TokenSource { get; set; }

	[Reactive] public bool IsVisible { get; set; }
	[Reactive] public string? Title { get; set; }
	[Reactive] public string? WorkText { get; set; }
	[Reactive] public double Value { get; set; }
	[Reactive] public bool CanCancel { get; private set; }

	[Reactive] public CancellationToken Token { get; private set; }
	[Reactive] public IRoutableViewModel? NextView { get; set; }

	private ReactiveCommand<Func<CancellationToken, Task>, Unit> RunCommand { get; }
	public RxCommandUnit CancelCommand { get; }

	public void IncreaseValue(double amount, string? workText = null)
	{
		Value += amount;
		if(workText != null) WorkText = workText;
	}

	private async Task<Unit> RunTaskAsync(Func<CancellationToken, Task> task)
	{
		await task(Token);
		Value = 100d;
		return Unit.Default;
	}

	public async void Start(Func<CancellationToken, Task> asyncTask, bool canCancel = false, IRoutableViewModel? switchToViewOnFinish = null)
	{
		await Dispatcher.UIThread.InvokeAsync(async () =>
		{
			TokenSource?.Dispose();
			TokenSource = new CancellationTokenSource();
			CanCancel = canCancel;
			Value = 0d;

			NextView = switchToViewOnFinish ?? await HostScreen.Router.CurrentViewModel;
			await HostScreen.Router.Navigate.Execute(this);
		}, DispatcherPriority.Background);

		RxApp.TaskpoolScheduler.ScheduleAsync(async (_, _) =>
		{
			await RunCommand.Execute(asyncTask);
			await FinishAsync(NextView);
		});
	}

	public async Task CancelAsync()
	{
		if (TokenSource != null) await TokenSource.CancelAsync();

		RxApp.MainThreadScheduler.Schedule(Finish);
	}

	private void Finish()
	{
		TokenSource?.Dispose();
		Value = 100d;
		RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(500), () =>
		{
			if(!IsVisible)
			{
				Title = WorkText = string.Empty;
				Value = 0d;
			}
		});
	}

	private async Task FinishAsync(IRoutableViewModel? lastView)
	{
		await Observable.StartAsync(async () =>
		{
			Finish();
			if (lastView != null)
			{
				await Task.Delay(500);
				await Dispatcher.UIThread.InvokeAsync(async () =>
				{
					await HostScreen.Router.Navigate.Execute(lastView);
				}, DispatcherPriority.Background);
			}
		}, RxApp.MainThreadScheduler);
	}

	public ProgressBarViewModel(IScreen? host = null)
	{
		Title = WorkText = string.Empty;
		Value = 0d;

		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;

		this.WhenAnyValue(x => x.TokenSource).WhereNotNull().Select(x => x.Token).BindTo(this, x => x.Token);

		var canCancel = this.WhenAnyValue(x => x.CanCancel);
		var hasToken = this.WhenAnyValue(x => x.Token).Select(x => !x.IsCancellationRequested);
		CancelCommand = ReactiveCommand.CreateFromTask(CancelAsync, canCancel.AllTrue(hasToken));

		//Cancellable execution logic since RunTaskAsync takes a CancellationToken as the second param
		RunCommand = ReactiveCommand.CreateFromTask<Func<CancellationToken, Task>, Unit>(RunTaskAsync);
		RunCommand.ThrownExceptions.Subscribe(ex =>
		{
			DivinityApp.Log($"Error running progress action:\n{ex}");
			TokenSource?.Cancel();
			Finish();
		});
	}
}

public class DesignProgressBarViewModel : ProgressBarViewModel
{
	public DesignProgressBarViewModel() : base(null)
	{
		Title = "Loading...";
		WorkText = "Loading paks in Mods folder...";
		Value = 50;
	}
}