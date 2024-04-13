namespace ModManager;
public static class CommonFileTypes
{
	private static readonly string[] _archiveFormats = [".7z", ".7zip", ".gzip", ".rar", ".tar", ".tar.gz", ".zip"];
	private static readonly string[] _compressedFormats = [".bz2", ".xz", ".zst"];

	public static readonly FileTypeFilter All = new("All files", ["*.*"]);
	public static readonly FileTypeFilter ArchiveFormats = new("Archive file", _archiveFormats);
	public static readonly FileTypeFilter CompressedFormats = new("Compressed file", _compressedFormats);
	public static readonly FileTypeFilter ModPak = new("Mod package", ["*.pak"]);
	public static readonly FileTypeFilter AllImportModTypes = new($"All mod formats", ["*.pak", .. _archiveFormats, .. _compressedFormats]);
	public static readonly FileTypeFilter LarianSaveFile = new("Larian save file", ["*.lsv"]);
	public static readonly FileTypeFilter Json = new("JSON file", ["*.json", "*.jsonc"]);
	public static readonly FileTypeFilter Text = new("Text file", ["*.txt"]);
	public static readonly FileTypeFilter Tsv = new("TSV file", ["*.tsv"]);
	public static readonly FileTypeFilter AllModOrderTextFormats = new("All formats", ["*.json", "*.txt", "*.tsv"]);

	public static readonly FileTypeFilter[] ArchiveFileTypes = [new("Archive file", _archiveFormats), All];
	public static readonly FileTypeFilter[] CompressedFileTypes = [new("Compressed file", _compressedFormats), All];
	public static readonly FileTypeFilter[] ImportModFileTypes = [AllImportModTypes, ModPak, ArchiveFormats, CompressedFormats, All];
	public static readonly FileTypeFilter[] ModOrderFileTypes = [AllModOrderTextFormats, Json, Text, Tsv, All];
}
