using DivinityModManager.Models.Updates;
using DivinityModManager.Windows;

using DynamicData;
using DynamicData.Binding;

using Ookii.Dialogs.Wpf;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace DivinityModManager.ViewModels;

public class CopyModUpdatesTask
{
	public List<DivinityModUpdateData> Updates { get; set; }
	public string DocumentsFolder { get; set; }
	public string ModPakFolder { get; set; }
	public int TotalProcessed { get; set; }
}

public class ModUpdatesViewData : ReactiveObject
{
	[Reactive] public bool Unlocked { get; set; }
	[Reactive] public bool JustUpdated { get; set; }

	public class UpdateTaskResult
	{
		public string ModId { get; set; }
		public bool Success { get; set; }
	}

	private readonly SourceList<DivinityModUpdateData> Mods = new();

	private readonly ReadOnlyObservableCollection<DivinityModUpdateData> _updates;
	public ReadOnlyObservableCollection<DivinityModUpdateData> Updates => _updates;

	[ObservableAsProperty] public bool AnySelected { get; }
	[ObservableAsProperty] public bool AllSelected { get; }
	[ObservableAsProperty] public int TotalUpdates { get; }

	public ICommand UpdateModsCommand { get; }
	public ICommand ToggleSelectCommand { get; }

	public Action<bool> CloseView { get; set; }

	private readonly MainWindowViewModel _mainWindowViewModel;

	public void Add(DivinityModUpdateData mod) => Mods.Add(mod);

	public void Add(IEnumerable<DivinityModUpdateData> mods) => Mods.AddRange(mods);

	public void Clear()
	{
		Mods.Clear();
		Unlocked = true;
	}

	public void SelectAll(bool select = true)
	{
		foreach (var x in Mods.Items)
		{
			x.IsSelected = select;
		}
	}

	public void UpdateSelectedMods()
	{
		var documentsFolder = _mainWindowViewModel.PathwayData.AppDataGameFolder;
		var modPakFolder = _mainWindowViewModel.PathwayData.AppDataModsPath;

		using var dialog = new TaskDialog()
		{
			Buttons = {
				new TaskDialogButton(ButtonType.Yes),
				new TaskDialogButton(ButtonType.No)
			},
			WindowTitle = "Update Mods?",
			Content = "Download / copy updates? Previous pak files will be moved to the Recycle Bin.",
			MainIcon = TaskDialogIcon.Warning
		};
		var result = dialog.ShowDialog(MainWindow.Self);
		if (result.ButtonType == ButtonType.Yes)
		{
			var updates = Mods.Items.Where(x => x.IsSelected).ToList();

			Unlocked = false;

			StartUpdating(new CopyModUpdatesTask()
			{
				DocumentsFolder = documentsFolder,
				ModPakFolder = modPakFolder,
				Updates = Mods.Items.Where(x => x.IsSelected).ToList(),
				TotalProcessed = 0
			});
		}
	}

	private async Task<UpdateTaskResult> AwaitDownloadPartition(IEnumerator<DivinityModUpdateData> partition, int progressIncrement, string outputFolder, CancellationToken token)
	{
		var result = new UpdateTaskResult();
		using (partition)
		{
			while (partition.MoveNext())
			{
				result.ModId = partition.Current.Mod.UUID;
				if (token.IsCancellationRequested) return result;
				await Task.Yield(); // prevents a sync/hot thread hangup
				var downloadResult = await partition.Current.DownloadData.DownloadAsync(partition.Current.LocalFilePath, outputFolder, token);
				result.Success = downloadResult.Success;
				await _mainWindowViewModel.IncreaseMainProgressValueAsync(progressIncrement);
			}
		}
		return result;
	}

	private async Task<Unit> ProcessUpdatesAsync(CopyModUpdatesTask taskData, IScheduler sch, CancellationToken token)
	{
		await _mainWindowViewModel.StartMainProgressAsync("Processing updates...");
		var currentTime = DateTime.Now;
		var partitionAmount = Environment.ProcessorCount;
		var progressIncrement = (int)Math.Ceiling(100d / taskData.Updates.Count);
		var results = await Task.WhenAll(Partitioner.Create(taskData.Updates).GetPartitions(partitionAmount).AsParallel().Select(p => AwaitDownloadPartition(p, progressIncrement, taskData.ModPakFolder, token)));
		UpdateLastUpdated(results);
		await Observable.Start(FinishUpdating, RxApp.MainThreadScheduler);
		return Unit.Default;
	}

	private void StartUpdating(CopyModUpdatesTask taskData)
	{
		RxApp.MainThreadScheduler.ScheduleAsync(async (sch, token) => await ProcessUpdatesAsync(taskData, sch, token));
	}

	private void UpdateLastUpdated(UpdateTaskResult[] results)
	{
		var settings = Services.Get<ISettingsService>();
		settings.UpdateLastUpdated(results.Where(x => x.Success == true).Select(x => x.ModId).ToList());
	}

	private void FinishUpdating()
	{
		Unlocked = true;
		JustUpdated = true;
		CloseView?.Invoke(true);
	}

	public ModUpdatesViewData(MainWindowViewModel mainWindowViewModel)
	{
		Unlocked = true;
		AllSelected = true;

		_mainWindowViewModel = mainWindowViewModel;

		Mods.CountChanged.ToUIProperty(this, x => x.TotalUpdates);

		var modsConnection = Mods.Connect();

		modsConnection.Bind(out _updates).Subscribe();

		var selectedMods = modsConnection.AutoRefresh(x => x.IsSelected).ToCollection();
		selectedMods.Select(x => x.Any(y => y.IsSelected)).ToUIProperty(this, x => x.AnySelected);
		selectedMods.Select(x => x.All(y => y.IsSelected)).ToUIProperty(this, x => x.AllSelected);

		var anySelectedObservable = this.WhenAnyValue(x => x.AnySelected);

		UpdateModsCommand = ReactiveCommand.Create(UpdateSelectedMods, anySelectedObservable);

		ToggleSelectCommand = ReactiveCommand.Create<bool>(b =>
		{
			foreach (var x in Updates)
			{
				x.IsSelected = b;
			}
		});
	}
}
