using DivinityModManager.Models;
using DivinityModManager.Models.View;
using DivinityModManager.Util;

using DynamicData.Binding;

using LSLib.LS.Stats;
using LSLib.LS.Story.GoalParser;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace DivinityModManager.ViewModels;
public class StatsValidatorWindowViewModel : ReactiveObject
{
	[Reactive] public DivinityModData Mod { get; set; }
	[Reactive] public string OutputText { get; private set; }

	public ObservableCollectionExtended<StatsValidatorFileResults> Entries { get; }

	[ObservableAsProperty] public string ModName { get; }
	[ObservableAsProperty] public Visibility LockScreenVisibility { get; }

	public ReactiveCommand<DivinityModData, Unit> ValidateCommand { get; }
	public ReactiveCommand<Unit, Unit> CancelValidateCommand { get; }

	private static string FormatMessage(StatLoadingError message)
	{
		string result = "";
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
		if(fileText.TryGetValue(filePath, out var lines))
		{
			var uniqueContexts = new List<CodeLocation>
			{
				error.Location
			};
			if(error.Contexts != null)
			{
                uniqueContexts.AddRange(error.Contexts.Where(x => x.Location != null).Select(x => x.Location));
            }

            var location = uniqueContexts.DistinctBy(x => x.StartLine).FirstOrDefault();

            var startLine = location.StartLine - 1;
			var endLine = location.EndLine - 1;
			if(startLine != endLine)
			{
				var lineText = new List<string>();
				for(var i = startLine; i < endLine; i++)
				{
					lineText.Add(lines[i]);
				}
				return String.Join(Environment.NewLine, lineText);
			}
			else
			{
				return lines[startLine];
			}
		}
		return String.Empty;
	}

	public void Load(ValidateModStatsResults result)
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
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
			foreach(var fileGroup in entries)
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

	private IObservable<Unit> StartValidationAsync(DivinityModData mod)
	{
		return ObservableEx.CreateAndStartAsync(token => DivinityGlobalCommands.ValidateModStats(mod, token), RxApp.TaskpoolScheduler)
			.TakeUntil(CancelValidateCommand);
	}

	public StatsValidatorWindowViewModel()
	{
		Entries = [];

		this.WhenAnyValue(x => x.Mod).WhereNotNull().Select(x => x.DisplayName).ToUIProperty(this, x => x.ModName);

		var canValidate = this.WhenAnyValue(x => x.Mod).Select(x => x != null);

		ValidateCommand = ReactiveCommand.CreateFromObservable<DivinityModData, Unit>(StartValidationAsync, canValidate);
		CancelValidateCommand = ReactiveCommand.Create(() => { }, ValidateCommand.IsExecuting);

		ValidateCommand.IsExecuting.Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.LockScreenVisibility, Visibility.Collapsed);
	}
}
