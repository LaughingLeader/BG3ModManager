using DivinityModManager.Models;
using DivinityModManager.Models.Mod;
using DivinityModManager.Models.Updates;
using DivinityModManager.Windows;

using DynamicData;
using DynamicData.Binding;

using Ookii.Dialogs.Wpf;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Splat;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace DivinityModManager.ViewModels.Main;

public class CopyModUpdatesTask
{
	public List<DivinityModUpdateData> Updates { get; set; }
	public string DocumentsFolder { get; set; }
	public string ModPakFolder { get; set; }
	public int TotalProcessed { get; set; }
}

public class ModUpdatesViewModel : ReactiveObject, IRoutableViewModel
{
	public string UrlPathSegment => "modupdates";
	public IScreen HostScreen { get; }

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
		var result = dialog.ShowDialog(App.WM.Main.Window);
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

	internal ModUpdatesViewModel(IScreen host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>();

		Unlocked = true;
		AllSelected = true;

		Mods.CountChanged.ToUIProperty(this, x => x.TotalUpdates);

		var modsConnection = Mods.Connect();

		modsConnection.ObserveOn(RxApp.MainThreadScheduler).Bind(out _updates).DisposeMany().Subscribe();

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

	public ModUpdatesViewModel(MainWindowViewModel mainWindowViewModel) : this((IScreen)mainWindowViewModel)
	{
		_mainWindowViewModel = mainWindowViewModel;
	}
}


public class DesignModUpdatesViewModel : ModUpdatesViewModel
{
	public DesignModUpdatesViewModel() : base()
	{
		Add(new DivinityModUpdateData()
		{
			Mod = new DivinityModData() { Name = "Test Mod", Author = "LaughingLeader", UUID = "0" },
			DownloadData = new ModDownloadData()
			{
				DownloadPath = "",
				DownloadPathType = ModDownloadPathType.URL,
				DownloadSourceType = ModSourceType.GITHUB,
				Version = "1.0.0.1",
				Date = DateTimeOffset.Now
			},
		});
		Add(new DivinityModUpdateData()
		{
			Mod = new DivinityModData() { Name = "Test Mod 2", Author = "LaughingLeader", UUID = "1" },
			DownloadData = new ModDownloadData()
			{
				DownloadPath = "",
				DownloadPathType = ModDownloadPathType.URL,
				DownloadSourceType = ModSourceType.NEXUSMODS,
				Version = "0.1.0.0",
				Date = DateTimeOffset.Now
			},
		});
	}
}

