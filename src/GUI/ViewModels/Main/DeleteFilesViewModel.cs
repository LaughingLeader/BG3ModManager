using DivinityModManager.Models;
using DivinityModManager.Util;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Splat;

using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;

namespace DivinityModManager.ViewModels.Main;

public class FileDeletionCompleteEventArgs : EventArgs
{
	public int TotalFilesDeleted => DeletedFiles?.Count ?? 0;
	public List<ModFileDeletionData> DeletedFiles { get; set; }
	public bool RemoveFromLoadOrder { get; set; }
	public bool IsDeletingDuplicates { get; set; }

	public FileDeletionCompleteEventArgs()
	{
		DeletedFiles = [];
	}
}

public class DeleteFilesViewModel : BaseProgressViewModel, IRoutableViewModel
{
	public string UrlPathSegment => "deletefiles";
	public IScreen HostScreen { get; }

	[Reactive] public bool PermanentlyDelete { get; set; }
	[Reactive] public bool RemoveFromLoadOrder { get; set; }
	[Reactive] public bool IsDeletingDuplicates { get; set; }
	[Reactive] public double DuplicateColumnWidth { get; set; }

	public ObservableCollectionExtended<ModFileDeletionData> Files { get; set; } = new ObservableCollectionExtended<ModFileDeletionData>();

	[ObservableAsProperty] public bool AnySelected { get; }
	[ObservableAsProperty] public bool AllSelected { get; }
	[ObservableAsProperty] public string SelectAllTooltip { get; }
	[ObservableAsProperty] public string Title { get; }
	[ObservableAsProperty] public Visibility RemoveFromLoadOrderVisibility { get; }

	public RxCommandUnit SelectAllCommand { get; private set; }

	public event EventHandler<FileDeletionCompleteEventArgs> FileDeletionComplete;

	public override async Task<bool> Run(CancellationToken token)
	{
		var targetFiles = Files.Where(x => x.IsSelected).ToList();

		await UpdateProgress($"Confirming deletion...", "", 0d);

		var result = await DivinityInteractions.ConfirmModDeletion.Handle(new DeleteFilesViewConfirmationData { Total = targetFiles.Count, PermanentlyDelete = PermanentlyDelete, Token = token });
		if (result)
		{
			var eventArgs = new FileDeletionCompleteEventArgs()
			{
				IsDeletingDuplicates = IsDeletingDuplicates,
				RemoveFromLoadOrder = !IsDeletingDuplicates && RemoveFromLoadOrder,
			};

			await Observable.Start(() => IsProgressActive = true, RxApp.MainThreadScheduler);
			await UpdateProgress($"Deleting {targetFiles.Count} mod file(s)...", "", 0d);
			double progressInc = 1d / targetFiles.Count;
			foreach (var f in targetFiles)
			{
				try
				{
					if (token.IsCancellationRequested)
					{
						DivinityApp.Log("Deletion stopped.");
						break;
					}
					if (File.Exists(f.FilePath))
					{
						await UpdateProgress("", $"Deleting {f.FilePath}...");
#if DEBUG
						eventArgs.DeletedFiles.Add(f);
#else
						if (RecycleBinHelper.DeleteFile(f.FilePath, false, PermanentlyDelete))
						{
							eventArgs.DeletedFiles.Add(f);
							DivinityApp.Log($"Deleted mod file '{f.FilePath}'");
						}
#endif
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error deleting file '${f.FilePath}':\n{ex}");
				}
				await UpdateProgress("", "", ProgressValue + progressInc);
			}
			await UpdateProgress("", "", 1d);
			await Task.Delay(500);
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				FileDeletionComplete?.Invoke(this, eventArgs);
				Close();
			});
		}
		return true;
	}

	public override void Close()
	{
		base.Close();
		Files.Clear();
	}

	public void ToggleSelectAll()
	{
		var b = !AllSelected;
		foreach (var f in Files)
		{
			f.IsSelected = b;
		}
	}

	private bool IsClosingAllowed(bool isDeletingDupes, int totalFiles) => !isDeletingDupes || totalFiles <= 0;

	public DeleteFilesViewModel(IScreen hostScreen)
	{
		HostScreen = hostScreen ?? Locator.Current.GetService<IScreen>();

		RemoveFromLoadOrder = true;
		PermanentlyDelete = false;

		//this.WhenAnyValue(x => x.IsDeletingDuplicates, x => x.Files.Count).Select(x => IsClosingAllowed(x.Item1, x.Item2)).BindTo(this, x => x.CanClose);

		this.WhenAnyValue(x => x.IsDeletingDuplicates).Select(PropertyConverters.BoolToVisibilityReversed).ToUIProperty(this, x => x.RemoveFromLoadOrderVisibility);
		this.WhenAnyValue(x => x.IsDeletingDuplicates).Select(b => !b ? "Files to Delete" : "Duplicate Mods to Delete").ToUIProperty(this, x => x.Title);

		var filesChanged = Files.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).ToCollection().Throttle(TimeSpan.FromMilliseconds(50)).ObserveOn(RxApp.MainThreadScheduler);
		filesChanged.Select(x => x.Any(y => y.IsSelected)).ToUIProperty(this, x => x.AnySelected);
		filesChanged.Select(x => x.All(y => y.IsSelected)).ToUIProperty(this, x => x.AllSelected);
		this.WhenAnyValue(x => x.AllSelected).Select(b => $"{(b ? "Deselect" : "Select")} All").ToUIProperty(this, x => x.SelectAllTooltip);

		SelectAllCommand = ReactiveCommand.Create(ToggleSelectAll, RunCommand.IsExecuting.Select(b => !b), RxApp.MainThreadScheduler);

		this.WhenAnyValue(x => x.AnySelected).BindTo(this, x => x.CanRun);
	}
}
