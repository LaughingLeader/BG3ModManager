using Avalonia.Media;

using DynamicData;
using DynamicData.Binding;

using Humanizer;

using Material.Icons;

using ModManager.Helpers.Sorting;
using ModManager.Models.Interfaces;
using ModManager.Services;
using ModManager.Utils;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace ModManager.Models.View;
public class ModFileEntry : ReactiveObject, IFileModel
{
	private readonly IFileSystemService _fs;

	private readonly SourceCache<ModFileEntry, string> _children = new(x => x.FilePath);

	private readonly ReadOnlyObservableCollection<ModFileEntry> _uiSubfiles;
	public ReadOnlyObservableCollection<ModFileEntry> Subfiles => _uiSubfiles;

	public void AddChild(ModFileEntry child) => _children.AddOrUpdate(child);
	public void AddChild(IEnumerable<ModFileEntry> children) => _children.AddOrUpdate(children);

	public bool TryGetChild(string filePath, [NotNullWhen(true)] out ModFileEntry? child)
	{
		child = null;
		var result = _children.Lookup(filePath);
		if (result.HasValue)
		{
			child = result.Value;
			return true;
		}
		return false;
	}

	public void ClearChildren()
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			_children.Clear();
		});
	}

	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public double SizeOnDisk { get; set; }
	[Reactive] public MaterialIconKind Icon { get; set; }
	[Reactive] public IBrush IconColor { get; set; }

	[ObservableAsProperty] public string? Size { get; }
	[ObservableAsProperty] public string? ExtensionDisplayName { get; }

	public string SourcePakFilePath { get; }
	public string FilePath { get; }
	public string FileName { get; }
	public string FileExtension { get; }
	public bool IsDirectory { get; }
	public bool IsFromPak { get; }

	public void PrintStructure(int indent = 0)
	{
		if (indent > 0)
		{
			var ending = IsDirectory ? _fs.Path.DirectorySeparatorChar.ToString() : "*";
			DivinityApp.Log($"{new string('\t', indent - 1)}{FileName}{ending}");
		}
		foreach(var child in Subfiles)
		{
			child.PrintStructure(indent + 1);
		}
	}

	private static string GetExtensionDisplayName(bool isDir, string ext)
	{
		if (isDir) return "<dir>";
		if (ext.Length > 1)
		{
			return ext[1..];
		}
		return ext;
	}

	public ModFileEntry(string sourcePakFilePath, string filePath, IFileSystemService fs, bool isDirectory = false, double size = 0) : base()
	{
		_fs = fs;
		SourcePakFilePath = sourcePakFilePath;
		FilePath = filePath;
		SizeOnDisk = size;
		FileName = fs.Path.GetFileName(filePath).NormalizeDirectorySep()!;
		IsDirectory = isDirectory;

		_children.Connect().ObserveOn(RxApp.MainThreadScheduler).SortAndBind(out _uiSubfiles, Sorters.FileIgnoreCase).DisposeMany().Subscribe();

		if(!IsDirectory)
		{
			FileExtension = fs.Path.GetExtension(FilePath).ToLower();
		}
		else
		{
			FileExtension = string.Empty;
		}

		this.WhenAnyValue(x => x.SizeOnDisk)
			.Select(x => x > 0 ? x.Bytes().Humanize() : string.Empty)
			.ToUIProperty(this, x => x.Size);

		this.WhenAnyValue(x => x.IsDirectory, x => x.FileExtension, GetExtensionDisplayName)
			.ToUIProperty(this, x => x.ExtensionDisplayName);

		if (isDirectory)
		{
			Icon = MaterialIconKind.Folder;
		}
		else
		{
			Icon = MaterialIconUtils.ExtensionToIconKind(FileExtension);
		}

		IsFromPak = SourcePakFilePath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase);

		if (FileExtension.Equals(".pak"))
		{
			this.WhenAnyValue(x => x.IsExpanded).Select(x => x ? MaterialIconKind.PackageVariant : MaterialIconKind.PackageVariantClosed).BindTo(this, x => x.Icon);
		}

		if (IsDirectory)
		{
			IconColor = Brushes.Tan;
		}
		else
		{
			IconColor = MaterialIconUtils.ExtensionToIconBrush(FileExtension);
		}
	}
}
