using ModManager.Models.Mod;

using System.Globalization;

namespace ModManager.Models.Updates;

public class DivinityModUpdateData : ReactiveObject, ISelectable
{
	[Reactive] public ModData Mod { get; set; }
	[Reactive] public ModDownloadData DownloadData { get; set; }
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public bool CanDrag { get; set; }
	[Reactive] public bool IsHidden { get; set; }
	public bool IsDraggable => false;

	[ObservableAsProperty] public ModSourceType Source { get; }
	[ObservableAsProperty] public bool IsEditorMod { get; }
	[ObservableAsProperty] public string? Author { get; }
	[ObservableAsProperty] public string? CurrentVersion { get; }
	[ObservableAsProperty] public string? UpdateVersion { get; }
	[ObservableAsProperty] public string? SourceText { get; }
	[ObservableAsProperty] public Uri? UpdateLink { get; }
	[ObservableAsProperty] public string? LocalFilePath { get; }
	[ObservableAsProperty] public string? LocalFileDateText { get; }
	[ObservableAsProperty] public string? UpdateFilePath { get; }
	[ObservableAsProperty] public string? UpdateDateText { get; }
	[ObservableAsProperty] public string? UpdateToolTip { get; }

	private Uri? SourceToLink(ValueTuple<ModData, ModSourceType> data)
	{
		if (data.Item1 != null)
		{
			var url = data.Item1.GetURL(data.Item2);
			if (url.IsValid())
			{
				return new Uri(url);
			}
		}
		return null;
	}

	private string DateToString(DateTimeOffset? date)
	{
		if (date.HasValue)
		{
			return date.Value.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture);
		}
		return "";
	}

	private string GetUpdateTooltip(ValueTuple<string, Uri> data)
	{
		var description = data.Item1;
		var url = data.Item2;
		var result = "";
		if (description.IsValid())
		{
			result = description;
		}
		if (url?.AbsoluteUri.IsValid() == true)
		{
			if (result.IsValid()) result += Environment.NewLine;
			result += url.AbsoluteUri;
		}
		return result;
	}

	public DivinityModUpdateData(ModData mod, ModDownloadData downloadData)
	{
		IsSelected = true;
		CanDrag = true;

		Mod = mod;
		DownloadData = downloadData;

		this.WhenAnyValue(x => x.Mod.IsLooseMod).ToUIProperty(this, x => x.IsEditorMod);
		this.WhenAnyValue(x => x.Mod.AuthorDisplayName).ToUIProperty(this, x => x.Author);
		this.WhenAnyValue(x => x.Mod.Version.Version).ToUIProperty(this, x => x.CurrentVersion);
		this.WhenAnyValue(x => x.Mod.FilePath).ToUIProperty(this, x => x.LocalFilePath);
		this.WhenAnyValue(x => x.Mod.LastModified).Select(DateToString).ToUIProperty(this, x => x.LocalFileDateText);

		var whenSource = this.WhenAnyValue(x => x.DownloadData.DownloadSourceType);
		whenSource.ToPropertyEx(this, x => x.Source);
		whenSource.Select(x => x.GetDescription()).ToPropertyEx(this, x => x.SourceText);

		this.WhenAnyValue(x => x.DownloadData.DownloadPath).ToUIProperty(this, x => x.UpdateFilePath);
		this.WhenAnyValue(x => x.DownloadData.Date).Select(DateToString).ToUIProperty(this, x => x.UpdateDateText);
		this.WhenAnyValue(x => x.DownloadData.Version).ToUIProperty(this, x => x.UpdateVersion);

		this.WhenAnyValue(x => x.Mod, x => x.Source).Select(SourceToLink).ToPropertyEx(this, x => x.UpdateLink);


		this.WhenAnyValue(x => x.DownloadData.Description, x => x.UpdateLink).Select(GetUpdateTooltip).ToPropertyEx(this, x => x.UpdateToolTip);
	}
}
