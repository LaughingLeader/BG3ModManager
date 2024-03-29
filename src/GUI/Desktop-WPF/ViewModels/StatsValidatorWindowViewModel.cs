using DynamicData.Binding;

using Humanizer;
using Humanizer.Localisation;

using LSLib.LS.Stats;
using LSLib.LS.Story.GoalParser;

using ModManager.Models.Mod;
using ModManager.Models.View;
using ModManager.Util;

using System.Globalization;
using System.Windows;

namespace ModManager.ViewModels;
public class StatsValidatorWindowViewModel : BaseWindowViewModel
{
	[Reactive] public DivinityModData Mod { get; set; }
	[Reactive] public string OutputText { get; private set; }
	[Reactive] public TimeSpan TimeTaken { get; private set; }

	public ObservableCollectionExtended<StatsValidatorFileResults> Entries { get; }

	[ObservableAsProperty] public string ModName { get; }
	[ObservableAsProperty] public string TimeTakenText { get; }
	[ObservableAsProperty] public Visibility HasTimeTakenText { get; }
	[ObservableAsProperty] public Visibility LockScreenVisibility { get; }

	public ReactiveCommand<DivinityModData, Unit> ValidateCommand { get; }
	public RxCommandUnit CancelValidateCommand { get; }

	private static string FormatMessage(StatLoadingError message)
	{
		var result = "";
		if (message.Code == DiagnosticCode.StatSyntaxError)
		{
			result += "[ERR] ";
		}
		else
		{
			result += "[WARN] ";
		}

		if (!String.IsNullOrEmpty(message.Location?.FileName))
		{
			var baseName = Path.GetFileName(message.Location.FileName);
			result += $"{baseName}:{message.Location.StartLine}: ";
		}

		result += $"[{message.Code}] {message.Message}";
		return result;
	}

	private static string GetLineText(string filePath, StatLoadingError error, Dictionary<string, string[]> fileText)
	{
		if (fileText.TryGetValue(filePath, out var lines))
		{
			var uniqueContexts = new List<CodeLocation>
			{
				error.Location
			};
			if (error.Contexts != null)
			{
				uniqueContexts.AddRange(error.Contexts.Where(x => x.Location != null).Select(x => x.Location));
			}

			var location = uniqueContexts.DistinctBy(x => x.StartLine).FirstOrDefault();

			var startLine = location.StartLine - 1;
			var endLine = location.EndLine - 1;
			if (startLine != endLine)
			{
				var lineText = new List<string>();
				for (var i = startLine; i < endLine; i++)
				{
					lineText.Add(lines[i].Trim());
				}
				return String.Join(Environment.NewLine, lineText);
			}
			else if (lines != null && startLine < lines.Length)
			{
				return lines[startLine].Trim();
			}
		}
		return String.Empty;
	}

	public void Load(ValidateModStatsResults result)
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			//TimeTaken = result.TimeTaken;
			Mod = result.Mods.FirstOrDefault();
			Entries.Clear();

			if (result.Errors.Count == 0)
			{
				OutputText = "No issues found!";
			}
			else
			{
				OutputText = $"{result.Errors.Count} issue(s):";
			}

			var entries = result.Errors.GroupBy(x => x.Location?.FileName);
			foreach (var fileGroup in entries)
			{
				var name = fileGroup.Key;
				if (String.IsNullOrEmpty(name)) name = "Unknown";
				StatsValidatorFileResults fileResults = new() { FilePath = name };
				foreach (var entry in fileGroup)
				{
					fileResults.Children.Add(new StatsValidatorErrorEntry(entry, GetLineText(name, entry, result.FileText)));
				}
				Entries.Add(fileResults);
			}
		});
	}

	private async Task StartValidationAsyncImpl(ValidateModStatsRequest data)
	{
		var gameDataPath = AppServices.Settings.ManagerSettings.GameDataPath;
		var validator = AppServices.Get<IStatsValidatorService>();

		var startTime = DateTimeOffset.Now;

		if (validator.GameDataPath != gameDataPath)
		{
			await DivinityApp.ShowAlertAsync("Initializing base data...", AlertType.Info);
			await Task.Run(() =>
			{
				validator.Initialize(gameDataPath);
			}, data.Token);
		}
		else
		{
			await DivinityApp.ShowAlertAsync("Validating mod stats...", AlertType.Info, 200);
		}

		var results = await Observable.StartAsync(async () =>
		{
			return await validator.ValidateModsAsync(data.Mods, data.Token);
			//eturn await ModUtils.ValidateStatsAsync(interaction.Input.Mods, AppServices.Settings.ManagerSettings.GameDataPath, interaction.Input.Token);
		}, RxApp.TaskpoolScheduler);

		await Observable.Start(() =>
		{
			TimeTaken = DateTimeOffset.Now - startTime;
			DivinityApp.ShowAlert($"Validation complete for {string.Join(";", data.Mods.Select(x => x.DisplayName))}", AlertType.Success, 30);
		}, RxApp.MainThreadScheduler);

		await DivinityInteractions.OpenValidateStatsResults.Handle(results);
	}

	private IObservable<Unit> StartValidationAsync(DivinityModData mod)
	{
		return ObservableEx.CreateAndStartAsync(token => StartValidationAsyncImpl(new([mod], token)), RxApp.TaskpoolScheduler)
			.TakeUntil(CancelValidateCommand);
	}

	private static string TimeTakenToText(TimeSpan time)
	{
		if (time == TimeSpan.Zero) return string.Empty;
		return time.Humanize(1, CultureInfo.CurrentCulture, TimeUnit.Second, TimeUnit.Second).ApplyCase(LetterCasing.Title);
	}

	public StatsValidatorWindowViewModel()
	{
		Entries = [];

		this.WhenAnyValue(x => x.Mod).WhereNotNull().Select(x => x.DisplayName).ToUIProperty(this, x => x.ModName);
		this.WhenAnyValue(x => x.TimeTaken).Select(TimeTakenToText).ToUIProperty(this, x => x.TimeTakenText, "");
		this.WhenAnyValue(x => x.TimeTakenText).Select(PropertyConverters.StringToVisibility).ToUIProperty(this, x => x.HasTimeTakenText, Visibility.Collapsed);

		var canValidate = this.WhenAnyValue(x => x.Mod).Select(x => x != null);

		ValidateCommand = ReactiveCommand.CreateFromObservable<DivinityModData, Unit>(StartValidationAsync, canValidate);
		CancelValidateCommand = ReactiveCommand.Create(() => { }, ValidateCommand.IsExecuting);

		ValidateCommand.IsExecuting.Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.LockScreenVisibility, Visibility.Collapsed);

		DivinityInteractions.RequestValidateModStats.RegisterHandler(async interaction =>
		{
			interaction.SetOutput(true);
			await StartValidationAsyncImpl(interaction.Input);
		});
	}
}
