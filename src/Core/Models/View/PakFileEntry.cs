using ModManager.Helpers;

namespace ModManager.Models.View;
public class PakFileEntry(string filePath, bool isDirectory = false) : TreeViewEntry, IFileModel
{
	public override object ViewModel => this;

	public string FilePath { get; } = filePath;
	public string FileName { get; } = Path.GetFileName(filePath);
	public bool IsDirectory { get; } = isDirectory;

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
}
