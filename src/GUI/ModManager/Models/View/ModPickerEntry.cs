using Avalonia.Media;

using Material.Icons;

using ModManager.Models.Interfaces;
using ModManager.Models.Mod;
using ModManager.Services;
using ModManager.Util;
using ModManager.Utils;

using System.IO.Abstractions;
using System.Text;

namespace ModManager.Models.View;
public class ModPickerEntry : ReactiveObject, INamedEntry
{
	public ModData Mod { get; }
	public string UUID { get; }
	[Reactive] public bool IsSelected { get; set; }

	[ObservableAsProperty] public string? Name { get; }
	[ObservableAsProperty] public string? FilePath { get; }
	[ObservableAsProperty] public string? DisplayFilePath { get; }
	[ObservableAsProperty] public string? FileExtension { get; }
	[ObservableAsProperty] public string? ShortDescription { get; }
	[ObservableAsProperty] public MaterialIconKind Icon { get; }
	[ObservableAsProperty] public IBrush? IconColor { get; }
	[ObservableAsProperty] public bool IsLooseMod { get; }
	[ObservableAsProperty] public bool IsInDataFolder { get; }
	[ObservableAsProperty] public bool IsHidden { get; }

	private static readonly IFileSystemService _fs;
	static ModPickerEntry()
	{
		_fs = AppServices.FS;
	}

	private static bool ParentFolderContainsData(IDirectoryInfo info)
	{
		if(info?.Name == "Data")
		{
			return true;
		}
		if(info?.Parent != null)
		{
			return ParentFolderContainsData(info.Parent);
		}
		return false;
	}

	private static bool CheckForDataFolder(string? path)
	{
		if (path.IsValid())
		{
			var info = _fs.FileInfo.New(path);
			if (info != null && info.Directory?.Parent != null)
			{
				if (ParentFolderContainsData(info.Directory))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static string? GetRelativePath(string? path, bool isLooseMod, bool isInDataFolder)
	{
		if(path.IsValid())
		{
			if(isInDataFolder)
			{
				var info = _fs.FileInfo.New(path);
				if (info != null && info.Directory?.Parent != null)
				{
					return _fs.Path.GetRelativePath(info.Directory.Parent.FullName, path);
				}
			}
			else
			{
				return _fs.Path.GetFileName(path);
			}
			return path;
		}
		return string.Empty;
	}

	private static IBrush? GetIconColor(string? ext, bool isLooseMod, bool isInDataFolder)
	{
		if(!isLooseMod && isInDataFolder)
		{
			return Brushes.MediumSlateBlue;
		}
		return MaterialIconUtils.ExtensionToIconBrush(ext);
	}

	private static string? GetShortDescription(string? displayName, string? description, string? author, string? filePath, bool isToolkitProject, bool isLooseMod)
	{
		var sb = new StringBuilder();
		var hasName = displayName.IsValid();
		var hasAuthor = author.IsValid();
		if(hasName && hasAuthor)
		{
			sb.Append($"{displayName} by ${author}");
			if(isToolkitProject)
			{
				sb.Append(" [Toolkit Project]");
			}
			else if(isLooseMod)
			{
				sb.Append(" [Loose Mod]");
			}
			sb.AppendLine();
		}
		else if(hasName)
		{
			sb.AppendLine(displayName);
		}
		if (description.IsValid())
		{
			if (hasName) sb.AppendLine();
			sb.AppendLine(description);
			sb.AppendLine();
		}
		if (filePath.IsValid()) sb.AppendLine(filePath);
		return sb.ToString();
	}

	public ModPickerEntry(ModData mod)
	{
		Mod = mod;
		UUID = mod.UUID;

		mod.WhenAnyValue(x => x.Name).ToUIProperty(this, x => x.Name);
		mod.WhenAnyValue(x => x.IsLooseMod).ToUIProperty(this, x => x.IsLooseMod);
		mod.WhenAnyValue(x => x.IsHidden).ToUIProperty(this, x => x.IsHidden);
		var whenFilePath = mod.WhenAnyValue(x => x.FilePath);
		whenFilePath.ToUIProperty(this, x => x.FilePath);
		whenFilePath.Select(CheckForDataFolder).ToUIProperty(this, x => x.IsInDataFolder);
		whenFilePath.Select(path => _fs.Path.GetExtension(path)?.ToLower()).ToUIProperty(this, x => x.FileExtension);
		this.WhenAnyValue(x => x.FileExtension, MaterialIconUtils.ExtensionToModIconKind).StartWith(MaterialIconKind.File).ToUIProperty(this, x => x.Icon);
		this.WhenAnyValue(x => x.FileExtension, x => x.IsLooseMod, x => x.IsInDataFolder, GetIconColor).StartWith(Brushes.White).ToUIProperty(this, x => x.IconColor);
		this.WhenAnyValue(x => x.FilePath, x => x.IsLooseMod, x => x.IsInDataFolder, GetRelativePath).ToUIProperty(this, x => x.DisplayFilePath);

		mod.WhenAnyValue(x => x.DisplayName, x => x.Description, x => x.AuthorDisplayName, x => x.FilePath, x => x.IsToolkitProject, x => x.IsLooseMod, GetShortDescription).ToUIProperty(this, x => x.ShortDescription);
	}
}
