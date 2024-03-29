using System.Diagnostics.CodeAnalysis;

namespace ModManager;

public readonly struct OpenFileBrowserDialogResults
{
	public bool Success { get; init; }

	[MemberNotNullWhen(true, nameof(Success))]
	public string? File { get; init; }

	[MemberNotNullWhen(true, nameof(Success))]
	public string?[] Files { get; init; }

	public int Total { get; init; }

	public OpenFileBrowserDialogResults(bool success, string? file, string?[]? files)
	{
		Success = success;
		File = file;
		Files = files ?? [file];
		Total = Files.Length;
	}
}
