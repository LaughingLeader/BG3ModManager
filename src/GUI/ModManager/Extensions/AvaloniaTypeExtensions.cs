﻿using Avalonia.Platform.Storage;

using FluentAvalonia.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager;
public static class AvaloniaTypeExtensions
{
	public static FilePickerFileType ToFilePickerType(this FileTypeFilter filter)
	{
		var first = filter.Extensions.FirstOrDefault() ?? "";
		if (first == "*" || first == "*.*") return FilePickerFileTypes.All;

		if (FilePickerFileTypes.TextPlain.Patterns != null && filter.Extensions.All(x => FilePickerFileTypes.TextPlain.Patterns.Contains(x)))
		{
			return FilePickerFileTypes.TextPlain;
		}

		return new FilePickerFileType(filter.Name)
		{
			Patterns = filter.Extensions,
			AppleUniformTypeIdentifiers = ["public.item"],
			MimeTypes = ["*/*"]
		};
	}
}