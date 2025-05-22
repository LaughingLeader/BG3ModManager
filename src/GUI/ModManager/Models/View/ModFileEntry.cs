using DynamicData;
using DynamicData.Binding;

using Humanizer;

using Material.Icons;

using ModManager.Helpers;

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace ModManager.Models.View;
public class ModFileEntry : ReactiveObject, IFileModel
{
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

	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsSelected { get; set; }

	[ObservableAsProperty] public string Size { get; }

	public string FilePath { get; }
	public string FileName { get; }
	public bool IsDirectory { get; }
	[Reactive] public double SizeOnDisk { get; set; }
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

	private static readonly IComparer<ModFileEntry> _fileSort = new NaturalFileSortComparer(StringComparison.OrdinalIgnoreCase);

	public ModFileEntry(string filePath, bool isDirectory = false, double size = 0) : base()
	{
		SizeOnDisk = size;
		FilePath = filePath;
		FileName = Path.GetFileName(filePath).Replace(Path.DirectorySeparatorChar, '/');
		IsDirectory = isDirectory;

		this.WhenAnyValue(x => x.SizeOnDisk).Select(x => x > 0 ? x.Bytes().Humanize() : string.Empty).ToUIProperty(this, x => x.Size);

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
