using DynamicData;
using DynamicData.Binding;

using Material.Icons;

using ModManager.Helpers;

using System.Collections.ObjectModel;

namespace ModManager.Models.View;
public class PakFileEntry : TreeViewEntry, IFileModel
{
	public override object ViewModel => this;

	public string FilePath { get; }
	public string FileName { get; }
	public bool IsDirectory { get; }
	public MaterialIconKind Icon { get; }

	private readonly ReadOnlyObservableCollection<PakFileEntry> _subfiles;

	public ReadOnlyObservableCollection<PakFileEntry> Subfiles => _subfiles;

	public void PrintStructure(int indent = 0)
	{
		if (indent > 0)
		{
			var ending = IsDirectory ? Path.DirectorySeparatorChar.ToString() : "*";
			DivinityApp.Log($"{new string('\t', indent - 1)}{FileName}{ending}");
		}
		foreach(var child in Children.Cast<PakFileEntry>())
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

		this.Children.ToObservableChangeSet()
			.Transform(x => (PakFileEntry)x)
			.Sort(_fileSort)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Bind(out _subfiles)
			.DisposeMany()
			.Subscribe();

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
