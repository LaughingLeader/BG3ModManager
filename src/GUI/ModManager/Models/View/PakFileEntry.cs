using DynamicData;
using DynamicData.Binding;

using Material.Icons;

using ModManager.Helpers;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace ModManager.Models.View;
public class PakFileEntry : ReactiveObject, IFileModel
{
	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsSelected { get; set; }

	private readonly SourceCache<PakFileEntry, string> _children = new(x => x.FilePath);

	private readonly ReadOnlyObservableCollection<PakFileEntry> _uiSubfiles;
	public ReadOnlyObservableCollection<PakFileEntry> Subfiles => _uiSubfiles;

	public void AddChild(PakFileEntry child) => _children.AddOrUpdate(child);
	public void AddChild(IEnumerable<PakFileEntry> children) => _children.AddOrUpdate(children);

	public bool TryGetChild(string filePath, [NotNullWhen(true)] out PakFileEntry? child)
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

	public string FilePath { get; }
	public string FileName { get; }
	public bool IsDirectory { get; }
	public MaterialIconKind Icon { get; }

	public void PrintStructure(int indent = 0)
	{
		if (indent > 0)
		{
			var ending = IsDirectory ? Path.DirectorySeparatorChar.ToString() : "*";
			DivinityApp.Log($"{new string('\t', indent - 1)}{FileName}{ending}");
		}
		foreach(var child in Subfiles)
		{
			child.PrintStructure(indent + 1);
		}
	}

	private static readonly IComparer<PakFileEntry> _fileSort = new NaturalFileSortComparer(StringComparison.OrdinalIgnoreCase);

	public PakFileEntry(string filePath, bool isDirectory = false) : base()
	{
		FilePath = filePath;
		FileName = Path.GetFileName(filePath).Replace(Path.DirectorySeparatorChar, '/');
		IsDirectory = isDirectory;

		_children.Connect().ObserveOn(RxApp.MainThreadScheduler).SortAndBind(out _uiSubfiles, _fileSort).DisposeMany().Subscribe();

		if(isDirectory)
		{
			Icon = MaterialIconKind.Folder;
		}
		else
		{
			Icon = Path.GetExtension(FilePath).ToLower() switch
			{
				".lua" => MaterialIconKind.LanguageLua,
				".lsx" or ".xml" => MaterialIconKind.Xml,
				".xaml" => MaterialIconKind.LanguageXaml,
				".lsj" or ".json" => MaterialIconKind.CodeJson,
				".gr2" or ".dae" => MaterialIconKind.BlenderSoftware,
				".dds" or ".png" => MaterialIconKind.Texture,
				".jpg" or ".png" => MaterialIconKind.Image,
				".md" => MaterialIconKind.LanguageMarkdown,
				".loca" => MaterialIconKind.Language,
				".txt" => MaterialIconKind.Text,
				_ => MaterialIconKind.File,
			};
		}
		
	}
}
