using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager;
public static class CommonFileTypes
{
	private static readonly string[] _archiveFormats = [".7z", ".7zip", ".gzip", ".rar", ".tar", ".tar.gz", ".zip"];
	private static readonly string[] _compressedFormats = [".bz2", ".xz", ".zst"];
	private static readonly string _archiveFormatsStr = string.Join(";", _archiveFormats.Select(x => "*" + x));
	private static readonly string _compressedFormatsStr = string.Join(";", _compressedFormats.Select(x => "*" + x));

	public static readonly FileTypeFilter All = new("All files (*.*)|*.*", ["*.*"]);
	public static readonly FileTypeFilter ArchiveFormats = new("Archive file (*.7z,*.rar;*.zip)", _archiveFormats);
	public static readonly FileTypeFilter CompressedFormats = new("Compressed file (*.bz2,*.xz;*.zst)", _compressedFormats);
	public static readonly FileTypeFilter ModPak = new("Mod package (*.pak)", ["*.pak"]);
	public static readonly FileTypeFilter AllImportModTypes = new($"All formats (*.pak;{_archiveFormatsStr};{_compressedFormatsStr})", ["*.pak", .. _archiveFormats, .. _compressedFormats]);
	public static readonly FileTypeFilter LarianSaveFile = new("Larian Save file (*.lsv)", ["*.lsv"]);
	public static readonly FileTypeFilter Json = new("JSON File (*.json)", ["*.json"]);
	public static readonly FileTypeFilter Text = new("Text file (*.txt)", ["*.txt"]);
	public static readonly FileTypeFilter Tsv = new("TSV file (*.tsv)", ["*.tsv"]);
	public static readonly FileTypeFilter AllModOrderTextFormats = new("All formats (*.json;*.txt;*.tsv)", ["*.json", "*.txt", "*.tsv"]);

	public static readonly FileTypeFilter[] ArchiveFileTypes = [new("Archive file (*.7z,*.rar;*.zip)", _archiveFormats), All];
	public static readonly FileTypeFilter[] CompressedFileTypes = [new("Compressed file (*.bz2,*.xz;*.zst)", _compressedFormats), All];
	public static readonly FileTypeFilter[] ImportModFileTypes = [AllImportModTypes, ModPak, ArchiveFormats, CompressedFormats, All];
	public static readonly FileTypeFilter[] ModOrderFileTypes = [AllModOrderTextFormats, Json, Text, Tsv, All];
}
